using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-012
    public enum WildSlimeState
    {
        Resting,
        Pursuing,
    }

    // 기능: spec-007 (바이옴별 종 추첨은 spec-009, 추격은 spec-012)
    public sealed class WildSlimeAgent : MonoBehaviour, IDamageable
    {
        [SerializeField] private string speciesId = "slime_basic";
        [SerializeField] private int attackDamage = 5;
        [SerializeField] private float detectionRadius = 4f;
        [SerializeField] private HealthBar healthBar;
        [SerializeField] private DamageFlash damageFlash;

        // 기획 assetsNeeded 의 "weakened slime visual indicator". 약화는 지금까지
        // 슬라임이 멈추는 것 말고는 화면에 아무 표시가 없어, 잡을 때가 됐는지
        // 맞을 때까지 세어야 했다.
        [SerializeField] private GameObject weakenedIndicator;

        // spec-012 assetsNeeded 의 "slime alert indicator". 추격이 시작된 것을
        // 화면에서 알 방법이 지금까지 없었다 — 슬라임이 움직이기 시작할 때쯤엔
        // 이미 붙어 있다.
        [SerializeField] private GameObject alertIndicator;

        // 기획서 core_mechanics: "각 오염 스택은 바이옴의 오염 티어를 올리고,
        // 이는 야생 슬라임 스탯을 강화한다." 티어당 15% 가산.
        private const float StatMultiplierPerTier = 0.15f;

        // 스탯의 speed 1 이 초당 몇 유닛인지. 플레이어 기본 이동은 5 이므로
        // 기본 speed 3 인 슬라임은 플레이어보다 느리다 — 도망칠 수 있어야 한다.
        private const float UnitsPerSpeedPoint = 0.4f;

        public SlimeInstance Instance { get; private set; }

        public WildSlimeState State { get; private set; } = WildSlimeState.Resting;

        private Rigidbody2D _body;
        private Transform _player;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            if (_body == null)
            {
                Debug.LogError("WildSlimeAgent: Rigidbody2D 컴포넌트가 없어 추격을 비활성화합니다.");
            }

            speciesId = ResolveSpeciesId();
            int tier = CurrentCorruptionTier();
            float multiplier = 1f + StatMultiplierPerTier * tier;
            var stats = new SlimeStatBlock(
                Mathf.RoundToInt(20 * multiplier),
                Mathf.RoundToInt(attackDamage * multiplier),
                Mathf.RoundToInt(2 * multiplier),
                Mathf.RoundToInt(3 * multiplier));
            Instance = new SlimeInstance(speciesId, stats);

            UpdateHealthBar();
            if (weakenedIndicator != null)
            {
                weakenedIndicator.SetActive(false);
            }

            if (alertIndicator != null)
            {
                alertIndicator.SetActive(false);
            }
        }

        // spec-009: 바이옴마다 나오는 종이 다르다. 표에 풀이 없으면 인스펙터에
        // 지정된 종을 그대로 쓴다.
        private string ResolveSpeciesId()
        {
            BiomeCatalog catalog = GameManager.Instance != null ? GameManager.Instance.Catalog : null;
            if (catalog == null)
            {
                return speciesId;
            }

            BiomeEntry entry = catalog.Find(GameManager.Instance.CurrentBiomeId);
            if (entry == null || entry.speciesPool == null || entry.speciesPool.Length == 0)
            {
                return speciesId;
            }

            return entry.speciesPool[Random.Range(0, entry.speciesPool.Length)];
        }

        // spec-012: 플레이어가 감지 범위 안에 있으면 쫓고, 아니면 쉰다. 약화된
        // 개체는 멈춘다 — 도망치면 포획(spec-002)이 불가능해진다.
        private void FixedUpdate()
        {
            if (_body == null)
            {
                return;
            }

            if (Instance.weakened)
            {
                SetState(WildSlimeState.Resting);
                _body.linearVelocity = Vector2.zero;
                return;
            }

            Transform player = FindPlayer();
            bool inRange = player != null &&
                Vector2.Distance(player.position, transform.position) <= detectionRadius;

            SetState(inRange ? WildSlimeState.Pursuing : WildSlimeState.Resting);

            if (State != WildSlimeState.Pursuing)
            {
                _body.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
            _body.linearVelocity = direction * (Instance.baseStats.speed * UnitsPerSpeedPoint);
        }

        private Transform FindPlayer()
        {
            if (_player != null)
            {
                return _player;
            }

            GameObject found = GameObject.FindGameObjectWithTag("Player");
            _player = found != null ? found.transform : null;
            return _player;
        }

        private void SetState(WildSlimeState next)
        {
            if (State == next)
            {
                return;
            }

            WildSlimeState previous = State;
            State = next;
            Debug.Log($"slime_state_changed species={Instance.speciesId} from={previous} to={next}");

            if (alertIndicator != null)
            {
                alertIndicator.SetActive(next == WildSlimeState.Pursuing);
            }
        }

        private static int CurrentCorruptionTier()
        {
            if (BiomeStigmaManager.Instance == null || GameManager.Instance == null)
            {
                return 0;
            }

            return BiomeStigmaManager.Instance.GetCorruptionTier(GameManager.Instance.CurrentBiomeId);
        }

        public void ApplyDamage(int amount, object source)
        {
            if (Instance.weakened)
            {
                return;
            }

            Instance.currentHp = Mathf.Max(0, Instance.currentHp - amount);
            Debug.Log(
                $"slime_damaged species={Instance.speciesId} " +
                $"hp={Instance.currentHp}/{Instance.baseStats.maxHp}");

            if (damageFlash != null)
            {
                damageFlash.Play();
            }

            UpdateHealthBar();

            if (Instance.currentHp == 0)
            {
                Instance.weakened = true;
                RunLogWriter.AppendLine($"WildSlimeWeakened species={Instance.speciesId}");

                // 막대는 0 이 되면 스스로 숨는다. 그 자리를 약화 표식이 대신해
                // "이제 E 로 잡는다" 를 알린다.
                if (weakenedIndicator != null)
                {
                    weakenedIndicator.SetActive(true);
                }
            }
        }

        private void UpdateHealthBar()
        {
            if (healthBar == null)
            {
                return;
            }

            healthBar.SetRatio((float)Instance.currentHp / Mathf.Max(1, Instance.baseStats.maxHp));
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (Instance.weakened)
            {
                return;
            }

            IDamageable target = collision.collider.GetComponent<IDamageable>();
            if (target == null)
            {
                return;
            }

            target.ApplyDamage(Instance.baseStats.attack, this);
        }
    }
}
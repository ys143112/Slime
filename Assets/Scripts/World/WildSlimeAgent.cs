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

        // 붙어 있는 동안 이 간격으로 접촉 피해가 들어간다. 대상별로 나누지 않고
        // 하나로 둔다 — 슬라임 하나가 동시에 둘을 물고 있는 경우가 드물다.
        // ponytail: 전역 쿨다운. 다대일 상황이 흔해지면 대상별로 나눈다.
        [SerializeField] private float contactDamageInterval = 1.2f;

        // 이 거리 안이면 추격을 멈춘다. 충돌체 반지름이 서로 0.5 라 중심거리 1.0
        // 이 곧 맞닿은 상태다 — 조금 여유를 준 값이다.
        [SerializeField] private float contactStopDistance = 1.05f;
        [SerializeField] private HealthBar healthBar;
        [SerializeField] private DamageFlash damageFlash;
        [SerializeField] private SlimeAppearance appearance;

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

        // 야생 개체 전용 클래스다 — 동행은 별도 클래스(CompanionAgent)라 여기서
        // 진영이 갈릴 일이 없다.
        public Faction Faction => Faction.Wild;

        public WildSlimeState State { get; private set; } = WildSlimeState.Resting;

        private Rigidbody2D _body;
        private Transform _player;
        private ActorAnimation _animation;
        private float _nextContactDamageTime;

        private void Awake()
        {
            _animation = GetComponent<ActorAnimation>();
            _body = GetComponent<Rigidbody2D>();
            if (_body == null)
            {
                Debug.LogError("WildSlimeAgent: Rigidbody2D 컴포넌트가 없어 추격을 비활성화합니다.");
            }

            // 씬에 손배치된 개체는 여기서 스스로 스탯을 만든다. 스포너가 만든
            // 개체는 Instantiate 직후 Initialize 로 이 값을 덮는다.
            Initialize(ResolveSpeciesId(), CurrentCorruptionTier());
        }

        // 스포너·동행이 종과 티어를 밖에서 정하는 유일한 자리. 이 경로가 없으면
        // 스탯이 Awake 안에 갇혀 있어 호출자마다 Awake 를 각자 고치게 된다.
        // 티어 배율 → 종 편향 순서는 바깥에서 다시 걸지 말 것(두 번 곱해진다).
        public void Initialize(string species, int tier)
        {
            speciesId = string.IsNullOrEmpty(species) ? speciesId : species;

            float multiplier = 1f + StatMultiplierPerTier * tier;
            var stats = new SlimeStatBlock(
                Mathf.RoundToInt(20 * multiplier),
                Mathf.RoundToInt(attackDamage * multiplier),
                Mathf.RoundToInt(2 * multiplier),
                Mathf.RoundToInt(3 * multiplier));

            // 종의 성격(방어형은 방어가 높고 공격이 0, 무지개는 총량이 낮다)을
            // 오염 티어 배율 위에 얹는다. 표가 없으면 종 구분 없이 예전과 같다.
            SlimeSpecies speciesData = SlimeSpeciesCatalog.Lookup(speciesId);
            if (speciesData != null)
            {
                stats = speciesData.ApplyBias(stats);
            }

            Initialize(new SlimeInstance(speciesId, stats));
        }

        // 완성된 개체를 그대로 심는 경로. 교배로 나온 슬라임을 야생에 세우거나
        // 테스트가 스탯을 고정할 때 쓴다.
        public void Initialize(SlimeInstance instance)
        {
            if (instance == null)
            {
                Debug.LogError("WildSlimeAgent.Initialize: instance 가 null 이라 무시합니다.");
                return;
            }

            Instance = instance;
            speciesId = instance.speciesId;

            // 종마다 그림이 다르다. 야생 개체는 아직 돌연변이 판정 전이라
            // (포획 시점에 굴린다) 평상시 그림이 걸린다.
            if (appearance != null)
            {
                appearance.Apply(Instance);
            }

            UpdateHealthBar();
            if (weakenedIndicator != null)
            {
                weakenedIndicator.SetActive(Instance.weakened);
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
                if (_animation != null)
                {
                    _animation.SetMovement(Vector2.zero);
                }

                return;
            }

            Transform target = FindTarget();
            bool inRange = target != null &&
                Vector2.Distance(target.position, transform.position) <= detectionRadius;

            SetState(inRange ? WildSlimeState.Pursuing : WildSlimeState.Resting);

            if (State != WildSlimeState.Pursuing)
            {
                _body.linearVelocity = Vector2.zero;
                if (_animation != null)
                {
                    _animation.SetMovement(Vector2.zero);
                }

                return;
            }

            // 닿을 만큼 붙었으면 멈춘다. 계속 밀고 들어가면 상대(플레이어·동행)를
            // 밀어내며 둘 다 미끄러진다 — 접촉 피해는 붙어만 있어도 들어가므로
            // 밀어붙일 이유가 없다.
            Vector2 offset = (Vector2)target.position - (Vector2)transform.position;
            if (offset.magnitude <= contactStopDistance)
            {
                // 붙어 있는 동안 계속 때린다. 콜백이 아니라 여기서 내므로 서로
                // 멈춰 서 있어도(=강체가 잠들어도) 피해가 끊기지 않는다.
                TryContactDamage(target);

                _body.linearVelocity = Vector2.zero;
                if (_animation != null)
                {
                    _animation.SetMovement(Vector2.zero);
                }

                return;
            }

            Vector2 direction = offset.normalized;
            _body.linearVelocity = direction * (Instance.baseStats.speed * UnitsPerSpeedPoint);

            if (_animation != null)
            {
                _animation.SetMovement(_body.linearVelocity);
            }
        }

        // 플레이어와 동행 중 가까운 쪽을 문다. 플레이어만 보게 두면 동행이 앞을
        // 막고 서 있어도 그대로 지나쳐 뒤를 때려서, 동행을 세우는 의미가 없다.
        // CompanionAgent.Active 는 동행이 죽을 때 스스로 null 이 되므로 죽은
        // 대상을 계속 쫓는 일은 없다.
        private Transform FindTarget()
        {
            Transform player = FindPlayer();
            CompanionAgent companion = CompanionAgent.Active;
            if (companion == null)
            {
                return player;
            }

            if (player == null)
            {
                return companion.transform;
            }

            float toCompanion = Vector2.Distance(companion.transform.position, transform.position);
            float toPlayer = Vector2.Distance(player.position, transform.position);
            return toCompanion < toPlayer ? companion.transform : player;
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
            if (Instance.weakened || Factions.IsFriendlyFire(source, this))
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

            if (_animation != null)
            {
                _animation.PlayHit();
            }

            UpdateHealthBar();

            if (Instance.currentHp == 0)
            {
                Instance.weakened = true;

                // 야생 슬라임은 죽지 않고 약화된다(포획 대상으로 남는다). 그래도
                // 표현상으로는 쓰러지는 것이 맞아 Dead 상태를 쓴다.
                if (_animation != null)
                {
                    _animation.PlayDeath();
                }

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

        // 접촉 피해는 충돌 콜백이 아니라 거리 검사로 낸다.
        //
        // 처음엔 OnCollisionEnter2D 뿐이라 "접촉이 시작되는 순간"에만 때렸고,
        // 상대가 붙은 채로 서 있으면 다시 부딪히는 일이 없어 그때부터 무해했다.
        // 그래서 OnCollisionStay2D 를 더했더니 이번에는 **강체가 잠들어** 콜백이
        // 끊겼다 — 밀어내지 않으려고 추격을 멈추게 한 것이 곧 슬립 조건이었다.
        // 콜백 두 개가 서로 다른 이유로 각각 안 오는 셈이라, 물리 이벤트를 아예
        // 안 쓴다. 동행(CompanionAgent)이 쓰는 방식과 같아진다.
        private void TryContactDamage(Transform target)
        {
            if (Time.time < _nextContactDamageTime || Instance.baseStats.attack <= 0)
            {
                return;
            }

            var victim = target.GetComponent<IDamageable>();

            // 공격력 0 인 종(방어형)은 부딪혀도 피해를 주지 않는다 — 그 종의
            // 정의가 "공격 불가" 이므로 접촉 피해도 없어야 말이 된다.
            if (victim == null || victim.Faction == Faction)
            {
                return;
            }

            _nextContactDamageTime = Time.time + contactDamageInterval;

            if (_animation != null)
            {
                _animation.PlayAttack((Vector2)target.position - (Vector2)transform.position);
            }

            victim.ApplyDamage(Instance.baseStats.attack, this);
        }
    }
}
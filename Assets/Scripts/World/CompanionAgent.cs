using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// 플레이어를 따라다니며 야생 슬라임을 때리는 동행 슬라임. 상한 1마리이고,
    /// 쓰러지면 보유 목록에서 영구히 사라진다(배낭 몰수와 같은 축).
    /// </summary>
    /// <remarks>
    /// <b>WildSlimeAgent 를 상속하거나 재사용하지 않는다.</b> 공유되는 건 "대상
    /// 쪽으로 이동" 한 조각뿐이고, 나머지 다섯이 정반대다 — 진영이 다르고,
    /// 스탯을 자가 생성하지 않고(로스터 개체를 그대로 쓴다), 0HP 가 약화가 아니라
    /// 죽음이고, 쫓아갈 대상(플레이어)과 때릴 대상(야생)이 갈린다.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CompanionAgent : MonoBehaviour, IDamageable
    {
        // 상한 1마리. 정적 참조 하나가 곧 그 규칙이다 — 세는 코드가 따로 없다.
        public static CompanionAgent Active { get; private set; }

        [SerializeField] private HealthBar healthBar;
        [SerializeField] private DamageFlash damageFlash;
        [SerializeField] private SlimeAppearance appearance;

        [Header("따라다니기")]
        // 플레이어 기본 이동이 5 라 스탯 speed(기본 3 → 1.2유닛/초)를 그대로 쓰면
        // 영영 뒤처진다. 따라올 때만 이 값을 쓰고, 스탯 speed 는 전투 접근에만 쓴다.
        [SerializeField] private float followSpeed = 6.5f;
        [SerializeField] private float followDistance = 1.6f;


        // 씬이 바뀌거나 벽에 끼면 영영 못 따라온다. 이 거리를 넘으면 순간이동한다.
        [SerializeField] private float teleportDistance = 14f;

        [Header("전투")]
        [SerializeField] private float detectionRadius = 4.5f;

        // 동행과 야생 슬라임의 원 충돌체가 각각 반지름 0.5 라 두 중심은 1.0 아래로
        // 못 붙는다. 사거리를 그보다 짧게 잡으면 서로 밀려나는 찰나에만 판정이
        // 들어가 "때리는 시늉만 하고 피해가 없는" 상태가 된다.
        [SerializeField] private float attackRange = 1.2f;
        [SerializeField] private float attackInterval = 1.5f;

        // 스탯의 speed 1 이 초당 몇 유닛인지. WildSlimeAgent 와 같은 환산이다.
        private const float UnitsPerSpeedPoint = 0.4f;

        public SlimeInstance Instance { get; private set; }

        public Faction Faction => Faction.Player;

        private Rigidbody2D _body;
        private ActorAnimation _animation;
        private Transform _player;
        private float _nextAttackTime;
        private bool _dead;

        /// <summary>
        /// 로스터의 개체를 동행으로 내보낸다. 이미 나가 있으면 그 개체를 먼저
        /// 거둬들인다(상한 1마리).
        /// </summary>
        /// <remarks>
        /// 프리팹을 <c>Resources</c> 에서 읽는 이유: 동행을 내보내는 곳이
        /// 인벤토리 UI(Boot 씬 영속)라 씬 참조로는 닿지 않는다. 씬을 안 건드리므로
        /// 병합 사고도 피한다.
        /// </remarks>
        public static CompanionAgent Deploy(SlimeInstance instance, Vector3 position)
        {
            if (instance == null)
            {
                return null;
            }

            if (Active != null)
            {
                Active.Recall();
            }

            var prefab = Resources.Load<CompanionAgent>("Companion");
            if (prefab == null)
            {
                Debug.LogError("CompanionAgent.Deploy: Resources/Companion 프리팹을 찾지 못했습니다.");
                return null;
            }

            // 플레이어 좌표에 그대로 내보내면 두 충돌체가 완전히 겹친 채로
            // 시작해 물리가 서로를 튕겨낸다. 옆으로 한 칸 비켜서 세운다.
            CompanionAgent spawned = Instantiate(prefab, position + new Vector3(-1.2f, -0.4f, 0f), Quaternion.identity);

            // 바이옴을 오가도 따라와야 한다 — 씬과 함께 지워지면 다이브 때마다
            // 사라진다.
            DontDestroyOnLoad(spawned.gameObject);
            spawned.Bind(instance);
            return spawned;
        }

        /// <summary>동행을 거둬들인다(로스터에는 그대로 남는다).</summary>
        public void Recall()
        {
            if (Active == this)
            {
                Active = null;
            }

            Destroy(gameObject);
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _animation = GetComponent<ActorAnimation>();
        }

        private void Bind(SlimeInstance instance)
        {
            Active = this;
            Instance = instance;

            // 로스터 개체는 예전 런에서 다친 채로 남아 있을 수 있다. 약화 상태로
            // 내보내면 나가자마자 죽으므로 체력을 채워서 내보낸다.
            Instance.weakened = false;
            Instance.currentHp = Mathf.Max(1, Instance.baseStats.maxHp);

            if (appearance != null)
            {
                appearance.Apply(Instance);
            }

            UpdateHealthBar();
        }

        private void FixedUpdate()
        {
            if (_dead || Instance == null || _body == null)
            {
                return;
            }

            Transform player = FindPlayer();
            if (player == null)
            {
                _body.linearVelocity = Vector2.zero;
                return;
            }

            float toPlayer = Vector2.Distance(player.position, transform.position);
            if (toPlayer > teleportDistance)
            {
                transform.position = player.position;
                _body.linearVelocity = Vector2.zero;
                return;
            }

            // 적이 사거리 안이면 그쪽을 우선한다. 다만 플레이어에게서 너무 멀어지면
            // 따라가는 쪽으로 돌아온다 — 안 그러면 적을 쫓아 화면 밖으로 나간다.
            Vector2 enemyPosition = Vector2.zero;
            IDamageable enemy = toPlayer <= teleportDistance * 0.5f ? FindEnemy(out enemyPosition) : null;
            if (enemy != null)
            {
                // 사거리 딱 그 자리에 서면 밀리는 순간마다 판정이 들락날락한다.
                // 조금 더 파고들게 해서 사거리 안쪽에 머무르게 한다.
                MoveToward(enemyPosition, Instance.baseStats.speed * UnitsPerSpeedPoint, attackRange - 0.2f);
                TryAttack(enemy, enemyPosition);
                return;
            }

            Follow(player.position, toPlayer);
        }

        // 거리로 "멈춤/출발"을 나누지 않는다. 그 방식은 문턱을 아무리 벌려도
        // 걷다-서다를 반복해 애니메이션이 툭툭 끊긴다(히스테리시스로도 주기만
        // 길어질 뿐 없어지지 않았다).
        //
        // 대신 플레이어에서 followDistance 만큼 떨어진 자리를 목표로 두고, 그
        // 자리까지의 오차에 비례해 속도를 낸다. 목표 자리가 플레이어를 따라 계속
        // 움직이므로 플레이어가 걷는 동안에는 속도도 끊기지 않는다. 플레이어가
        // 서면 오차가 0 으로 줄어 자연히 멈춘다.
        private void Follow(Vector2 playerPosition, float distance)
        {
            Vector2 self = transform.position;

            // 플레이어에서 동행 쪽으로 followDistance 떨어진 점. 뒤로 돌아 들어가지
            // 않고 지금 서 있는 방향을 그대로 지킨다.
            Vector2 away = distance > 0.01f ? (self - playerPosition) / distance : Vector2.down;
            Vector2 anchor = playerPosition + away * followDistance;

            Vector2 error = anchor - self;
            float gap = error.magnitude;

            // 딱 붙어 서 있을 때 미세한 오차로 떠는 걸 막는다.
            if (gap < 0.12f)
            {
                _body.linearVelocity = Vector2.zero;
                if (_animation != null)
                {
                    _animation.SetMovement(Vector2.zero);
                }

                return;
            }

            _body.linearVelocity = error.normalized * Mathf.Min(followSpeed, gap * 6f);
            if (_animation != null)
            {
                _animation.SetMovement(_body.linearVelocity);
            }
        }

        private void MoveToward(Vector2 destination, float speed, float stopDistance)
        {
            Vector2 offset = destination - (Vector2)transform.position;
            if (offset.magnitude <= stopDistance)
            {
                _body.linearVelocity = Vector2.zero;
                if (_animation != null)
                {
                    _animation.SetMovement(Vector2.zero);
                }

                return;
            }

            _body.linearVelocity = offset.normalized * speed;
            if (_animation != null)
            {
                _animation.SetMovement(_body.linearVelocity);
            }
        }

        // 접촉 충돌(OnCollisionEnter2D)이 아니라 주기적인 원 검사로 때린다.
        // 밀착한 채로 서 있으면 충돌 이벤트가 다시 안 와서 한 대만 때리고 만다.
        private IDamageable FindEnemy(out Vector2 position)
        {
            position = Vector2.zero;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
            IDamageable best = null;
            float bestDistance = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                if (hit.gameObject == gameObject)
                {
                    continue;
                }

                var target = hit.GetComponent<IDamageable>();
                if (target == null || target.Faction == Faction)
                {
                    continue;
                }

                // 이미 약화된 개체는 때려도 안 들어간다. 그런 개체를 쫓느라
                // 멀쩡한 적을 지나치면 동행이 아무 일도 안 하는 것처럼 보인다.
                if (target is WildSlimeAgent wild && wild.Instance.weakened)
                {
                    continue;
                }

                float distance = Vector2.Distance(hit.transform.position, transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = target;
                    position = hit.transform.position;
                }
            }

            return best;
        }

        private void TryAttack(IDamageable enemy, Vector2 enemyPosition)
        {
            if (Instance.baseStats.attack <= 0 || Time.time < _nextAttackTime)
            {
                return;
            }

            if (Vector2.Distance(enemyPosition, transform.position) > attackRange)
            {
                return;
            }

            _nextAttackTime = Time.time + attackInterval;
            if (_animation != null)
            {
                _animation.PlayAttack(enemyPosition - (Vector2)transform.position);
            }

            // 판정이 나갔다는 것과 피해가 들어갔다는 것을 로그로 구분할 수 있게
            // 한다 — 사거리와 충돌체 반지름이 어긋나면 앞만 일어난다.
            Debug.Log($"companion_attack damage={Instance.baseStats.attack}");
            enemy.ApplyDamage(Instance.baseStats.attack, this);
        }

        private Transform FindPlayer()
        {
            if (_player != null)
            {
                return _player;
            }

            // 씬을 넘나들면 예전 플레이어가 파괴돼 참조가 죽는다 — 매번 다시 찾는다.
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            _player = found != null ? found.transform : null;

            // 플레이어와는 부딪히지 않게 한다. 따라다니는 사이라 계속 몸이 닿는데,
            // 그때마다 서로를 밀어내면 둘 다 버벅이고 좁은 통로에서는 낀다.
            // 레이어를 새로 만들지 않고 이 두 충돌체만 무시시킨다.
            if (_player != null)
            {
                var mine = GetComponent<Collider2D>();
                var theirs = _player.GetComponent<Collider2D>();
                if (mine != null && theirs != null)
                {
                    Physics2D.IgnoreCollision(mine, theirs);
                }
            }

            return _player;
        }

        public void ApplyDamage(int amount, object source)
        {
            if (_dead || Instance == null || Factions.IsFriendlyFire(source, this))
            {
                return;
            }

            Instance.currentHp = Mathf.Max(0, Instance.currentHp - amount);
            Debug.Log($"companion_damaged species={Instance.speciesId} hp={Instance.currentHp}/{Instance.baseStats.maxHp}");

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
                Die();
            }
        }

        // 야생 슬라임과 달리 동행은 약화가 아니라 죽는다 — 로스터에서 영구히
        // 사라진다. 이 몰수가 있어야 동행을 데리고 나가는 선택에 무게가 생긴다.
        private void Die()
        {
            _dead = true;
            _body.linearVelocity = Vector2.zero;

            if (_animation != null)
            {
                _animation.PlayDeath();
            }

            PlayerRoster.Instance?.Remove(Instance);
            RunLogWriter.AppendLine($"CompanionDied species={Instance.speciesId}");
            EventBus.Publish(new GameEvent(
                GameEventId.CompanionDied,
                GameManager.Instance != null ? GameManager.Instance.CurrentBiomeId : string.Empty,
                Instance));

            if (Active == this)
            {
                Active = null;
            }

            // 쓰러지는 그림을 잠깐 남긴다. 즉시 지우면 왜 사라졌는지 안 보인다.
            Destroy(gameObject, 1f);
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
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
    }
}

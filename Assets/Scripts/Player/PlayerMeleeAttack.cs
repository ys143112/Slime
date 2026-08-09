using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay
{
    // 기능: spec-007 (방향 공격은 spec-012)
    public sealed class PlayerMeleeAttack : MonoBehaviour, IFactionMember
    {
        // 판정 원 안에 동행 슬라임이 들어와도 때리지 않게 하는 근거.
        public Faction Faction => Faction.Player;

        // 8 이면 HP 20 짜리 tier0 슬라임에 3타, 티어가 오르면 4~5타라 잡는 데
        // 너무 오래 걸렸다(사용자, 2026-08-08). 12 면 tier0 는 2타, tier3(HP 29)
        // 도 3타다.
        [SerializeField] private int attackDamage = 12;
        [SerializeField] private float hitboxRadius = 0.6f;
        [SerializeField] private float reachOffset = 0.9f;

        // spec-012 assetsNeeded 의 "attack swing arc". 판정이 어느 쪽으로 나갔는지
        // 보이지 않으면 빗나간 것인지 닿았는데 안 들어간 것인지 구분할 수 없다.
        [SerializeField] private GameObject swingArc;
        [SerializeField] private float swingVisibleSeconds = 0.12f;

        // 쿨다운이 없어 연타하면 프레임마다 판정이 나갔다 — 아무 슬라임이나
        // 붙어서 스페이스를 문지르면 이겼다. 공격 애니메이션(0.12초)보다 넉넉히
        // 길어야 클립이 매번 처음으로 되감기지도 않는다.
        [SerializeField] private float attackCooldown = 0.5f;

        private PlayerMovement _movement;
        private ActorAnimation _animation;
        private float _swingHideTime;
        private float _nextAttackTime;

        /// <summary>0 = 방금 때림, 1 = 때릴 수 있음. 쿨다운 표시가 읽는다.</summary>
        public float CooldownRatio =>
            attackCooldown <= 0f ? 1f : Mathf.Clamp01(1f - (_nextAttackTime - Time.time) / attackCooldown);

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _animation = GetComponent<ActorAnimation>();
            AttackCooldownBar.Show(this);
            if (swingArc != null)
            {
                FitSwingArcToHitbox();
                swingArc.SetActive(false);
            }
        }

        /// <summary>궤적 그림을 판정 원 지름에 맞춘다.</summary>
        /// <remarks>
        /// 그림 크기가 씬에서 손으로 정해져 있어 판정(반지름 0.6, 지름 1.2유닛)보다
        /// 훨씬 컸다 — 그림에 닿았는데 안 맞는 자리가 생겨 표시가 거짓말을 한다
        /// (팀 QA, 2026-08-09). 배율을 상수로 박지 않고 스프라이트 실제 크기에서
        /// 계산하는 이유: <see cref="hitboxRadius"/> 든 그림이든 한쪽만 바뀌어도
        /// 다시 어긋나기 때문이다.
        /// </remarks>
        private void FitSwingArcToHitbox()
        {
            // 꺼진 오브젝트의 렌더러는 bounds 가 0 이다. 재는 동안만 켠다.
            bool wasActive = swingArc.activeSelf;
            swingArc.SetActive(true);

            var renderer = swingArc.GetComponentInChildren<SpriteRenderer>();
            float widest = renderer != null ? Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.y) : 0f;
            swingArc.SetActive(wasActive);

            if (widest <= 0.0001f)
            {
                return;
            }

            // 지금 크기 대비 배율이라 자식이 자기 스케일을 들고 있어도 맞는다.
            // Awake 에서 한 번만 부르므로 누적되지 않는다.
            float factor = hitboxRadius * 2f / widest;
            swingArc.transform.localScale *= factor;
        }

        // EventSystem 이 없으면(씬 배선 사고) 클릭을 통째로 막지 않는다 —
        // 공격이 조용히 안 되는 것보다 UI 뒤에서 한 번 휘두르는 편이 낫다.
        private static bool PointerOverUI()
        {
            var events = UnityEngine.EventSystems.EventSystem.current;
            return events != null && events.IsPointerOverGameObject();
        }

        private void Update()
        {
            // 공격은 마우스 좌클릭이다(사용자 결정, 2026-08-08). 예전엔 Space
            // 였는데, 이동이 WASD 라 왼손이 이미 바쁘고 오른손은 놀고 있었다.
            //
            // UI 위에서 누른 클릭은 무시한다 — 인벤토리 슬롯을 고르려고 누를
            // 때마다 뒤에서 칼을 휘두르면 안 된다.
            bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (clicked && Time.time >= _nextAttackTime && !PointerOverUI() && !BreedingPen.PointerOverPen())
            {
                _nextAttackTime = Time.time + attackCooldown;
                PerformAttack();
            }

            if (swingArc != null && swingArc.activeSelf && Time.time >= _swingHideTime)
            {
                swingArc.SetActive(false);
            }
        }

        private void PerformAttack()
        {
            PerformAttack(ResolveFacing());
        }

        // 마우스가 있으면 마우스 쪽, 없으면 마지막 이동 방향. 어느 쪽이든 상하좌우
        // 네 방향 중 하나로 접는다.
        private Vector2 ResolveFacing()
        {
            Vector2 fallback = _movement != null ? _movement.LastDirection : Vector2.down;
            Camera view = Camera.main;
            if (Mouse.current == null || view == null)
            {
                return SnapToCardinal(fallback);
            }

            Vector3 pointer = view.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 toPointer = (Vector2)pointer - (Vector2)transform.position;

            // 커서가 플레이어 위에 정확히 겹치면 방향이 없다. 그때는 이동 방향을 쓴다.
            return SnapToCardinal(toPointer.sqrMagnitude < 0.0001f ? fallback : toPointer);
        }

        // 대각선을 허용하면 판정 원 네 자리와 궤적 스프라이트 네 방향으로는
        // 표현할 수 없는 각이 생긴다. 큰 축 하나만 남긴다.
        public static Vector2 SnapToCardinal(Vector2 direction)
        {
            if (direction == Vector2.zero)
            {
                return Vector2.down;
            }

            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                return direction.x >= 0f ? Vector2.right : Vector2.left;
            }

            return direction.y >= 0f ? Vector2.up : Vector2.down;
        }

        private void PerformAttack(Vector2 facing)
        {
            // spec-012: 판정 원을 바라보는 쪽으로 밀어낸다. 반대쪽을 보고 때리면
            // 원이 대상에서 벗어나 피해가 0 이 된다.
            Vector2 origin = (Vector2)transform.position + facing * reachOffset;
            ShowSwingArc(facing);

            // 공격 애니메이션과 효과음은 판정과 같은 프레임에 나가야 한다 —
            // 늦으면 맞았는데 안 때린 것처럼 보인다.
            if (_animation != null)
            {
                _animation.PlayAttack(facing);
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, hitboxRadius);
            int struck = 0;
            foreach (Collider2D hit in hits)
            {
                if (hit.gameObject == gameObject)
                {
                    continue;
                }

                IDamageable target = hit.GetComponent<IDamageable>();
                if (target == null)
                {
                    continue;
                }

                target.ApplyDamage(attackDamage, this);
                struck++;
            }

            // 빗나가도 지금까진 아무 표시가 없어 키가 안 먹는 것처럼 보였다.
            // 판정 자체는 매번 발생한다는 걸 로그로 구분할 수 있게 한다.
            Debug.Log(struck > 0 ? $"melee_hit count={struck}" : "melee_miss");
        }

        // 궤적 스프라이트는 위(+Y)를 향해 그려져 있다. 판정 원과 같은 자리에
        // 놓고 바라보는 쪽으로 돌린다 — 판정과 그림이 어긋나면 표시가 거짓말을
        // 하게 된다.
        private void ShowSwingArc(Vector2 facing)
        {
            if (swingArc == null)
            {
                return;
            }

            // 월드 좌표로 놓는다. localPosition 을 쓰면 Player 루트의 스케일
            // (0.5)이 곱해져 그림만 판정 원의 절반 거리에 뜬다 — 이펙트와 실제
            // 사거리가 어긋나 보이던 원인이다(팀 QA, 2026-08-09). 크기는
            // FitSwingArcToHitbox 가 이미 월드 기준으로 맞춰 둔다.
            swingArc.transform.position = (Vector2)transform.position + facing * reachOffset;
            swingArc.transform.localRotation =
                Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, facing));
            swingArc.SetActive(true);
            _swingHideTime = Time.time + swingVisibleSeconds;
        }
    }
}
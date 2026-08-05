using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay
{
    // 기능: spec-007 (방향 공격은 spec-012)
    public sealed class PlayerMeleeAttack : MonoBehaviour
    {
        [SerializeField] private int attackDamage = 8;
        [SerializeField] private float hitboxRadius = 0.6f;
        [SerializeField] private float reachOffset = 0.9f;

        // spec-012 assetsNeeded 의 "attack swing arc". 판정이 어느 쪽으로 나갔는지
        // 보이지 않으면 빗나간 것인지 닿았는데 안 들어간 것인지 구분할 수 없다.
        [SerializeField] private GameObject swingArc;
        [SerializeField] private float swingVisibleSeconds = 0.12f;

        private PlayerMovement _movement;
        private float _swingHideTime;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            if (swingArc != null)
            {
                swingArc.SetActive(false);
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
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

            swingArc.transform.localPosition = facing * reachOffset;
            swingArc.transform.localRotation =
                Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, facing));
            swingArc.SetActive(true);
            _swingHideTime = Time.time + swingVisibleSeconds;
        }
    }
}
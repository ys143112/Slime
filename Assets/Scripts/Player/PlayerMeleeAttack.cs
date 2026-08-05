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
            // spec-012: 판정 원을 바라보는 쪽으로 밀어낸다. 반대쪽을 보고 때리면
            // 원이 대상에서 벗어나 피해가 0 이 된다.
            Vector2 facing = _movement != null ? _movement.LastDirection : Vector2.down;
            Vector2 origin = (Vector2)transform.position + facing * reachOffset;
            ShowSwingArc(facing);
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, hitboxRadius);
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
            }
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
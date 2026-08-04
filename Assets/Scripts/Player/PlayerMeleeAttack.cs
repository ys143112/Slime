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

        private PlayerMovement _movement;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                PerformAttack();
            }
        }

        private void PerformAttack()
        {
            // spec-012: 판정 원을 바라보는 쪽으로 밀어낸다. 반대쪽을 보고 때리면
            // 원이 대상에서 벗어나 피해가 0 이 된다.
            Vector2 facing = _movement != null ? _movement.LastDirection : Vector2.down;
            Vector2 origin = (Vector2)transform.position + facing * reachOffset;
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
    }
}
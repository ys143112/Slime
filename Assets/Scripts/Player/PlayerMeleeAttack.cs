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
    }
}
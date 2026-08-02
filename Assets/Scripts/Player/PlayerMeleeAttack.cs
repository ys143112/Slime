using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay
{
    // 기능: spec-007
    public sealed class PlayerMeleeAttack : MonoBehaviour
    {
        [SerializeField] private int attackDamage = 8;
        [SerializeField] private float hitboxRadius = 0.6f;
        [SerializeField] private Transform hitboxOrigin;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                PerformAttack();
            }
        }

        private void PerformAttack()
        {
            Vector2 origin = hitboxOrigin != null ? (Vector2)hitboxOrigin.position : (Vector2)transform.position;
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
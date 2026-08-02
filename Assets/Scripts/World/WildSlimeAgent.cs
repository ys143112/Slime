using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-007
    public sealed class WildSlimeAgent : MonoBehaviour, IDamageable
    {
        [SerializeField] private string speciesId = "slime_basic";
        [SerializeField] private int attackDamage = 5;

        public SlimeInstance Instance { get; private set; }

        private void Awake()
        {
            var stats = new SlimeStatBlock(20, attackDamage, 2, 3);
            Instance = new SlimeInstance(speciesId, stats);
        }

        public void ApplyDamage(int amount, object source)
        {
            if (Instance.weakened)
            {
                return;
            }

            Instance.currentHp = Mathf.Max(0, Instance.currentHp - amount);
            if (Instance.currentHp == 0)
            {
                Instance.weakened = true;
                RunLogWriter.AppendLine($"WildSlimeWeakened species={Instance.speciesId}");
            }
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

            target.ApplyDamage(attackDamage, this);
        }
    }
}
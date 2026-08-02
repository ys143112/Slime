using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-007
    public sealed class WildSlimeAgent : MonoBehaviour, IDamageable
    {
        [SerializeField] private string speciesId = "slime_basic";
        [SerializeField] private int attackDamage = 5;

        // 기획서 core_mechanics: "각 오염 스택은 바이옴의 오염 티어를 올리고,
        // 이는 야생 슬라임 스탯을 강화한다." 티어당 15% 가산.
        private const float StatMultiplierPerTier = 0.15f;

        public SlimeInstance Instance { get; private set; }

        private void Awake()
        {
            int tier = CurrentCorruptionTier();
            float multiplier = 1f + StatMultiplierPerTier * tier;
            var stats = new SlimeStatBlock(
                Mathf.RoundToInt(20 * multiplier),
                Mathf.RoundToInt(attackDamage * multiplier),
                Mathf.RoundToInt(2 * multiplier),
                Mathf.RoundToInt(3 * multiplier));
            Instance = new SlimeInstance(speciesId, stats);
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

            target.ApplyDamage(Instance.baseStats.attack, this);
        }
    }
}
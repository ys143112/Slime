using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-003 (돌연변이 판정은 spec-005, 오염 유전자 상속은 spec-006 을 함께 구현한다)
    public sealed class BreedingPen : MonoBehaviour
    {
        [SerializeField] private TraitInheritanceTable inheritanceTable;

        public void Breed(SlimeInstance parentA, SlimeInstance parentB)
        {
            if (inheritanceTable == null)
            {
                Debug.LogError("BreedingPen: TraitInheritanceTable 이 연결되지 않아 교배를 진행할 수 없습니다.");
                return;
            }

            SlimeStatBlock offspringStats = inheritanceTable.Blend(parentA.baseStats, parentB.baseStats);
            var offspring = new SlimeInstance(parentA.speciesId, offspringStats)
            {
                corruptedGeneFlag = parentA.corruptedGeneFlag || parentB.corruptedGeneFlag,
            };

            if (MutantRollService.TryRollMutant(0))
            {
                MutantRollService.ApplyReversal(offspring);
            }

            if (PlayerRoster.Instance == null)
            {
                Debug.LogError("BreedingPen: PlayerRoster 인스턴스가 없어 자손을 등록할 수 없습니다.");
                return;
            }

            PlayerRoster.Instance.Add(offspring);
            RunLogWriter.AppendLine($"BreedingCompleted species={offspring.speciesId}");
        }
    }
}
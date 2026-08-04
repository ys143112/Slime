using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-003 (돌연변이 판정은 spec-005, 오염 유전자 상속은 spec-006 을 함께 구현한다)
    //       배치·회수는 spec-008
    public sealed class BreedingPen : MonoBehaviour
    {
        public const int Capacity = 2;

        [SerializeField] private TraitInheritanceTable inheritanceTable;
        [SerializeField] private GameObject placedSlimePrefab;
        [SerializeField] private Transform[] slots = new Transform[Capacity];
        [SerializeField] private float hatchDurationSeconds = 30f;
        [SerializeField] private string hatchConditionLabel = "부화 시간 경과";


        private readonly List<SlimeInstance> _placed = new List<SlimeInstance>(Capacity);
        private readonly List<GameObject> _placedObjects = new List<GameObject>(Capacity);

        public int PlacedCount => _placed.Count;

        // spec-008: 보유 슬라임 한 마리를 교배장에 내놓는다. 두 마리가 차는
        // 순간 교배가 자동으로 시작되고 교배장은 다시 빈다.
        public bool TryPlace(SlimeInstance instance)
        {
            if (instance == null)
            {
                return false;
            }

            // 정상 경로에서는 걸리지 않는다 — 2마리가 차는 순간 교배하고 비우므로
            // 교배장이 2마리인 채로 쉬지 않는다. 교배가 도중에 터져 비우지 못했을
            // 때 계속 쌓이는 것만 막는다.
            if (_placed.Count >= Capacity)
            {
                Debug.Log("pen_full: 교배장에 이미 슬라임 2마리가 있습니다.");
                return false;
            }

            if (PlayerRoster.Instance == null || !PlayerRoster.Instance.Remove(instance))
            {
                Debug.Log("pen_place_failed: 보유 목록에 없는 슬라임입니다.");
                return false;
            }

            _placed.Add(instance);
            _placedObjects.Add(SpawnPlacedObject(_placed.Count - 1));

            if (_placed.Count == Capacity)
            {
                Breed(_placed[0], _placed[1]);
                Clear();
            }

            return true;
        }

        // spec-008: 교배가 시작되기 전이라면 마지막으로 내놓은 슬라임을 되돌린다.
        public SlimeInstance WithdrawLast()
        {
            if (_placed.Count == 0)
            {
                return null;
            }

            int last = _placed.Count - 1;
            SlimeInstance instance = _placed[last];
            _placed.RemoveAt(last);
            DestroyPlacedObject(last);

            if (PlayerRoster.Instance == null)
            {
                Debug.LogError("BreedingPen: PlayerRoster 인스턴스가 없어 슬라임을 되돌릴 수 없습니다.");
                return null;
            }

            PlayerRoster.Instance.Add(instance);
            return instance;
        }

        private GameObject SpawnPlacedObject(int index)
        {
            if (placedSlimePrefab == null)
            {
                Debug.LogWarning("BreedingPen: placedSlimePrefab 이 연결되지 않아 슬라임이 보이지 않습니다.");
                return null;
            }

            Transform slot = slots != null && index < slots.Length && slots[index] != null
                ? slots[index]
                : transform;
            return Instantiate(placedSlimePrefab, slot.position, Quaternion.identity, transform);
        }

        private void DestroyPlacedObject(int index)
        {
            if (index >= _placedObjects.Count)
            {
                return;
            }

            if (_placedObjects[index] != null)
            {
                Destroy(_placedObjects[index]);
            }

            _placedObjects.RemoveAt(index);
        }

        private void Clear()
        {
            _placed.Clear();
            foreach (GameObject placed in _placedObjects)
            {
                if (placed != null)
                {
                    Destroy(placed);
                }
            }

            _placedObjects.Clear();
        }

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

            // spec-005 acceptance: 부모 중 하나라도 MutantFlag 면 자손은 100% 상속한다.
            // 둘 다 아니면 그때만 바이옴 오염 티어를 반영한 낮은 확률로 새로 굴린다.
            if (parentA.mutantFlag || parentB.mutantFlag)
            {
                MutantRollService.ApplyReversal(offspring);
            }
            else
            {
                string biomeId = GameManager.Instance != null ? GameManager.Instance.CurrentBiomeId : string.Empty;
                int corruptionTier = BiomeStigmaManager.Instance != null
                    ? BiomeStigmaManager.Instance.GetCorruptionTier(biomeId)
                    : 0;
                if (MutantRollService.TryRollMutant(corruptionTier))
                {
                    MutantRollService.ApplyReversal(offspring);
                }
            }

            // spec-003: 자손은 즉시 로스터에 들어가지 않고 알로 감싸여 부화 시간만큼 기다린다.
            if (EggIncubator.Instance == null)
            {
                Debug.LogError("BreedingPen: EggIncubator 인스턴스가 없어 알을 생성할 수 없습니다.");
                return;
            }

            EggIncubator.Instance.AddEgg(offspring, hatchDurationSeconds, hatchConditionLabel);
            RunLogWriter.AppendLine($"BreedingProducedEgg species={offspring.speciesId}");
        }
    }
}

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

        private TextMesh _label;

        // 작업장과 같은 이유 — 여기 뭐가 있고 무슨 키를 누르는지 화면에
        // 아무 데도 안 적혀 있었다.
        private void Awake()
        {
            _label = WorldLabel.Attach(transform, string.Empty, 0.9f);
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            if (_label != null)
            {
                _label.text = $"교배장 ({_placed.Count}/{Capacity})\nQ 선택  F 내놓기  G 되돌리기";
            }
        }

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
            _placedObjects.Add(SpawnPlacedObject(_placed.Count - 1, instance));

            if (_placed.Count == Capacity)
            {
                Breed(_placed[0], _placed[1]);
                Clear();
            }

            RefreshLabel();
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
            RefreshLabel();
            return instance;
        }

        private GameObject SpawnPlacedObject(int index, SlimeInstance instance)
        {
            if (placedSlimePrefab == null)
            {
                Debug.LogWarning("BreedingPen: placedSlimePrefab 이 연결되지 않아 슬라임이 보이지 않습니다.");
                return null;
            }

            Transform slot = slots != null && index < slots.Length && slots[index] != null
                ? slots[index]
                : transform;
            GameObject spawned = Instantiate(placedSlimePrefab, slot.position, Quaternion.identity, transform);

            // 교배장에 내놓은 것이 어느 개체인지 눈으로 구분돼야 한다 — 종도
            // 이로치 색도 개체마다 다르므로 프리팹 기본 그림으로 두면 전부 같아 보인다.
            SlimeAppearance appearance = spawned.GetComponentInChildren<SlimeAppearance>();
            if (appearance != null)
            {
                appearance.Apply(instance);
            }

            return spawned;
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

            // 외형(PNG)도 유전한다 — 종이 곧 그림이므로 부모 중 한쪽의 종을
            // 물려받는다. parentA 로 고정하면 아빠 쪽만 계속 나와 "유전"이 아니다.
            string inheritedSpecies = Random.value < 0.5f ? parentA.speciesId : parentB.speciesId;

            // 부모 편향을 벗기고 섞은 뒤 자손 종의 편향을 입힌다. 그냥 섞으면
            // 방어형 부모의 2.2배 방어가 자손 종과 무관하게 흘러들어가, 종은
            // 용암인데 성격은 부모 평균인 개체가 나온다. 순서를 지켜야 "종이
            // 곧 성격" 이라는 규칙이 교배에서도 성립한다.
            SlimeStatBlock offspringStats = inheritanceTable.Blend(
                Unbias(parentA), Unbias(parentB));

            SlimeSpecies offspringSpecies = SlimeSpeciesCatalog.Lookup(inheritedSpecies);
            if (offspringSpecies != null)
            {
                offspringStats = offspringSpecies.ApplyBias(offspringStats);
            }

            var offspring = new SlimeInstance(inheritedSpecies, offspringStats)
            {
                corruptedGeneFlag = parentA.corruptedGeneFlag || parentB.corruptedGeneFlag,
            };

            string biomeId = GameManager.Instance != null ? GameManager.Instance.CurrentBiomeId : string.Empty;

            // spec-005 acceptance: 부모 중 하나라도 MutantFlag 면 자손은 100% 상속한다.
            // 둘 다 아니면 그때만 바이옴 오염 티어를 반영한 낮은 확률로 새로 굴린다.
            if (parentA.mutantFlag || parentB.mutantFlag)
            {
                MutantRollService.ApplyReversal(offspring);
                MutantRollService.TryRollShiny(offspring, biomeId);
            }
            else
            {
                int corruptionTier = BiomeStigmaManager.Instance != null
                    ? BiomeStigmaManager.Instance.GetCorruptionTier(biomeId)
                    : 0;
                MutantRollService.RollMutantAndShiny(offspring, corruptionTier, biomeId);
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

        // 표에 없는 종은 편향이 걸린 적도 없으므로 그대로 쓴다.
        private static SlimeStatBlock Unbias(SlimeInstance parent)
        {
            SlimeSpecies species = SlimeSpeciesCatalog.Lookup(parent.speciesId);
            return species != null ? species.RemoveBias(parent.baseStats) : parent.baseStats;
        }
    }
}

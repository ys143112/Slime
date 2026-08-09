using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
        [SerializeField] private string hatchConditionLabel = "부화 중";

        // 교배 전용 종이 나올 확률은 종마다 다르므로 여기가 아니라
        // SlimeSpecies.breedingChance 가 들고 있다(방패 25%, 무지개 5%).
        // 값 하나로 묶으면 둘 중 하나만 조정할 수가 없다.


        private readonly List<SlimeInstance> _placed = new List<SlimeInstance>(Capacity);
        private readonly List<GameObject> _placedObjects = new List<GameObject>(Capacity);

        public int PlacedCount => _placed.Count;

        [Tooltip("이 거리 안에서 건물을 클릭해야 교배창이 열린다.")]
        [SerializeField] private float interactRange = 3.5f;

        private TextMesh _label;
        private Collider2D _collider;
        private Transform _player;

        /// <summary>지금 마우스가 교배장 건물 위에 있나. 공격이 이 클릭을 안 먹게 한다.</summary>
        /// <remarks>
        /// 좌클릭이 공격 키가 되면서 건물을 누르는 클릭과 겹쳤다. 클릭을 두
        /// 곳에서 각자 읽으므로 어느 쪽이 먼저 도는지에 기대면 안 된다 —
        /// 공격 쪽이 이 상태를 직접 물어보게 한다(사용자 요청, 2026-08-08).
        /// </remarks>
        public static bool PointerOverPen()
        {
            foreach (BreedingPen pen in Open)
            {
                if (pen != null && pen.PointerOverThis())
                {
                    return true;
                }
            }

            return false;
        }

        // 목장에 하나뿐이지만 목록으로 든다 — static 하나로 두면 씬을 오갈 때
        // 죽은 참조가 남는다.
        private static readonly List<BreedingPen> Open = new List<BreedingPen>();

        private void OnEnable() => Open.Add(this);

        private void OnDisable() => Open.Remove(this);

        private void Update()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (!PlayerInRange() || !PointerOverThis())
            {
                return;
            }

            BreedingUIPanel.Instance?.Toggle();
        }

        private bool PlayerInRange()
        {
            if (_player == null)
            {
                GameObject found = GameObject.FindGameObjectWithTag("Player");
                _player = found != null ? found.transform : null;
            }

            return _player != null &&
                Vector2.Distance(_player.position, transform.position) <= interactRange;
        }

        private bool PointerOverThis()
        {
            if (_collider == null)
            {
                _collider = GetComponent<Collider2D>();
            }

            Camera camera = Camera.main;
            if (_collider == null || camera == null)
            {
                return false;
            }

            Vector2 world = camera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            return _collider.OverlapPoint(world);
        }

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
                // 이름 한 줄. 조작 안내는 게임 설명 패널이 적고 있다.
                _label.text = "교배장";
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

            // 교배 전용 종(방패·무지개)은 여기가 유일한 등장 경로다 — 야생 풀에
            // 없고, 자손 종은 부모 둘 중 하나라 부모에 없으면 영영 안 나온다.
            //
            // 부모가 서로 다른 종일 때로 제한하지 않는다. 확률을 종마다 못박은
            // 이상(방패 25%, 무지개 5%) 그 위에 조건을 하나 더 얹으면 실제
            // 빈도가 적힌 값과 달라진다 — 표를 읽고 기대한 것과 어긋난다.
            string special = PickBreedingOnlySpecies();
            if (!string.IsNullOrEmpty(special))
            {
                inheritedSpecies = special;
                Debug.Log($"breeding_special species={special}");
            }

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

        // 교배 전용 종을 각자 정해진 확률로 뽑는다(방패 25%, 무지개 5% —
        // 사용자 지정, 2026-08-08). 예전엔 후보를 균등 추첨해서 흔한 쪽과
        // 귀한 쪽이 같은 빈도로 나왔다.
        //
        // 굴림 하나를 누적 구간으로 가르므로 각 종의 확률이 표에 적힌 값 그대로다
        // — 종마다 따로 굴리면 앞 종이 빗나갈 확률만큼 뒤 종이 깎인다.
        private static string PickBreedingOnlySpecies()
        {
            SlimeSpeciesCatalog catalog = SlimeSpeciesCatalog.Instance;
            if (catalog == null)
            {
                return null;
            }

            float roll = Random.value;
            float cumulative = 0f;

            foreach (SlimeSpecies entry in catalog.Species)
            {
                if (entry == null || !entry.breedingOnly || entry.breedingChance <= 0f)
                {
                    continue;
                }

                cumulative += entry.breedingChance;
                if (roll < cumulative)
                {
                    return entry.speciesId;
                }
            }

            return null;
        }

        // 표에 없는 종은 편향이 걸린 적도 없으므로 그대로 쓴다.
        private static SlimeStatBlock Unbias(SlimeInstance parent)
        {
            SlimeSpecies species = SlimeSpeciesCatalog.Lookup(parent.speciesId);
            return species != null ? species.RemoveBias(parent.baseStats) : parent.baseStats;
        }
    }
}

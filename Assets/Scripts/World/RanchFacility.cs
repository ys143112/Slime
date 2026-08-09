using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-010
    // 배치된 슬라임 한 마리가 주기마다 자원을 낸다. 배치는 보유 목록에서
    // 빼는 것이므로, 배치된 개체는 교배장(spec-008)에 내놓을 수 없다 —
    // 한 마리가 두 곳에서 일하는 것을 막는 유일한 장치다.
    public sealed class RanchFacility : MonoBehaviour
    {
        [SerializeField] private string facilityId = "facility_1";

        [Tooltip("이 웅덩이가 받는 바이옴. 그 바이옴 야생 풀의 종만 눕힐 수 있다. 비우면 아무 종이나.")]
        [SerializeField] private string acceptedBiomeId;

        [Tooltip("켜면 교배 전용 종(방패·무지개)만 받는다. acceptedBiomeId 보다 우선한다.")]
        [SerializeField] private bool specialOnly;

        [Tooltip("이름표에 쓸 이름. 비우면 Rest Area.")]
        [SerializeField] private string displayName = "휴식소";
        [SerializeField] private LaborOutputTable outputTable;
        [SerializeField] private float tickSeconds = 2f;

        // 틱마다 최대 체력의 이 비율만큼 회복한다. 0.1 이면 10틱 = 20초에 완치다.
        // 고정값이 아니라 비율인 이유: 종마다 최대 체력이 16~28 로 달라, 고정값이면
        // 튼튼한 슬라임만 훨씬 오래 걸린다.
        [SerializeField] [Range(0f, 1f)] private float healPercentPerTick = 0.1f;

        public SlimeInstance Assigned { get; private set; }

        public int TotalProduced { get; private set; }

        public int OutputPerTick =>
            Assigned != null && outputTable != null ? outputTable.OutputFor(Assigned.baseStats) : 0;

        private float _elapsed;
        private TextMesh _label;
        private string _selectionText = "없음";
        private bool _nearby;
        private float _nearbyUntil;

        /// <summary>
        /// 플레이어가 가까이 있을 때 지금 고른 슬라임이 무엇인지 이름표에 띄운다.
        /// </summary>
        /// <remarks>
        /// Z 로 고른 결과가 화면 어디에도 안 나와 작동 확인이 불가능했다. 표시를
        /// 시설이 맡는 이유는 배선이 필요 없기 때문이다 — 인터랙터의 statusText 는
        /// 씬에서 꽂아야 하는데 어느 씬에도 안 꽂혀 있었다.
        /// </remarks>
        public void ShowSelection(string selectionText, int rosterCount)
        {
            _selectionText = rosterCount > 0 ? selectionText : "없음";

            // 인터랙터는 범위를 벗어나면 아예 안 부른다. "안 불린 지 좀 됐으면
            // 멀어진 것" 으로 판정해야 근접 안내가 계속 떠 있지 않는다.
            _nearby = true;
            _nearbyUntil = Time.time + 0.2f;
            RefreshLabel();
        }

        // 그림이 바닥 타일 조각이라 배경에 묻혀 여기 뭐가 있는지 안 보였다.
        // 전용 그림이 생기기 전까지는 글자가 그 역할을 한다.
        private void Awake()
        {
            // 웅덩이 그림이 2유닛이라 반지름 1. 그 위로 올려야 명패가 웅덩이를
            // 안 덮는다.
            _label = WorldLabel.Attach(transform, string.Empty, 1.3f);
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            if (_label == null)
            {
                return;
            }

            // 이름과 상태만. 조작키 안내(Z/X/C)는 게임 설명 패널이 이미 적고
            // 있어서 뺐다 — 세 줄짜리 안내가 웅덩이 넷에 동시에 떠 있으면
            // 목장이 글자로 뒤덮인다(사용자, 2026-08-08).
            string title = string.IsNullOrEmpty(displayName) ? "휴식소" : displayName;

            if (Assigned == null)
            {
                // 가까이 갔을 때만 지금 고른 슬라임을 보여준다. 그게 X 를 누르면
                // 무엇이 들어가는지 알 수 있는 유일한 자리다.
                _label.text = _nearby && _selectionText != "없음" ? $"{title}\n{_selectionText}" : title;
                return;
            }

            bool full = Assigned.currentHp >= Assigned.baseStats.maxHp;
            _label.text = $"{title}\n{SlimeSpeciesCatalog.DisplayName(Assigned.speciesId)} " +
                $"{Assigned.currentHp}/{Assigned.baseStats.maxHp}{(full ? " OK" : "")}";
        }

        // 눕혀 둔 슬라임이 틱마다 회복한다. 이것이 게임에 있는 유일한 회복
        // 수단이다 — 산출량(TotalProduced)은 아직 아무도 안 읽는 죽은 숫자라,
        // 여기에 슬라임을 넣을 이유가 실질적으로 이쪽뿐이다.
        private void Rest()
        {
            if (Assigned == null)
            {
                return;
            }

            int max = Assigned.baseStats.maxHp;
            if (Assigned.currentHp >= max)
            {
                return;
            }

            // 최소 1 은 올린다. 비율이 작고 최대 체력이 낮으면 반올림으로 0 이 되어
            // 영영 안 낫는다.
            int heal = Mathf.Max(1, Mathf.RoundToInt(max * healPercentPerTick));
            Assigned.currentHp = Mathf.Min(max, Assigned.currentHp + heal);
            Assigned.weakened = false;
            RefreshLabel();
        }

        /// <summary>이 휴식소가 받아 주는 종인가.</summary>
        /// <remarks>
        /// 웅덩이 넷이 바이옴별로 생김새가 다르다 — 아무 슬라임이나 아무 데나
        /// 눕힐 수 있으면 그림이 넷인 이유가 없다(사용자, 2026-08-08).
        /// 종 목록을 여기 적지 않고 <see cref="BiomeCatalog"/> 를 되읽는 이유:
        /// 바이옴에 종을 하나 더 넣을 때 씬의 웅덩이까지 고치게 하지 않으려는 것.
        /// </remarks>
        public bool Accepts(SlimeInstance instance)
        {
            if (instance == null)
            {
                return false;
            }

            SlimeSpecies species = SlimeSpeciesCatalog.Lookup(instance.speciesId);

            // 특수 수조는 야생에 안 나오는 종(방패·무지개) 전용이다. 그 종들은
            // 어느 바이옴 풀에도 없어서 다른 웅덩이가 받아 줄 수 없다.
            if (specialOnly)
            {
                return species != null && species.breedingOnly;
            }

            if (species != null && species.breedingOnly)
            {
                return false;
            }

            BiomeCatalog catalog = GameManager.Instance != null ? GameManager.Instance.Catalog : null;
            if (catalog == null || string.IsNullOrEmpty(acceptedBiomeId))
            {
                return true; // 표가 없으면 막지 않는다 — 회복 수단이 이것뿐이다.
            }

            foreach (string allowed in catalog.WildSpeciesPool(acceptedBiomeId))
            {
                if (allowed == instance.speciesId)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryAssign(SlimeInstance instance)
        {
            if (instance == null || Assigned != null)
            {
                Debug.Log("labor_assign_failed: 이미 배치된 슬라임이 있습니다.");
                return false;
            }

            if (!Accepts(instance))
            {
                Debug.Log($"labor_assign_failed: {facilityId} 는 {instance.speciesId} 를 받지 않습니다.");
                return false;
            }

            if (PlayerRoster.Instance == null || !PlayerRoster.Instance.Remove(instance))
            {
                Debug.Log("labor_assign_failed: 보유 목록에 없는 슬라임입니다.");
                return false;
            }

            Assigned = instance;
            _elapsed = 0f;
            RefreshLabel();
            return true;
        }

        public SlimeInstance Unassign()
        {
            if (Assigned == null)
            {
                return null;
            }

            if (PlayerRoster.Instance == null)
            {
                Debug.LogError("RanchFacility: PlayerRoster 인스턴스가 없어 슬라임을 되돌릴 수 없습니다.");
                return null;
            }

            SlimeInstance released = Assigned;
            Assigned = null;
            _elapsed = 0f;
            RefreshLabel();
            PlayerRoster.Instance.Add(released);
            return released;
        }

        private void Update()
        {
            if (_nearby && Time.time > _nearbyUntil)
            {
                _nearby = false;
                RefreshLabel();
            }

            if (Assigned == null || outputTable == null)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed < tickSeconds)
            {
                return;
            }

            _elapsed -= tickSeconds;
            int amount = OutputPerTick;
            TotalProduced += amount;
            Debug.Log($"labor_output facility={facilityId} amount={amount}");
            RunLogWriter.AppendLine($"LaborOutput facility={facilityId} amount={amount}");

            Rest();
        }
    }
}

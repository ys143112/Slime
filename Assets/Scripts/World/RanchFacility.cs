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
            _label = WorldLabel.Attach(transform, string.Empty, 0.7f);
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            if (_label == null)
            {
                return;
            }

            if (Assigned == null)
            {
                _label.text = _nearby
                    ? $"휴식소 (비어 있음)\n선택: {_selectionText}\nZ 전환  X 눕히기"
                    : "휴식소\n가까이 가면 조작할 수 있다";
                return;
            }

            bool full = Assigned.currentHp >= Assigned.baseStats.maxHp;
            _label.text = $"휴식소: {SlimeSpeciesCatalog.DisplayName(Assigned.speciesId)}\n" +
                $"HP {Assigned.currentHp}/{Assigned.baseStats.maxHp}{(full ? " (다 나음)" : "")}  C 회수";
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

        public bool TryAssign(SlimeInstance instance)
        {
            if (instance == null || Assigned != null)
            {
                Debug.Log("labor_assign_failed: 이미 배치된 슬라임이 있습니다.");
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

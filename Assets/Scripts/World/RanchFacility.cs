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

        public SlimeInstance Assigned { get; private set; }

        public int TotalProduced { get; private set; }

        public int OutputPerTick =>
            Assigned != null && outputTable != null ? outputTable.OutputFor(Assigned.baseStats) : 0;

        private float _elapsed;
        private TextMesh _label;

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

            _label.text = Assigned == null
                ? "작업장\nZ 선택  X 배치"
                : $"작업장: {Assigned.speciesId}\n산출 {OutputPerTick}/틱  C 회수";
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
        }
    }
}

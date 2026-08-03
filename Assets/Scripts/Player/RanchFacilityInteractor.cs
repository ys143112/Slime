using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: spec-010
    // 교배장 조작(spec-008)과 같은 방식이되 키가 다르다 — Z 선택 전환,
    // X 배치, C 회수.
    public sealed class RanchFacilityInteractor : MonoBehaviour
    {
        [SerializeField] private float interactRange = 1.5f;
        [SerializeField] private Text statusText;

        private int _selectedIndex;

        private void Update()
        {
            RanchFacility facility = FindFacilityInRange();

            if (Keyboard.current != null)
            {
                if (Keyboard.current.zKey.wasPressedThisFrame)
                {
                    CycleSelection();
                }

                if (facility != null && Keyboard.current.xKey.wasPressedThisFrame)
                {
                    Assign(facility);
                }

                if (facility != null && Keyboard.current.cKey.wasPressedThisFrame)
                {
                    facility.Unassign();
                    ClampSelection();
                }
            }

            Refresh(facility);
        }

        private RanchFacility FindFacilityInRange()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRange);
            foreach (Collider2D hit in hits)
            {
                RanchFacility facility = hit.GetComponent<RanchFacility>();
                if (facility != null)
                {
                    return facility;
                }
            }

            return null;
        }

        private void CycleSelection()
        {
            int count = RosterCount();
            if (count == 0)
            {
                _selectedIndex = 0;
                return;
            }

            _selectedIndex = (_selectedIndex + 1) % count;
        }

        private void ClampSelection()
        {
            int count = RosterCount();
            _selectedIndex = count == 0 ? 0 : Mathf.Clamp(_selectedIndex, 0, count - 1);
        }

        private void Assign(RanchFacility facility)
        {
            if (RosterCount() == 0)
            {
                Debug.Log("labor_assign_failed: 보유한 슬라임이 없습니다.");
                return;
            }

            ClampSelection();
            facility.TryAssign(PlayerRoster.Instance.Roster[_selectedIndex]);
            ClampSelection();
        }

        private static int RosterCount()
        {
            return PlayerRoster.Instance != null ? PlayerRoster.Instance.Roster.Count : 0;
        }

        // 배치 수·산출량 표시는 매 프레임 실제 값을 그대로 읽는다.
        private void Refresh(RanchFacility facility)
        {
            if (statusText == null)
            {
                return;
            }

            if (facility == null)
            {
                statusText.text = string.Empty;
                return;
            }

            int count = RosterCount();
            string selected = count > 0
                ? PlayerRoster.Instance.Roster[Mathf.Clamp(_selectedIndex, 0, count - 1)].speciesId
                : "없음";
            string assigned = facility.Assigned != null ? facility.Assigned.speciesId : "비어 있음";
            statusText.text =
                $"작업장: {assigned} | 산출 {facility.OutputPerTick}/틱 (누적 {facility.TotalProduced}) | " +
                $"선택: {selected} ({count}마리) | Z 전환 X 배치 C 회수";
        }
    }
}

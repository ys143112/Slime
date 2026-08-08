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

        // 매 프레임 실제 값을 그대로 읽는다.
        //
        // 표시를 시설의 이름표로 보내는 이유: statusText 는 씬에서 배선해야 하는데
        // 지금 어느 씬에도 안 꽂혀 있어, Z 를 눌러도 무엇이 골렸는지 화면에 아무
        // 표시가 없었다 — 작동 확인이 불가능했다(2026-08-08). 이름표는 시설이
        // 스스로 만들므로 배선이 필요 없다.
        private void Refresh(RanchFacility facility)
        {
            if (facility != null)
            {
                facility.ShowSelection(SelectedSpeciesId(), RosterCount());
            }

            if (statusText == null)
            {
                return;
            }

            if (facility == null)
            {
                statusText.text = string.Empty;
                return;
            }

            statusText.text =
                $"휴식소: {(facility.Assigned != null ? SlimeSpeciesCatalog.DisplayName(facility.Assigned.speciesId) : "비어 있음")} | " +
                $"선택: {SelectedSpeciesId()} ({RosterCount()}마리) | Z 전환 X 눕히기 C 회수";
        }

        private string SelectedSpeciesId()
        {
            int count = RosterCount();
            if (count == 0)
            {
                return "없음";
            }

            SlimeInstance selected = PlayerRoster.Instance.Roster[Mathf.Clamp(_selectedIndex, 0, count - 1)];
            return $"{SlimeSpeciesCatalog.DisplayName(selected.speciesId)} (HP {selected.currentHp}/{selected.baseStats.maxHp})";
        }
    }
}

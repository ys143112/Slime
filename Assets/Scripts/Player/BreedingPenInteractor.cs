using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: spec-008
    public sealed class BreedingPenInteractor : MonoBehaviour
    {
        [SerializeField] private float interactRange = 1.5f;
        [SerializeField] private Text statusText;

        private int _selectedIndex;

        private void Update()
        {
            BreedingPen pen = FindPenInRange();

            if (Keyboard.current != null)
            {
                if (Keyboard.current.qKey.wasPressedThisFrame)
                {
                    CycleSelection();
                }

                if (pen != null && Keyboard.current.fKey.wasPressedThisFrame)
                {
                    Place(pen);
                }

                if (pen != null && Keyboard.current.gKey.wasPressedThisFrame)
                {
                    pen.WithdrawLast();
                    ClampSelection();
                }
            }

            // spec-008 acceptance: 표시는 실제 배치 수와 항상 같아야 한다 —
            // 배치·회수 경로마다 갱신을 부르는 대신 매 프레임 그대로 읽는다.
            Refresh(pen);
        }

        private BreedingPen FindPenInRange()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRange);
            foreach (Collider2D hit in hits)
            {
                BreedingPen pen = hit.GetComponent<BreedingPen>();
                if (pen != null)
                {
                    return pen;
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

        private void Place(BreedingPen pen)
        {
            if (PlayerRoster.Instance == null)
            {
                Debug.LogError("BreedingPenInteractor: PlayerRoster 인스턴스가 없어 슬라임을 내놓을 수 없습니다.");
                return;
            }

            if (RosterCount() == 0)
            {
                Debug.Log("pen_place_failed: 보유한 슬라임이 없습니다.");
                return;
            }

            ClampSelection();
            pen.TryPlace(PlayerRoster.Instance.Roster[_selectedIndex]);
            ClampSelection();
        }

        private static int RosterCount()
        {
            return PlayerRoster.Instance != null ? PlayerRoster.Instance.Roster.Count : 0;
        }

        private void Refresh(BreedingPen pen)
        {
            if (statusText == null)
            {
                return;
            }

            if (pen == null)
            {
                statusText.text = string.Empty;
                return;
            }

            int count = RosterCount();
            string selected = count > 0
                ? PlayerRoster.Instance.Roster[Mathf.Clamp(_selectedIndex, 0, count - 1)].speciesId
                : "없음";
            statusText.text =
                $"교배장 {pen.PlacedCount}/{BreedingPen.Capacity} | 선택: {selected} ({count}마리) | Q 전환 F 배치 G 회수";
        }
    }
}

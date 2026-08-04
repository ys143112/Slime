using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay
{
    // 기능: spec-002. 포획해 보유 목록(PlayerRoster)에 들어간 슬라임을 눈으로
    // 확인하는 창 — I 키로 토글한다. 번식장·목장도 나중에 같은 목록을 보여줘야
    // 하므로 PlayerRoster 를 직접 읽어 슬롯을 늘어놓기만 한다 (데이터 사본 없음).
    public sealed class InventoryUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform slotContainer;
        [SerializeField] private InventorySlotView slotPrefab;

        private readonly List<InventorySlotView> _spawned = new List<InventorySlotView>();

        private void Awake()
        {
            // Boot 씬에만 있다 — 바이옴을 오가도 인벤토리 창은 그대로 떠 있어야 한다.
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        private void Toggle()
        {
            if (panelRoot == null)
            {
                return;
            }

            bool next = !panelRoot.activeSelf;
            panelRoot.SetActive(next);
            if (next)
            {
                Refresh();
            }
        }

        // 번식장·목장 UI 가 슬라임을 골라야 할 때 재사용할 진입점. 열려 있지
        // 않아도 최신 목록을 강제로 다시 그린다.
        public void Refresh()
        {
            foreach (InventorySlotView slot in _spawned)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }

            _spawned.Clear();

            if (PlayerRoster.Instance == null || slotPrefab == null || slotContainer == null)
            {
                return;
            }

            foreach (SlimeInstance instance in PlayerRoster.Instance.Roster)
            {
                InventorySlotView slot = Instantiate(slotPrefab, slotContainer);
                slot.Bind(instance);
                _spawned.Add(slot);
            }
        }
    }
}

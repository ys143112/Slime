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

        public static InventoryUI Instance { get; private set; }

        private readonly List<InventorySlotView> _spawned = new List<InventorySlotView>();

        private void Awake()
        {
            Instance = this;

            // Boot 씬에만 있다 — 바이옴을 오가도 인벤토리 창은 그대로 떠 있어야 한다.
            DontDestroyOnLoad(gameObject);
        }

        // PlayerRoster.Instance 는 Awake 에서 세팅된다 - 같은 Boot 씬의 영속
        // 오브젝트끼리는 Awake 순서가 보장되지 않으므로, OnEnable 에서 구독하면
        // PlayerRoster 가 아직 null 일 때 조용히 스킵되고 다시는 구독되지 않을 수
        // 있다. Start 는 씬의 모든 Awake 가 끝난 뒤 실행이 보장된다.
        private void Start()
        {
            if (PlayerRoster.Instance != null)
            {
                PlayerRoster.Instance.RosterChanged += OnRosterChanged;
            }
        }

        private void OnDisable()
        {
            if (PlayerRoster.Instance != null)
            {
                PlayerRoster.Instance.RosterChanged -= OnRosterChanged;
            }
        }

        // 열려 있는 채로 포획·부화·교배장 배치가 일어나도 다시 열 필요 없이
        // 바로 갱신되도록 - 닫혀 있으면 어차피 Toggle() 이 열 때 다시 그린다.
        private void OnRosterChanged()
        {
            if (panelRoot != null && panelRoot.activeSelf)
            {
                Refresh();
            }
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
                // I 로 인벤토리를 열 때 교배 UI(U)가 떠 있으면 같이 닫는다 -
                // 두 창이 동시에 겹치면 어느 쪽이 입력을 받는지 알 수 없다.
                BreedingUIPanel.Instance?.Close();
                BestiaryPanel.CloseIfOpen();
                Refresh();
            }
        }

        public void Close()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
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

                // 슬롯을 누르면 그 개체가 동행으로 나간다. 새 화면도 새 버튼
                // 프리팹도 만들지 않는다 — 슬롯은 매번 다시 만들어지므로 구독을
                // 따로 풀 필요도 없다.
                slot.Clicked += ToggleCompanion;
                _spawned.Add(slot);
            }
        }

        // 이미 나가 있는 개체를 다시 누르면 거둬들인다. 상한 1마리라 다른 개체를
        // 누르면 CompanionAgent.Deploy 가 앞의 동행을 알아서 거둔다.
        private void ToggleCompanion(SlimeInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            if (CompanionAgent.Active != null && CompanionAgent.Active.Instance == instance)
            {
                CompanionAgent.Active.Recall();
                Refresh();
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("InventoryUI: 플레이어가 없는 화면이라 동행을 내보낼 수 없습니다.");
                return;
            }

            CompanionAgent.Deploy(instance, player.transform.position);
            Refresh();
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

        // 격자 한 칸 크기와 간격. 슬롯 프리팹 크기는 무시된다 —
        // GridLayoutGroup 이 자식 RectTransform 을 cellSize 로 덮어쓴다.
        private const int Columns = 6;
        private static readonly Vector2 CellSize = new Vector2(140f, 140f);
        private static readonly Vector2 CellSpacing = new Vector2(14f, 14f);

        private void Awake()
        {
            Instance = this;

            // Boot 씬에만 있다 — 바이옴을 오가도 인벤토리 창은 그대로 떠 있어야 한다.
            DontDestroyOnLoad(gameObject);

            EnsureGrid();
        }

        /// <summary>슬롯을 한 줄짜리 목록이 아니라 격자로 늘어놓는다.</summary>
        /// <remarks>
        /// 씬의 Content 에는 <see cref="VerticalLayoutGroup"/> 이 붙어 있어 슬롯이
        /// 세로로만 쌓였다(사용자, 2026-08-09: 참고 그림처럼 가로·세로로 채워
        /// 달라). 씬을 고치는 대신 코드에서 바꾸는 이유는 Boot 씬이 병합 사고로
        /// UI 를 통째로 잃은 전력이 있어서다(CLAUDE.md 「씬 병합 함정」).
        ///
        /// <see cref="ContentSizeFitter"/> 가 있어야 줄이 늘어난 만큼 Content 가
        /// 길어져 스크롤이 생긴다 — 없으면 첫 화면 밖의 슬라임은 볼 수 없다.
        /// </remarks>
        private void EnsureGrid()
        {
            if (slotContainer == null)
            {
                return;
            }

            // DestroyImmediate 여야 한다. Destroy 는 프레임 끝에 처리되는데, 한
            // 오브젝트에 LayoutGroup 은 하나뿐이라 그동안은 AddComponent 가
            // **에러 없이 거부된다** — 세로 정렬이 사라진 자리에 격자가 안 붙어
            // 슬롯이 전부 한 칸에 겹쳐 쌓였다(2026-08-09 실측).
            var vertical = slotContainer.GetComponent<VerticalLayoutGroup>();
            if (vertical != null)
            {
                DestroyImmediate(vertical);
            }

            var grid = slotContainer.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                grid = slotContainer.gameObject.AddComponent<GridLayoutGroup>();
            }

            grid.cellSize = CellSize;
            grid.spacing = CellSpacing;
            grid.padding = new RectOffset(16, 16, 16, 16);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            grid.childAlignment = TextAnchor.UpperLeft;

            var fitter = slotContainer.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = slotContainer.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
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

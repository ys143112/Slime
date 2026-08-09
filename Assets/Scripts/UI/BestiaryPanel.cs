using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Gameplay
{
    /// <summary>J 로 여는 도감 창. 종 목록과 진행도(몇/총)를 보여준다.</summary>
    /// <remarks>
    /// 씬이 아니라 코드로 만든다 — Boot 씬은 병합 사고로 UI 가 통째로 사라진
    /// 전력이 있고(CLAUDE.md 「씬 병합 함정」), 이 창은 종 카탈로그만 있으면
    /// 그려진다. <see cref="HudRoot"/> 캔버스를 같이 쓰므로 기준 해상도도 하나다.
    ///
    /// 안 잡은 종은 이름과 그림을 가린다("???"). 가리지 않으면 도감이 목표가
    /// 아니라 그냥 설명서가 된다.
    /// </remarks>
    public sealed class BestiaryPanel : MonoBehaviour
    {
        private static BestiaryPanel _instance;

        private GameObject _window;
        private Text _title;
        private readonly List<GameObject> _rows = new List<GameObject>();
        private Transform _list;

        public static void Show()
        {
            if (_instance != null)
            {
                return;
            }

            // 루트는 RectTransform 이어야 한다 — 그냥 Transform 이면 자식 앵커가
            // 크기 0 부모를 기준으로 잡힌다(ScreenInfoHud 와 같은 함정).
            var go = new GameObject("BestiaryPanel", typeof(RectTransform), typeof(BestiaryPanel));
            go.transform.SetParent(HudRoot.Get(), false);
            HudRoot.Stretch(go.GetComponent<RectTransform>());
            _instance = go.GetComponent<BestiaryPanel>();
        }

        /// <summary>다른 창이 열릴 때 닫는다 — 겹쳐 뜨면 둘 다 못 읽는다.</summary>
        /// <returns>실제로 닫았으면 true. Esc 처리가 이 값으로 "닫을 게 있었나" 를 안다.</returns>
        public static bool CloseIfOpen()
        {
            if (_instance == null || !_instance._window.activeSelf)
            {
                return false;
            }

            _instance._window.SetActive(false);
            return true;
        }

        private void Awake()
        {
            BuildWindow();
            _window.SetActive(false);
            SlimeBestiary.Changed += Refresh;
        }

        private void OnDestroy()
        {
            SlimeBestiary.Changed -= Refresh;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.jKey.wasPressedThisFrame)
            {
                return;
            }

            bool open = !_window.activeSelf;
            _window.SetActive(open);

            if (!open)
            {
                return;
            }

            // 인벤토리·교배 창과 상호 배타로 연다(둘은 서로 이미 그렇게 한다).
            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.Close();
            }

            if (BreedingUIPanel.Instance != null)
            {
                BreedingUIPanel.Instance.Close();
            }

            if (SatchelCounterUI.Instance != null)
            {
                SatchelCounterUI.Instance.Close();
            }

            Refresh();
        }

        private void BuildWindow()
        {
            _window = HudRoot.Frame("Window", transform);
            var rect = _window.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(760f, 620f);

            _title = HudRoot.Label("Title", _window.transform, 33);
            _title.alignment = TextAnchor.MiddleLeft;
            var titleRect = _title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(24f, -70f);
            titleRect.offsetMax = new Vector2(-24f, -16f);

            var listGo = new GameObject("List", typeof(RectTransform), typeof(GridLayoutGroup));
            listGo.transform.SetParent(_window.transform, false);
            var listRect = listGo.GetComponent<RectTransform>();
            listRect.anchorMin = Vector2.zero;
            listRect.anchorMax = Vector2.one;
            listRect.offsetMin = new Vector2(24f, 24f);
            listRect.offsetMax = new Vector2(-24f, -80f);

            // 인벤토리와 같은 격자다(사용자, 2026-08-09). 한 줄에 한 종씩 긴
            // 설명을 늘어놓으면 창이 표가 아니라 문서가 된다 — 종이 늘수록
            // 세로로 흘러 넘친다.
            var grid = listGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = CellSize;
            grid.spacing = new Vector2(16f, 16f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            grid.childAlignment = TextAnchor.UpperLeft;

            _list = listGo.transform;
        }

        private const int Columns = 4;
        private static readonly Vector2 CellSize = new Vector2(160f, 190f);

        private void Refresh()
        {
            SlimeSpeciesCatalog catalog = SlimeSpeciesCatalog.Instance;
            if (catalog == null || _window == null)
            {
                return;
            }

            foreach (GameObject row in _rows)
            {
                Destroy(row);
            }

            _rows.Clear();

            _title.text = $"도감   {SlimeBestiary.CaughtCount}/{SlimeBestiary.TotalSpecies} 종" +
                $"   ·   이로치 {SlimeBestiary.ShinyCount}";

            foreach (SlimeSpecies species in catalog.Species)
            {
                if (species == null)
                {
                    continue;
                }

                _rows.Add(BuildCell(species));
            }
        }

        /// <summary>격자 한 칸 = 종 하나. 그림 + 이름 + 한 줄 요약.</summary>
        /// <remarks>
        /// 자리는 <see cref="GridLayoutGroup"/> 이 정하므로 앵커를 손으로 잡지
        /// 않는다 — 잡으면 격자가 덮어써서 결과가 안 보인다. 스탯 표는 뺐다:
        /// 칸에 다 못 들어가고, 같은 숫자를 <see cref="SlimeTooltipUI"/> 가
        /// 이미 보유 슬라임에 대해 보여준다.
        /// </remarks>
        private GameObject BuildCell(SlimeSpecies species)
        {
            bool caught = SlimeBestiary.IsCaught(species.speciesId);
            bool shiny = SlimeBestiary.IsShinyCaught(species.speciesId);

            // 칸도 인벤토리와 같은 슬롯 틀을 쓴다. 안 잡은 종은 틀을 어둡게 눌러
            // 빈칸이라는 게 그림 없이도 읽히게 한다.
            GameObject cell = HudRoot.Slot("Cell_" + species.speciesId, _list);
            cell.GetComponent<Image>().color = caught ? Color.white : new Color(0.55f, 0.55f, 0.6f, 0.85f);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(cell.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -12f);
            iconRect.sizeDelta = new Vector2(112f, 112f);

            var icon = iconGo.GetComponent<Image>();
            icon.sprite = species.defaultSprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // 안 잡은 종은 그림을 검게 눌러 실루엣만 남긴다.
            icon.color = species.defaultSprite == null
                ? new Color(1f, 1f, 1f, 0f)
                : caught ? Color.white : new Color(0f, 0f, 0f, 0.85f);

            Text label = HudRoot.Label("Label", cell.transform, 22);
            label.alignment = TextAnchor.UpperCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 8f);
            labelRect.sizeDelta = new Vector2(-12f, 58f);
            label.text = caught ? CaughtText(species, shiny) : UnknownText(species);
            label.color = caught ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            return cell;
        }

        private static string CaughtText(SlimeSpecies species, bool shiny)
        {
            string origin = species.breedingOnly ? "교배 전용" : "야생";
            return $"{species.displayName}{(shiny ? " ★" : "")}\n{origin}";
        }

        // 안 잡은 종도 "어디서 나오는지" 만 알려준다. 아무 단서도 없으면 도감이
        // 목표가 아니라 빈칸 목록이 된다.
        private static string UnknownText(SlimeSpecies species)
        {
            return "???\n" + (species.breedingOnly ? "교배로만 등장" : "야생에 등장");
        }
    }
}

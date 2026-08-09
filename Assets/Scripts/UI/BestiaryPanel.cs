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
        public static void CloseIfOpen()
        {
            if (_instance != null && _instance._window.activeSelf)
            {
                _instance._window.SetActive(false);
            }
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

            Refresh();
        }

        private void BuildWindow()
        {
            _window = HudRoot.Panel("Window", transform, new Color(0.06f, 0.07f, 0.1f, 0.92f));
            var rect = _window.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(760f, 620f);

            _title = HudRoot.Label("Title", _window.transform, 30);
            _title.alignment = TextAnchor.MiddleLeft;
            var titleRect = _title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(24f, -70f);
            titleRect.offsetMax = new Vector2(-24f, -16f);

            var listGo = new GameObject("List", typeof(RectTransform));
            listGo.transform.SetParent(_window.transform, false);
            var listRect = listGo.GetComponent<RectTransform>();
            listRect.anchorMin = Vector2.zero;
            listRect.anchorMax = Vector2.one;
            listRect.offsetMin = new Vector2(24f, 24f);
            listRect.offsetMax = new Vector2(-24f, -80f);
            _list = listGo.transform;
        }

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

            _title.text = $"Bestiary   {SlimeBestiary.CaughtCount}/{SlimeBestiary.TotalSpecies} species" +
                $"   ·   {SlimeBestiary.ShinyCount} shiny";

            float y = 0f;
            foreach (SlimeSpecies species in catalog.Species)
            {
                if (species == null)
                {
                    continue;
                }

                _rows.Add(BuildRow(species, y));
                y -= 96f;
            }
        }

        private GameObject BuildRow(SlimeSpecies species, float y)
        {
            bool caught = SlimeBestiary.IsCaught(species.speciesId);
            bool shiny = SlimeBestiary.IsShinyCaught(species.speciesId);

            GameObject row = HudRoot.Panel("Row_" + species.speciesId, _list,
                caught ? new Color(1f, 1f, 1f, 0.06f) : new Color(1f, 1f, 1f, 0.02f));
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, y);
            rowRect.sizeDelta = new Vector2(0f, 88f);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(row.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(12f, 0f);
            iconRect.sizeDelta = new Vector2(72f, 72f);

            var icon = iconGo.GetComponent<Image>();
            icon.sprite = species.defaultSprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // 안 잡은 종은 그림을 검게 눌러 실루엣만 남긴다.
            icon.color = species.defaultSprite == null
                ? new Color(1f, 1f, 1f, 0f)
                : caught ? Color.white : new Color(0f, 0f, 0f, 0.75f);

            Text label = HudRoot.Label("Label", row.transform, 22);
            label.alignment = TextAnchor.MiddleLeft;
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(100f, 6f);
            labelRect.offsetMax = new Vector2(-12f, -6f);
            label.text = caught ? CaughtText(species, shiny) : UnknownText(species);
            label.color = caught ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            return row;
        }

        // 스탯 편향은 배율이라 "×1.4" 로 보여준다 — 종마다 절대 수치가 다르고
        // 티어 배율이 다시 곱해지므로, 절대값을 적으면 화면과 실제가 어긋난다.
        private static string CaughtText(SlimeSpecies species, bool shiny)
        {
            string origin = species.breedingOnly ? "breeding only" : "wild";
            string passive = species.passive == SpeciesPassiveKind.None
                ? "no passive"
                : $"{species.passive} r{species.passiveRadius:0.#}";

            return $"{species.displayName}{(shiny ? "  ★shiny" : "")}\n" +
                $"HP ×{species.maxHpMultiplier:0.##}  ATK ×{species.attackMultiplier:0.##}  " +
                $"DEF ×{species.defenseMultiplier:0.##}  SPD ×{species.speedMultiplier:0.##}\n" +
                $"{origin}  ·  {passive}";
        }

        // 안 잡은 종도 "어디서 나오는지" 만 알려준다. 아무 단서도 없으면 도감이
        // 목표가 아니라 빈칸 목록이 된다.
        private static string UnknownText(SlimeSpecies species)
        {
            return "???\n" +
                (species.breedingOnly
                    ? "Appears only from breeding"
                    : "Found in the wild") +
                "\nNot yet caught";
        }
    }
}

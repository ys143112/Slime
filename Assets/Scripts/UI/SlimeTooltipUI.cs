using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    /// <summary>슬라임 칸에 마우스를 올리면 뜨는 스탯 쪽지.</summary>
    /// <remarks>
    /// 슬롯을 격자로 좁히면서 칸 안에 글자를 넣을 자리가 없어졌다 — 어느
    /// 슬라임인지는 그림이, 숫자는 이 쪽지가 맡는다(사용자, 2026-08-09).
    ///
    /// <see cref="HudRoot"/> 캔버스(sortingOrder 100)에 올라가므로 인벤토리·교배
    /// 창보다 위에 그려진다. 창마다 쪽지를 따로 두면 두 창이 각자 다른 서식을
    /// 갖게 된다 — 하나만 만들어 둘이 같이 쓴다.
    /// </remarks>
    public sealed class SlimeTooltipUI : MonoBehaviour
    {
        private const float CursorOffset = 18f;

        private static SlimeTooltipUI _instance;

        private RectTransform _panel;
        private RectTransform _canvasRect;
        private Text _text;
        private SlimeInstance _shown;

        /// <summary>칸이 hover 를 알린다. 같은 개체를 다시 주면 위치만 따라간다.</summary>
        public static void Show(SlimeInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            SlimeTooltipUI tooltip = Ensure();
            if (tooltip._shown != instance)
            {
                tooltip._shown = instance;
                tooltip._text.text = Describe(instance);
                tooltip.Resize();
            }

            tooltip._panel.gameObject.SetActive(true);
        }

        /// <summary>이 개체를 보여주고 있을 때만 닫는다.</summary>
        /// <remarks>
        /// 칸 사이를 빠르게 지나가면 새 칸의 Enter 가 옛 칸의 Exit 보다 먼저 올 수
        /// 있다. 무조건 닫으면 그때 쪽지가 사라진 채로 남는다.
        /// </remarks>
        public static void Hide(SlimeInstance instance)
        {
            if (_instance == null || (instance != null && _instance._shown != instance))
            {
                return;
            }

            _instance._shown = null;
            _instance._panel.gameObject.SetActive(false);
        }

        private static SlimeTooltipUI Ensure()
        {
            if (_instance != null)
            {
                return _instance;
            }

            var go = new GameObject("SlimeTooltip", typeof(RectTransform), typeof(SlimeTooltipUI));
            go.transform.SetParent(HudRoot.Get(), false);
            HudRoot.Stretch(go.GetComponent<RectTransform>());
            _instance = go.GetComponent<SlimeTooltipUI>();
            return _instance;
        }

        private void Awake()
        {
            _canvasRect = (RectTransform)HudRoot.Get();

            GameObject panel = HudRoot.Panel("Panel", transform, new Color(0.05f, 0.06f, 0.09f, 0.94f));
            _panel = panel.GetComponent<RectTransform>();

            // 커서 오른쪽 아래로 펼친다 — 기준점을 왼쪽 위로 두면 위치 계산이
            // 커서 좌표 하나로 끝난다.
            _panel.anchorMin = new Vector2(0f, 0f);
            _panel.anchorMax = new Vector2(0f, 0f);
            _panel.pivot = new Vector2(0f, 1f);

            _text = HudRoot.Label("Text", panel.transform, 22);
            _text.alignment = TextAnchor.UpperLeft;

            var textRect = (RectTransform)_text.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 12f);
            textRect.offsetMax = new Vector2(-14f, -12f);

            panel.SetActive(false);
        }

        private void Update()
        {
            if (_shown == null || !_panel.gameObject.activeSelf)
            {
                return;
            }

            Follow();
        }

        // 마우스를 따라가되 화면 밖으로 안 나간다. 오른쪽·아래 끝에서는 커서
        // 반대편으로 접는다 — 안 그러면 쪽지의 절반이 화면 밖에 걸린다.
        private void Follow()
        {
            Vector2 screen = UnityEngine.InputSystem.Mouse.current != null
                ? UnityEngine.InputSystem.Mouse.current.position.ReadValue()
                : Vector2.zero;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, screen, null, out Vector2 local))
            {
                return;
            }

            // 캔버스 로컬 좌표는 가운데가 0 이고, 패널 앵커는 왼쪽 아래다.
            Vector2 canvasSize = _canvasRect.rect.size;
            Vector2 point = local + canvasSize * 0.5f;
            Vector2 size = _panel.sizeDelta;

            float x = point.x + CursorOffset;
            if (x + size.x > canvasSize.x)
            {
                x = point.x - CursorOffset - size.x;
            }

            float y = point.y - CursorOffset;
            if (y - size.y < 0f)
            {
                y = point.y + CursorOffset + size.y;
            }

            _panel.anchoredPosition = new Vector2(x, y);
        }

        private void Resize()
        {
            var textRect = (RectTransform)_text.transform;
            float padX = textRect.offsetMin.x - textRect.offsetMax.x;
            float padY = textRect.offsetMin.y - textRect.offsetMax.y;
            _panel.sizeDelta = new Vector2(_text.preferredWidth + padX, _text.preferredHeight + padY);
        }

        // 인벤토리 라벨에 있던 내용에 종 성격과 출신을 더한다 — 칸에서 글자를
        // 뺀 만큼 여기서 더 말해 준다.
        private static string Describe(SlimeInstance instance)
        {
            SlimeStatBlock stats = instance.baseStats;
            SlimeSpecies species = SlimeSpeciesCatalog.Lookup(instance.speciesId);

            string tags = string.Empty;
            if (instance.bossFlag) tags += "  [WANTED]";
            if (instance.shinyFlag) tags += "  [SHINY]";
            if (instance.mutantFlag) tags += "  [MUTANT]";
            if (instance.corruptedGeneFlag) tags += "  [CORRUPT]";

            bool companion = CompanionAgent.Active != null && CompanionAgent.Active.Instance == instance;

            string body = $"{SlimeSpeciesCatalog.DisplayName(instance.speciesId)}{tags}\n" +
                $"HP {instance.currentHp}/{stats.maxHp}\n" +
                $"ATK {stats.attack}   DEF {stats.defense}   SPD {stats.speed}";

            if (species != null && species.passive != SpeciesPassiveKind.None)
            {
                body += $"\nPassive {species.passive} (r{species.passiveRadius:0.#})";
            }

            if (!string.IsNullOrEmpty(instance.capturedBiomeId))
            {
                BiomeCatalog catalog = GameManager.Instance != null ? GameManager.Instance.Catalog : null;
                string origin = catalog != null
                    ? catalog.DisplayNameOf(instance.capturedBiomeId)
                    : instance.capturedBiomeId;
                body += $"\nFrom {origin}";
            }

            if (instance.weakened)
            {
                body += "\nWeakened";
            }

            if (companion)
            {
                body += "\nOut as companion  (click to recall)";
            }

            return body;
        }
    }
}

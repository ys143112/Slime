using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    /// <summary>화면에 늘 떠 있는 두 가지: 오른쪽 위 런 정보, 오른쪽 아래 조작 안내.</summary>
    /// <remarks>
    /// 둘 다 "설명 패널을 열지 않으면 알 수 없던 것" 이라 한 스크립트로 묶었다
    /// (팀 QA, 2026-08-09: 조작키를 화면에 표시 / 돌연변이 확률을 보이게).
    ///
    /// 조작 안내는 <c>H</c> 로 접는다 — 화면 구석을 계속 차지하면 익숙해진 뒤에는
    /// 방해가 된다. 접은 상태는 <see cref="PlayerPrefs"/> 에 남겨 다음 판까지
    /// 이어진다.
    ///
    /// 런 정보는 매 프레임이 아니라 값이 바뀔 때만 문자열을 다시 만든다 —
    /// 티어와 확률은 방을 옮길 때나 바뀐다.
    /// </remarks>
    public sealed class ScreenInfoHud : MonoBehaviour
    {
        private const string HintPrefsKey = "hud_hints_visible";

        private const string Hints =
            "WASD Move    Left click Attack    E Capture\n" +
            "I Inventory    B Satchel    U Breeding    J Bestiary\n" +
            "Z / X / C Rest area    Esc Pause    H Hide this";

        private static ScreenInfoHud _instance;

        private Text _runInfo;
        private Text _hints;

        // 켜고 끄는 대상은 글자가 아니라 판이다 — 글자만 끄면 검은 사각형이 남는다.
        private GameObject _hintsRoot;
        private string _lastRunInfo;

        public static void Show()
        {
            if (_instance != null)
            {
                return;
            }

            // RectTransform 으로 만들고 캔버스에 꽉 채워야 한다. 그냥 Transform
            // 이면 자식 앵커가 크기 0 짜리 부모를 기준으로 잡혀, 오른쪽 아래로
            // 붙인 것이 화면 한가운데에 뜬다(2026-08-09 실측).
            var go = new GameObject("ScreenInfoHud", typeof(RectTransform), typeof(ScreenInfoHud));
            go.transform.SetParent(HudRoot.Get(), false);
            HudRoot.Stretch(go.GetComponent<RectTransform>());
            _instance = go.GetComponent<ScreenInfoHud>();
        }

        private void Awake()
        {
            _runInfo = BuildCorner("RunInfo", new Vector2(1f, 1f), new Vector2(-24f, -24f),
                new Vector2(520f, 120f), TextAnchor.UpperRight, 24);

            _hints = BuildCorner("Hints", new Vector2(1f, 0f), new Vector2(-24f, 24f),
                new Vector2(760f, 110f), TextAnchor.LowerRight, 22);
            _hints.text = Hints;
            FitToText(_hints);
            _hintsRoot = _hints.transform.parent.gameObject;
            _hintsRoot.SetActive(PlayerPrefs.GetInt(HintPrefsKey, 1) == 1);
        }

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.hKey.wasPressedThisFrame)
            {
                bool visible = !_hintsRoot.activeSelf;
                _hintsRoot.SetActive(visible);
                PlayerPrefs.SetInt(HintPrefsKey, visible ? 1 : 0);
            }

            RefreshRunInfo();
        }

        private void RefreshRunInfo()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            string biomeId = GameManager.Instance.CurrentBiomeId;
            int tier = BiomeStigmaManager.Instance != null
                ? BiomeStigmaManager.Instance.GetCorruptionTier(biomeId)
                : 0;

            // 오염 티어가 곧 돌연변이 확률이라는 것이 이 두 줄의 요점이다.
            // 이로치 확률은 그 안에서 다시 굴리므로 곱해서 보여준다 — 따로 적으면
            // "5% 중 25%" 를 플레이어가 암산해야 한다.
            float mutant = MutantRollService.MutantChance(tier);
            float shiny = mutant * MutantRollService.ShinyChance;

            string next = $"{BiomeName(biomeId)}   Corruption T{tier}\n" +
                $"Mutant {mutant * 100f:0.#}%   Shiny {shiny * 100f:0.##}%\n" +
                $"Guaranteed mutant in {MutantRollService.PityRemaining} more catches";

            // 같은 문자열을 다시 넣으면 uGUI 가 메시를 다시 굽는다.
            if (next == _lastRunInfo)
            {
                return;
            }

            _lastRunInfo = next;
            _runInfo.text = next;
            FitToText(_runInfo);
        }

        /// <summary>배경판을 글자 크기에 맞춘다.</summary>
        /// <remarks>
        /// 판을 고정 폭으로 두면 오른쪽 정렬한 글자 왼쪽에 검은 여백이 길게
        /// 남는다. 글자가 바뀔 때마다 다시 재므로 확률 숫자 자릿수가 늘어도
        /// 판이 따라간다.
        /// </remarks>
        private static void FitToText(Text text)
        {
            var panel = (RectTransform)text.transform.parent;
            var textRect = (RectTransform)text.transform;

            // 글자 칸은 판 안쪽으로 들여 놓았다 — 그 여백을 도로 더해야 판이
            // 글자를 자르지 않는다.
            float padX = textRect.offsetMin.x - textRect.offsetMax.x;
            float padY = textRect.offsetMin.y - textRect.offsetMax.y;
            panel.sizeDelta = new Vector2(text.preferredWidth + padX, text.preferredHeight + padY);
        }

        private static string BiomeName(string biomeId)
        {
            BiomeCatalog catalog = GameManager.Instance != null ? GameManager.Instance.Catalog : null;
            return catalog != null ? catalog.DisplayNameOf(biomeId) : biomeId;
        }

        // 반투명 판을 부모로, 글자를 그 자식으로 둔다. uGUI 는 자식을 나중에
        // 그리므로 이 순서라야 판이 글자 뒤로 간다 — 반대로 하면 검은 사각형이
        // 글자를 덮는다. 접을 때도 부모 하나만 끄면 판까지 같이 사라진다.
        private Text BuildCorner(string name, Vector2 anchor, Vector2 position, Vector2 size,
            TextAnchor alignment, int fontSize)
        {
            GameObject backdrop = HudRoot.Panel(name, transform, new Color(0f, 0f, 0f, 0.4f));
            var backdropRect = backdrop.GetComponent<RectTransform>();
            backdropRect.anchorMin = anchor;
            backdropRect.anchorMax = anchor;
            backdropRect.pivot = anchor;
            backdropRect.anchoredPosition = position;
            backdropRect.sizeDelta = size;

            Text text = HudRoot.Label(name + "Text", backdrop.transform, fontSize);
            text.alignment = alignment;

            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;

            // 글자가 판 가장자리에 붙지 않게 안쪽으로 8픽셀 들인다.
            textRect.offsetMin = new Vector2(12f, 8f);
            textRect.offsetMax = new Vector2(-12f, -8f);
            return text;
        }
    }
}

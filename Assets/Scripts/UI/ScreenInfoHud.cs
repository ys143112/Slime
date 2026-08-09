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

        /// <summary>조작 안내 한 줄: 키캡 몇 개 + 무슨 일이 일어나는지.</summary>
        /// <remarks>
        /// 글자 한 덩어리("WASD Move ...")였던 것을 키 모양 아이콘으로 바꿨다
        /// (사용자, 2026-08-09: 첨부한 키보드 배치도처럼). 아이콘을 그림 자산으로
        /// 만들지 않고 uGUI 사각형 + 글자로 그리는 이유는 키가 11개고 문구가
        /// 바뀔 때마다 자산을 다시 뽑을 이유가 없어서다 — 배경·글자색만 바꾸면
        /// 키캡처럼 읽힌다.
        /// </remarks>
        private static readonly (string[] Keys, string Action)[] HintRows =
        {
            (new[] { "W", "A", "S", "D" }, "이동"),
            (new[] { "좌클릭" }, "공격"),
            (new[] { "E" }, "포획"),
            (new[] { "I" }, "인벤토리"),
            (new[] { "B" }, "런 배낭"),
            (new[] { "U" }, "교배"),
            (new[] { "J" }, "도감"),
            (new[] { "Z", "X", "C" }, "휴식소"),
            (new[] { "Esc" }, "일시정지"),
            (new[] { "H" }, "안내 접기"),
        };

        private const float HintRowHeight = 34f;
        private const float HintColumnWidth = 250f;
        private const int HintColumns = 2;

        private static ScreenInfoHud _instance;

        private Text _runInfo;

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

            _hintsRoot = BuildHintPad();
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

            string next = $"{BiomeName(biomeId)}   오염 T{tier}\n" +
                $"돌연변이 {mutant * 100f:0.#}%   이로치 {shiny * 100f:0.##}%\n" +
                $"{MutantRollService.PityRemaining}마리 더 잡으면 돌연변이 확정";

            // 같은 문자열을 다시 넣으면 uGUI 가 메시를 다시 굽는다.
            if (next == _lastRunInfo)
            {
                return;
            }

            _lastRunInfo = next;
            _runInfo.text = next;
            FitToText(_runInfo);
        }

        /// <summary>오른쪽 아래 조작 안내판. 두 칸짜리 격자에 키캡 줄을 늘어놓는다.</summary>
        private GameObject BuildHintPad()
        {
            int rows = Mathf.CeilToInt(HintRows.Length / (float)HintColumns);
            GameObject pad = HudRoot.Frame("Hints", transform);

            var padRect = pad.GetComponent<RectTransform>();
            padRect.anchorMin = new Vector2(1f, 0f);
            padRect.anchorMax = new Vector2(1f, 0f);
            padRect.pivot = new Vector2(1f, 0f);
            padRect.anchoredPosition = new Vector2(-24f, 24f);
            padRect.sizeDelta = new Vector2(HintColumnWidth * HintColumns + 40f, HintRowHeight * rows + 36f);

            var grid = pad.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(HintColumnWidth, HintRowHeight);

            // 창틀 테두리(위 16 / 나머지 10~12)를 피해 들인다.
            grid.padding = new RectOffset(20, 20, 20, 16);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = HintColumns;

            foreach ((string[] keys, string action) in HintRows)
            {
                BuildHintRow(pad.transform, keys, action);
            }

            return pad;
        }

        // 키캡을 왼쪽부터 붙이고 남은 자리에 설명을 쓴다. 가로 배치는
        // HorizontalLayoutGroup 이 아니라 x 를 직접 더한다 — 키캡 폭이 글자 수에
        // 따라 달라서(예: "좌클릭") 레이아웃 그룹에 넘겨도 결국 폭을 손으로 준다.
        private void BuildHintRow(Transform parent, string[] keys, string action)
        {
            var rowGo = new GameObject("Hint_" + action, typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);

            float x = 0f;
            foreach (string key in keys)
            {
                float width = 26f + Mathf.Max(0, key.Length - 1) * 12f;

                // 키캡은 자산 틀을 안 쓴다. 나무 슬롯 그림을 26px 칸에 넣으면
                // 테두리가 칸을 다 먹어 글자가 동그라미에 갇힌 것처럼 보였다
                // (사용자, 2026-08-09: 예전 게 낫다). 밝은 사각형 + 어두운 글자가
                // 실제 키보드 키캡과 같은 대비다.
                GameObject cap = HudRoot.Panel("Key_" + key, rowGo.transform,
                    new Color(0.92f, 0.92f, 0.86f, 0.95f));
                var capRect = cap.GetComponent<RectTransform>();
                capRect.anchorMin = new Vector2(0f, 0.5f);
                capRect.anchorMax = new Vector2(0f, 0.5f);
                capRect.pivot = new Vector2(0f, 0.5f);
                capRect.anchoredPosition = new Vector2(x, 0f);
                capRect.sizeDelta = new Vector2(width, 26f);

                Text capText = HudRoot.Label("Cap", cap.transform, 22);
                capText.alignment = TextAnchor.MiddleCenter;
                capText.color = new Color(0.1f, 0.1f, 0.12f);
                capText.text = key;
                var capTextRect = capText.GetComponent<RectTransform>();
                capTextRect.anchorMin = Vector2.zero;
                capTextRect.anchorMax = Vector2.one;
                capTextRect.offsetMin = Vector2.zero;
                capTextRect.offsetMax = Vector2.zero;

                x += width + 4f;
            }

            Text label = HudRoot.Label("Action", rowGo.transform, 22);
            label.alignment = TextAnchor.MiddleLeft;
            label.text = action;
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(x + 6f, 0f);
            labelRect.offsetMax = Vector2.zero;
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
            GameObject backdrop = HudRoot.Frame(name, transform);
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

            // 나무 창틀의 테두리 두께(왼 12 / 아래 10 / 오른 12 / 위 16, PPU 100)
            // 보다 넓게 들여야 글자가 테두리 위로 올라타지 않는다 — 예전 값(12/8)
            // 은 위쪽 테두리를 그대로 밟아 글자가 판을 벗어난 것처럼 보였다
            // (사용자, 2026-08-09).
            textRect.offsetMin = new Vector2(22f, 18f);
            textRect.offsetMax = new Vector2(-22f, -26f);
            return text;
        }
    }
}

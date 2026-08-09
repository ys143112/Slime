using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    /// <summary>
    /// 코드로 만드는 화면 고정 HUD 의 공용 뿌리. 여기 붙는 것들(동행 체력바,
    /// 공격 쿨다운)이 같은 캔버스·같은 기준 해상도를 쓰게 한다.
    /// </summary>
    /// <remarks>
    /// 씬에 캔버스를 두지 않는 이유는 Boot 씬 병합 사고를 피하기 위해서다. 그
    /// 씬의 <c>PersistentUICanvas</c> 는 아직 800×600 기준이라 나머지
    /// (1920×1080)와 크기도 어긋난다.
    /// </remarks>
    public static class HudRoot
    {
        private static Transform _root;

        public static Transform Get()
        {
            if (_root != null)
            {
                return _root;
            }

            var go = new GameObject("Hud", typeof(Canvas), typeof(CanvasScaler));
            Object.DontDestroyOnLoad(go);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // 인벤토리·교배 창보다 위에 그린다. 체력과 쿨다운은 창이 열려 있어도
            // 보여야 한다.
            canvas.sortingOrder = 100;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _root = go.transform;
            return _root;
        }

        /// <summary>스프라이트 없는 <c>Image</c> 는 흰 사각형으로 그려진다 — 막대 하나 때문에 자산을 만들 이유가 없다.</summary>
        public static GameObject Panel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            return go;
        }

        private const string ElementsPath = "UI/window_inventroy_UI_Farm_game-style_UI_a_style_fea/elements/";

        private static Sprite _windowSprite;
        private static Sprite _slotSprite;

        /// <summary>인벤토리·교배창과 같은 나무 창틀. 9-slice 라 크기를 아무렇게나 잡아도 된다.</summary>
        /// <remarks>
        /// 코드로 만든 HUD 는 색만 칠한 사각형이라 씬에 있는 창들과 톤이 달랐다
        /// (사용자, 2026-08-09: UI 에셋 안 쓴 것들 다 바꿔라). 자산을 못 찾으면
        /// 예전처럼 반투명 검정으로 떨어진다 — 창 하나 때문에 화면이 비면 안 된다.
        /// </remarks>
        public static GameObject Frame(string name, Transform parent)
        {
            return Sliced(name, parent, ref _windowSprite, "Window", new Color(0.06f, 0.07f, 0.1f, 0.92f));
        }

        /// <summary>슬롯 한 칸짜리 틀. 도감 칸·키캡처럼 작은 것에 쓴다(테두리가 얇다).</summary>
        public static GameObject Slot(string name, Transform parent)
        {
            return Sliced(name, parent, ref _slotSprite, "slot", new Color(1f, 1f, 1f, 0.08f));
        }

        private static GameObject Sliced(string name, Transform parent, ref Sprite cache,
            string fileName, Color fallback)
        {
            if (cache == null)
            {
                cache = Resources.Load<Sprite>(ElementsPath + fileName);
            }

            GameObject go = Panel(name, parent, cache != null ? Color.white : fallback);
            var image = go.GetComponent<UnityEngine.UI.Image>();
            if (cache != null)
            {
                image.sprite = cache;
                image.type = UnityEngine.UI.Image.Type.Sliced;
            }

            return go;
        }

        /// <summary>부모를 꽉 채운다. 코드로 만든 HUD 묶음의 뿌리에 반드시 걸어야 한다.</summary>
        /// <remarks>
        /// 묶음 오브젝트를 <c>RectTransform</c> 없이 만들거나 크기를 안 주면, 그
        /// 밑의 자식이 "화면 오른쪽 아래" 로 앵커를 잡아도 크기 0 인 부모 기준이
        /// 되어 화면 한가운데에 뜬다(2026-08-09 실측).
        /// </remarks>
        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>왼쪽 아래 구석 기준으로 자리를 잡는다. 해상도가 바뀌어도 안 밀린다.</summary>
        public static RectTransform PinBottomLeft(GameObject go, Vector2 position, Vector2 size)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Font _font;

        /// <summary>한글 글리프가 든 폰트. 없으면 빌트인으로 떨어진다.</summary>
        /// <remarks>
        /// 빌트인 <c>LegacyRuntime.ttf</c> 에는 한글이 없다. WebGL 에는 OS 폰트
        /// 폴백도 없어서, 이 자리에서 갈아 끼우지 않으면 한국어 HUD 가 빈칸으로
        /// 나온다(<see cref="HelpPanel"/> 이 같은 이유로 같은 파일을 읽는다).
        /// </remarks>
        private static Font UiFont()
        {
            if (_font == null)
            {
                _font = Resources.Load<Font>("Fonts/KoreanFont")
                    ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return _font;
        }

        public static Text Label(string name, Transform parent, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<Text>();

            // 폰트를 못 찾으면 글자만 비고 막대는 그대로 뜬다 — 표시가
            // 폰트 하나 때문에 통째로 사라지지 않게 한다.
            text.font = UiFont();
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.LowerLeft;
            text.raycastTarget = false;
            return text;
        }
    }
}

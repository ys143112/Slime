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

        public static Text Label(string name, Transform parent, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<Text>();

            // 빌트인 폰트를 못 찾으면 글자만 비고 막대는 그대로 뜬다 — 표시가
            // 폰트 하나 때문에 통째로 사라지지 않게 한다.
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.LowerLeft;
            text.raycastTarget = false;
            return text;
        }
    }
}

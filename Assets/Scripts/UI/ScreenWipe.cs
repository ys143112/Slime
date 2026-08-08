using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    /// <summary>씬이 바뀌는 동안 화면을 검게 쓸어 덮는 전환 연출.</summary>
    /// <remarks>
    /// 덮을 때는 왼쪽에서 오른쪽으로, 열 때는 오른쪽에서 왼쪽으로 지나간다
    /// (사용자 지정, 2026-08-08). 검은 판 하나를 왼쪽에서 밀어 넣었다가 <b>왔던
    /// 쪽으로</b> 도로 빼면 두 방향이 저절로 나온다 — 앞 가장자리가 들어올 땐
    /// 오른쪽으로, 나갈 땐 왼쪽으로 움직인다. 판을 오른쪽으로 계속 빼면 열리는
    /// 방향까지 왼쪽에서 오른쪽이 되어 지시와 어긋난다.
    ///
    /// <b>씬에 안 둔다.</b> 전환은 다섯 씬 어디서나 일어나므로 씬마다 캔버스를
    /// 놓으면 배선이 다섯 벌이 된다. 처음 필요할 때 코드가 만들고
    /// <c>DontDestroyOnLoad</c> 로 들고 다닌다.
    ///
    /// 시간은 <c>unscaledDeltaTime</c> 으로 센다 — Esc 일시정지가
    /// <c>Time.timeScale = 0</c> 을 걸어 두면 보통 시간으로는 코루틴이 그 자리에
    /// 멈춰 화면이 검은 채로 영영 남는다.
    /// </remarks>
    public sealed class ScreenWipe : MonoBehaviour
    {
        // 가장자리가 흐려지는 폭(px). 화면(960) 대비 1/4 쯤이라야 "그라데이션"
        // 으로 읽힌다 — 더 좁으면 그냥 검은 막대가 지나가는 것처럼 보인다.
        private const float RampWidth = 240f;

        // 열기가 덮기보다 길다. 열 때는 판이 화면 폭만큼 더 가야 하는데(덮을 때는
        // 화면을 채우면 끝, 열 때는 반대편으로 완전히 빠져야 한다) 같은 시간을 주면
        // 두 배 속도로 지나가 눈으로 못 쫓는다(사용자, 2026-08-08).
        private const float CoverSeconds = 0.45f;
        private const float RevealSeconds = 0.75f;

        private static ScreenWipe _instance;

        private RectTransform _canvasRect;
        private RectTransform _wipe;

        public static ScreenWipe Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Create();
                }

                return _instance;
            }
        }

        /// <summary>화면이 완전히 덮인 순간. 이때 뒤에서 무엇을 치워도 안 보인다.</summary>
        public event System.Action Covered;

        /// <summary>왼쪽에서 오른쪽으로 화면을 덮는다.</summary>
        public IEnumerator Cover()
        {
            Layout();
            yield return Slide(-Width(), -RampWidth, CoverSeconds);
            Covered?.Invoke();
        }

        /// <summary>덮을 때와 <b>같은 방향</b>으로, 계속 오른쪽으로 빠지며 화면을 연다.</summary>
        public IEnumerator Reveal()
        {
            Layout();
            yield return Slide(-RampWidth, ScreenWidth(), RevealSeconds);
            _wipe.gameObject.SetActive(false);
        }

        // 판 크기를 화면에 맞춘다.
        //
        // <b>캔버스 rect 를 안 믿는다.</b> 만든 직후에는 아직 레이아웃이 안 돌아
        // rect.width 가 0 으로 나오고, 그러면 판이 RampWidth(240) 폭짜리 띠가
        // 되어 나머지 화면에 카메라 배경(파랑)이 그대로 비친다 — 부트에서 목장으로
        // 넘어갈 때 뒤가 파랗게 보인 원인이 이것이다(사용자 신고, 2026-08-08).
        // 스케일러를 안 붙였으므로 캔버스 픽셀 = 화면 픽셀이라 Screen.width 가
        // 곧 정답이고, 시점과 무관하게 항상 옳다.
        private float ScreenWidth() => Mathf.Max(Screen.width, _canvasRect.rect.width);

        // 양 끝에 그라데이션 띠가 하나씩 붙는다. 열 때도 덮을 때와 같은 방향으로
        // 빠지려면 **뒷 가장자리에도** 띠가 있어야 한다 — 없으면 검은 판이
        // 직선으로 뚝 끊기며 사라진다(사용자, 2026-08-08).
        private float Width() => ScreenWidth() + RampWidth * 2f;

        private void Layout()
        {
            _wipe.sizeDelta = new Vector2(Width(), 0f);
            _wipe.gameObject.SetActive(true);
        }

        // 한 프레임에 인정하는 최대 시간. SceneManager.LoadScene 은 동기라 그
        // 프레임이 통째로 멈추는데, 그 멈춘 시간이 다음 프레임의
        // unscaledDeltaTime 에 그대로 실려 온다 — 로드가 길면 열기 코루틴의
        // 첫 프레임이 이미 지속시간을 넘겨 애니메이션 없이 뚝 사라졌다
        // (사용자 신고, 2026-08-08: "이동 후에 갑자기 검은색이 사라진다").
        private const float MaxFrameStep = 1f / 30f;

        private IEnumerator Slide(float from, float to, float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxFrameStep);
                float t = Mathf.Clamp01(elapsed / seconds);

                // 시작과 끝을 부드럽게. 등속이면 판이 "툭" 서는 것이 보인다.
                _wipe.anchoredPosition = new Vector2(Mathf.Lerp(from, to, t * t * (3f - 2f * t)), 0f);
                yield return null;
            }

            _wipe.anchoredPosition = new Vector2(to, 0f);
        }

        private static ScreenWipe Create()
        {
            var go = new GameObject("ScreenWipe");
            DontDestroyOnLoad(go);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // 다른 UI 를 전부 덮어야 한다 — 일시정지 메뉴나 인벤토리가 전환
            // 위에 떠 있으면 씬이 바뀌는 것이 안 보인다.
            canvas.sortingOrder = short.MaxValue;

            // 스케일러를 안 붙인다. 픽셀 단위 = 화면 픽셀이라 판 크기를 화면
            // 폭으로 그대로 잡을 수 있다(기준 해상도 환산이 필요 없다).
            var wipe = new GameObject("Wipe", typeof(RectTransform));
            wipe.transform.SetParent(go.transform, false);

            var rect = (RectTransform)wipe.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            AddSolid(rect);
            AddRamp(rect, leading: true);
            AddRamp(rect, leading: false);

            var component = go.AddComponent<ScreenWipe>();
            component._canvasRect = (RectTransform)go.transform;
            component._wipe = rect;
            wipe.SetActive(false);
            return component;
        }

        // 판의 왼쪽 대부분 — 완전한 검정. 오른쪽 RampWidth 만 비워 둔다.
        private static void AddSolid(RectTransform parent)
        {
            var go = new GameObject("Solid", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(RampWidth, 0f);
            rect.offsetMax = new Vector2(-RampWidth, 0f);

            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.black;
            image.raycastTarget = false;
        }

        // 가장자리 띠. 검정에서 투명으로 넘어가는 이 띠가 곧 그라데이션이다.
        // leading = 오른쪽(덮을 때 앞서는 쪽), 아니면 왼쪽(열 때 뒤따르는 쪽).
        private static void AddRamp(RectTransform parent, bool leading)
        {
            var go = new GameObject(leading ? "RampRight" : "RampLeft", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            float side = leading ? 1f : 0f;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(side, 0f);
            rect.anchorMax = new Vector2(side, 1f);
            rect.pivot = new Vector2(side, 0.5f);
            rect.sizeDelta = new Vector2(RampWidth, 0f);
            rect.anchoredPosition = Vector2.zero;

            var raw = go.AddComponent<RawImage>();
            raw.texture = GradientTexture();
            raw.raycastTarget = false;

            // 왼쪽 띠는 같은 텍스처를 좌우로 뒤집어 쓴다 — 검정이 본체 쪽,
            // 투명이 바깥 쪽이라는 관계가 양쪽에서 같아야 한다.
            //
            // **localScale 로 뒤집지 않는다.** 피벗이 왼쪽 끝이라 스케일 -1 은
            // 띠를 제자리에서 뒤집는 게 아니라 피벗 **바깥으로** 옮겨 버린다 —
            // 원래 자리에 투명한 구멍이 남는다. UV 를 뒤집으면 사각형은 그대로다.
            if (!leading)
            {
                raw.uvRect = new Rect(1f, 0f, -1f, 1f);
            }
        }

        private static Texture2D GradientTexture()
        {
            const int width = 64;
            var texture = new Texture2D(width, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            for (int x = 0; x < width; x++)
            {
                // 왼쪽 끝(=판 본체와 맞닿는 쪽)이 불투명, 오른쪽 끝이 투명.
                float alpha = 1f - x / (width - 1f);
                texture.SetPixel(x, 0, new Color(0f, 0f, 0f, alpha));
            }

            texture.Apply();
            return texture;
        }
    }
}

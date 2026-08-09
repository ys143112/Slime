using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    /// <summary>시작 화면(과 일시정지 메뉴)에 바로 붙는 배경음·효과음 슬라이더.</summary>
    /// <remarks>
    /// 볼륨이 설정 패널 안에만 있어서, 소리를 줄이려면 설정을 열고 다시 닫아야
    /// 했다 — 메인 화면에서 바로 만지게 해 달라는 요청(팀 QA, 2026-08-09).
    ///
    /// 씬을 고치지 않고 코드로 만드는 이유: Boot 씬은 이미 한 번 병합 사고로
    /// UI 가 통째로 사라진 적이 있고(CLAUDE.md 「씬 병합 함정」), 이 정도 위젯은
    /// 자산이 필요 없다. <see cref="SettingsPanel"/> 의 슬라이더와 같은
    /// <see cref="AudioManager"/> 값을 읽고 쓰므로 둘은 저절로 같은 값을 가리킨다.
    /// </remarks>
    public sealed class MainVolumeControls : MonoBehaviour
    {
        private const float RowHeight = 46f;

        private Slider _bgm;
        private Slider _sfx;

        public static MainVolumeControls Build(Transform parent)
        {
            var go = new GameObject("MainVolumeControls", typeof(RectTransform), typeof(MainVolumeControls));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();

            // 화면 왼쪽 아래 구석. 가운데 버튼 기둥과 겹치지 않는 유일한 빈 자리다.
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(48f, 48f);
            rect.sizeDelta = new Vector2(420f, RowHeight * 2f);

            var controls = go.GetComponent<MainVolumeControls>();
            controls._bgm = controls.BuildRow("Bgm", "BGM", RowHeight, OnBgmChanged);
            controls._sfx = controls.BuildRow("Sfx", "SFX", 0f, OnSfxChanged);
            controls.Pull();
            return controls;
        }

        /// <summary>설정 패널에서 값을 바꾸고 돌아왔을 수도 있다 — 열 때마다 되읽는다.</summary>
        private void OnEnable()
        {
            Pull();
        }

        private void Pull()
        {
            // OnEnable 은 컴포넌트가 붙는 순간(=new GameObject) 이미 한 번 돈다.
            // 그때는 Build 가 아직 슬라이더를 만들기 전이라 둘 다 null 이다.
            if (AudioManager.Instance == null || _bgm == null || _sfx == null)
            {
                return;
            }

            // SetValueWithoutNotify 라 되읽기가 다시 쓰기를 부르지 않는다.
            _bgm.SetValueWithoutNotify(AudioManager.Instance.BgmVolume);
            _sfx.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
        }

        private static void OnBgmChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.BgmVolume = value;
            }
        }

        private static void OnSfxChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SfxVolume = value;
            }
        }

        private Slider BuildRow(string name, string caption, float y, UnityEngine.Events.UnityAction<float> onChanged)
        {
            Text label = HudRoot.Label(name + "Label", transform, 24);
            label.alignment = TextAnchor.MiddleLeft;
            var labelRect = label.GetComponent<RectTransform>();
            Pin(labelRect, new Vector2(0f, y), new Vector2(90f, RowHeight));
            label.text = caption;

            var sliderGo = new GameObject(name + "Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(transform, false);
            Pin(sliderGo.GetComponent<RectTransform>(), new Vector2(100f, y + 12f), new Vector2(300f, 22f));

            GameObject background = HudRoot.Panel("Background", sliderGo.transform, new Color(0f, 0f, 0f, 0.55f));
            HudRoot.Stretch(background.GetComponent<RectTransform>());

            GameObject fillArea = HudRoot.Panel("Fill", sliderGo.transform, new Color(0.55f, 0.85f, 1f, 0.95f));
            HudRoot.Stretch(fillArea.GetComponent<RectTransform>());

            // 손잡이는 레이캐스트를 받아야 끌 수 있다 — Panel 은 raycastTarget 을
            // 꺼서 만들므로 여기서 다시 켠다.
            GameObject handle = HudRoot.Panel("Handle", sliderGo.transform, Color.white);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(18f, 30f);
            handle.GetComponent<Image>().raycastTarget = true;

            var slider = sliderGo.GetComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.fillRect = fillArea.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.onValueChanged.AddListener(onChanged);

            // 배경이 클릭을 먹어야 막대 아무 데나 눌러 값이 바뀐다.
            background.GetComponent<Image>().raycastTarget = true;
            return slider;
        }

        private static void Pin(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

    }
}

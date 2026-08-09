using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: Boot 시작 화면 겸 게임 중 일시정지 메뉴. 시작하기/게임 설명/설정/
    // 나가기 버튼을 붙인다. 인스펙터 배선 없이 자식 이름으로 직접 찾는다 - MCP 로
    // 씬을 만들 때 오브젝트 참조 필드 배선이 안 먹혀 이 방식으로 바꿨다(2026-08-06).
    //
    // 시작 화면과 일시정지 메뉴가 같은 오브젝트인 이유: 버튼 셋(설명·설정·나가기)이
    // 똑같다. 따로 만들면 문구·스킨·배선을 두 벌 유지하게 된다. 게임을 시작하면
    // 캔버스를 통째로 숨겼다가 Esc 로 다시 꺼내고, 그때는 시작하기만 감춘다.
    public sealed class BootMenuUI : MonoBehaviour
    {
        // 클립이 아직 없다. 넣으면 세 버튼 모두 이 소리를 낸다.
        [SerializeField] private AudioClip clickSfx;

        private GameObject _settingsPanel;
        private GameObject _helpPanel;
        private GameObject _background;
        private GameObject _buttons;
        private GameObject _startButton;
        private MainVolumeControls _volume;
        private Text _pausedLabel;
        private bool _inGame;
        private bool _menuOpen = true;

        private void Awake()
        {
            // 씬을 넘어가도 살아남아야 Esc 메뉴가 목장·바이옴에서도 열린다.
            // 이게 없으면 Boot 을 떠나는 순간 게임을 끌 방법이 사라진다.
            DontDestroyOnLoad(gameObject);

            _settingsPanel = FindDeep("SettingsPanel").gameObject;
            _settingsPanel.SetActive(false);

            // 설명은 켜 둔 채로 시작한다 — 처음 켠 사람이 조작키를 어디서 보는지
            // 모른 채 목장에 떨어지는 것이 지금까지의 기본값이었다. 닫으면
            // How to Play 버튼으로 다시 연다.
            _helpPanel = FindDeep("HelpPanel").gameObject;
            _helpPanel.SetActive(true);

            Transform background = FindDeep("Background");
            _background = background != null ? background.gameObject : null;

            Transform buttons = FindDeep("Buttons");
            _buttons = buttons != null ? buttons.gameObject : null;

            Transform start = FindDeep("StartButton");
            _startButton = start != null ? start.gameObject : null;

            Wire("StartButton", OnStart);
            Wire("HelpButton", OnHelp);
            Wire("SettingsButton", OnSettings);
            Wire("QuitButton", OnQuit);

            // 볼륨은 설정 안이 아니라 이 화면에도 있어야 한다(팀 QA, 2026-08-09).
            // 버튼 묶음과 같은 부모에 붙여 메뉴와 함께 켜지고 꺼지게 한다.
            Transform host = _buttons != null ? _buttons.transform.parent : transform;
            _volume = MainVolumeControls.Build(host);

            _pausedLabel = BuildPausedLabel(host);
        }

        // 일시정지가 이미 되고 있는데 화면에 아무 말이 없어 "멈춘 건지 멈춘 척인지"
        // 알 수 없었다(팀 QA, 2026-08-09). 게임 중에 메뉴가 떠 있을 때만 띄운다.
        private static Text BuildPausedLabel(Transform parent)
        {
            Text text = HudRoot.Label("PausedLabel", parent, 33);
            text.text = "일시정지  -  Esc 로 계속";
            text.alignment = TextAnchor.UpperCenter;

            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -40f);
            rect.sizeDelta = new Vector2(700f, 50f);

            text.gameObject.SetActive(false);
            return text;
        }

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (!Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            // 창이 떠 있으면 그 창부터 닫는다. 예전엔 Esc 가 늘 일시정지 메뉴를
            // 열어, 인벤토리를 열어 둔 채 Esc 를 누르면 창 위에 메뉴가 겹쳤다
            // (사용자, 2026-08-09). 창을 닫는 키는 창마다 다르고(I/U/J) 그걸
            // 외우게 하는 대신 Esc 하나로 통일한다.
            if (CloseTopmostPanel())
            {
                return;
            }

            // 타이틀 화면에서는 메뉴가 곧 화면이라 Esc 로 끄면 안 된다 — 창을
            // 닫는 일만 하고 빠진다.
            if (!_inGame)
            {
                return;
            }

            ShowMenu(!_menuOpen);
        }

        /// <summary>떠 있는 창 하나를 닫는다. 닫을 게 없으면 false.</summary>
        /// <remarks>
        /// 창끼리는 이미 상호 배타라 동시에 둘이 뜨지 않는다 — 순서는 "먼저 닫히는
        /// 쪽" 을 정하는 게 아니라 어느 하나를 찾는 일이다. 설명 패널은 게임 중에도
        /// 버튼으로 열리므로 같이 본다.
        /// </remarks>
        private bool CloseTopmostPanel()
        {
            if (_helpPanel != null && _helpPanel.activeSelf)
            {
                _helpPanel.SetActive(false);
                return true;
            }

            if (_settingsPanel != null && _settingsPanel.activeSelf)
            {
                _settingsPanel.SetActive(false);
                return true;
            }

            if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
            {
                InventoryUI.Instance.Close();
                return true;
            }

            if (BreedingUIPanel.Instance != null && BreedingUIPanel.Instance.IsOpen)
            {
                BreedingUIPanel.Instance.Close();
                return true;
            }

            if (SatchelCounterUI.Instance != null && SatchelCounterUI.Instance.IsOpen)
            {
                SatchelCounterUI.Instance.Close();
                return true;
            }

            return BestiaryPanel.CloseIfOpen();
        }

        private void ShowMenu(bool visible)
        {
            _menuOpen = visible;

            if (_background != null)
            {
                // 게임 중에는 타이틀 배경 그림을 안 깐다 — 목장이 안 보이면
                // 일시정지가 아니라 화면이 넘어간 것처럼 읽힌다.
                _background.SetActive(visible && !_inGame);
            }

            if (_buttons != null)
            {
                _buttons.SetActive(visible);
            }

            if (_startButton != null)
            {
                _startButton.SetActive(!_inGame);
            }

            if (_volume != null)
            {
                _volume.gameObject.SetActive(visible);
            }

            if (_pausedLabel != null)
            {
                _pausedLabel.gameObject.SetActive(visible && _inGame);
            }

            if (!visible)
            {
                _helpPanel.SetActive(false);
                _settingsPanel.SetActive(false);
            }

            // 메뉴가 떠 있는 동안 슬라임이 계속 때리면 "잠깐 멈추는" 것이 아니다.
            Time.timeScale = visible && _inGame ? 0f : 1f;
        }

        private void Wire(string childName, UnityEngine.Events.UnityAction action)
        {
            Transform child = FindDeep(childName);
            if (child == null)
            {
                Debug.LogError($"BootMenuUI: '{childName}' 을 찾지 못해 배선을 건너뜁니다.");
                return;
            }

            child.GetComponent<Button>().onClick.AddListener(action);
        }

        // 이름으로 손자까지 훑는다. 버튼을 Buttons 같은 묶음 오브젝트 밑으로
        // 옮기면 직계 자식만 보는 transform.Find 는 null 을 내고, 그 자리에서
        // NullReferenceException 이 나 시작 화면 전체가 죽는다.
        private Transform FindDeep(string childName)
        {
            foreach (Transform candidate in GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == childName)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void PlayClick()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx(clickSfx);
            }
        }

        private void OnStart()
        {
            PlayClick();
            _inGame = true;

            // 시작 화면을 **지금** 치우지 않는다. 예전엔 여기서 바로 껐는데,
            // 화면 전환이 아직 덮기 전이라 그 사이에 배경 그림이 사라진 빈 화면
            // (카메라 클리어 = 파란색)이 그라데이션 틈으로 비쳤다 — 부트에서
            // 목장으로 넘어갈 때만 파랗게 보인 원인이다(사용자 신고, 2026-08-08).
            // 완전히 덮인 뒤에 치우면 무엇이 사라지든 안 보인다.
            ScreenWipe.Instance.Covered += HideOnceCovered;
            GameManager.Instance.BeginGame();
        }

        private void HideOnceCovered()
        {
            ScreenWipe.Instance.Covered -= HideOnceCovered;
            ShowMenu(false);
        }

        // 두 패널은 같은 자리를 쓴다. 서로 닫아 주지 않으면 겹쳐서 뜬다.
        private void OnHelp()
        {
            PlayClick();
            _settingsPanel.SetActive(false);
            _helpPanel.SetActive(true);
        }

        private void OnSettings()
        {
            PlayClick();
            _helpPanel.SetActive(false);
            _settingsPanel.SetActive(true);
        }

        private void OnQuit()
        {
            PlayClick();

            // 일시정지로 0 이 된 채 나가면 다음 Play 가 멈춘 상태로 시작한다.
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

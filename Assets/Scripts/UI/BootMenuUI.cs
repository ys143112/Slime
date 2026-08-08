using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: Boot 시작 화면. 시작하기/설정/종료 버튼을 붙인다. 인스펙터 배선 없이
    // 자식 이름으로 직접 찾는다 - MCP 로 씬을 만들 때 오브젝트 참조 필드
    // 배선이 안 먹혀 이 방식으로 바꿨다(2026-08-06).
    public sealed class BootMenuUI : MonoBehaviour
    {
        // 클립이 아직 없다. 넣으면 세 버튼 모두 이 소리를 낸다.
        [SerializeField] private AudioClip clickSfx;

        private GameObject _settingsPanel;
        private GameObject _helpPanel;

        private void Awake()
        {
            _settingsPanel = transform.Find("SettingsPanel").gameObject;
            _settingsPanel.SetActive(false);

            // 설명은 켜 둔 채로 시작한다 — 처음 켠 사람이 조작키를 어디서 보는지
            // 모른 채 목장에 떨어지는 것이 지금까지의 기본값이었다. 닫으면
            // How to Play 버튼으로 다시 연다.
            _helpPanel = transform.Find("HelpPanel").gameObject;
            _helpPanel.SetActive(true);

            transform.Find("StartButton").GetComponent<Button>().onClick.AddListener(OnStart);
            transform.Find("HelpButton").GetComponent<Button>().onClick.AddListener(OnHelp);
            transform.Find("SettingsButton").GetComponent<Button>().onClick.AddListener(OnSettings);
            transform.Find("QuitButton").GetComponent<Button>().onClick.AddListener(OnQuit);
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
            GameManager.Instance.BeginGame();
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
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

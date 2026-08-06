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

        private void Awake()
        {
            _settingsPanel = transform.Find("SettingsPanel").gameObject;
            _settingsPanel.SetActive(false);

            transform.Find("StartButton").GetComponent<Button>().onClick.AddListener(OnStart);
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

        private void OnSettings()
        {
            PlayClick();
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

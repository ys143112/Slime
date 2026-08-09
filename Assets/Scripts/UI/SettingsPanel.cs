using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: 설정 패널 - 배경음/효과음 조절. 자식은 인스펙터 배선 대신 이름으로
    // 직접 찾는다(BootMenuUI 와 같은 이유). 조작키·게임 설명은 HelpPanel 로
    // 옮겼다 — 볼륨 하나 만지러 들어온 사람에게 규칙 설명을 같이 들이밀 이유가
    // 없다.
    public sealed class SettingsPanel : MonoBehaviour
    {
        private Slider _bgmSlider;
        private Slider _sfxSlider;

        private void Awake()
        {
            _bgmSlider = transform.Find("BgmSlider").GetComponent<Slider>();
            _sfxSlider = transform.Find("SfxSlider").GetComponent<Slider>();
            _bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
            _sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            transform.Find("CloseButton").GetComponent<Button>().onClick.AddListener(Close);
        }

        private void OnEnable()
        {
            if (AudioManager.Instance == null)
            {
                return;
            }

            _bgmSlider.SetValueWithoutNotify(AudioManager.Instance.BgmVolume);
            _sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
        }

        private void OnBgmVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.BgmVolume = value;
            }
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SfxVolume = value;
            }
        }

        private void Close()
        {
            gameObject.SetActive(false);
        }
    }
}

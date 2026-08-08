using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: 설정 패널 - 배경음/효과음 조절, 조작키 설명, 게임 설명. 자식은
    // 인스펙터 배선 대신 이름으로 직접 찾는다(BootMenuUI 와 같은 이유).
    public sealed class SettingsPanel : MonoBehaviour
    {
        // 조작·설명 문구는 씬이 아니라 여기에 둔다. 씬의 Text 에 직접 적어 두면
        // 키가 바뀔 때마다 씬을 열어 고쳐야 하고, 그때마다 .unity 가 더러워져
        // 병합 사고 위험이 생긴다. 이 패널은 씬에 비활성으로 저장돼 있어
        // MCP 도구로는 손도 못 댄다 — 코드에서 채우면 그 제약도 비껴간다.
        private const string ControlsBody =
            "WASD  이동\n" +
            "Space  공격 (쿨다운 0.5초, 화면 왼쪽 아래 막대로 확인)\n" +
            "E  포획 (약화된 슬라임만)\n" +
            "I  인벤토리 — 슬롯을 누르면 그 슬라임이 동행으로 나간다\n" +
            "B  런 배낭 (다이브 중 잡은 것)\n" +
            "U  교배 창 (목장에서만)\n" +
            "Q / F / G  교배장: 선택 전환 / 내놓기 / 되돌리기\n" +
            "Z / X / C  목장 작업장: 선택 전환 / 배치 / 회수";

        private const string DescriptionBody =
            "바이옴에 다이브해 야생 슬라임을 약화시키고(Space) 잡는다(E).\n" +
            "잡은 슬라임은 곧바로 내 것이 아니다 — 런 배낭(B)에 쌓이고, 추출 지점까지\n" +
            "살아서 돌아와야 보유 목록에 들어간다. 도중에 죽으면 전부 몰수다.\n" +
            "\n" +
            "동행: 인벤토리에서 슬라임 한 마리를 데리고 다닐 수 있다(한 마리까지).\n" +
            "같이 싸워 주지만 쓰러지면 보유 목록에서 영구히 사라진다.\n" +
            "\n" +
            "목장 작업장: 슬라임을 배치하면(X) 주기마다 자원을 낸다. 배치된 동안에는\n" +
            "보유 목록에서 빠져 교배·동행에 못 쓴다. 회수하면(C) 체력이 차서 돌아온다\n" +
            "— 지금은 이것이 유일한 회복 수단이다.\n" +
            "\n" +
            "교배장: 두 마리를 내놓으면(F) 알이 생기고, 시간이 지나면 부화한다.\n" +
            "자손은 부모 중 한쪽의 종을 물려받고 스탯은 그 종의 성격을 따른다.\n" +
            "오염된 바이옴일수록 돌연변이(스탯 반전)와 이로치가 잘 나온다.";

        private Slider _bgmSlider;
        private Slider _sfxSlider;

        private void Awake()
        {
            _bgmSlider = transform.Find("BgmSlider").GetComponent<Slider>();
            _sfxSlider = transform.Find("SfxSlider").GetComponent<Slider>();
            _bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
            _sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            transform.Find("CloseButton").GetComponent<Button>().onClick.AddListener(Close);

            SetText("ControlsText", ControlsBody);
            SetText("DescriptionText", DescriptionBody);
        }

        // 칸이 없어도 조용히 넘어간다 — 문구 하나 때문에 설정 패널 전체가
        // 안 열리면 볼륨 조절까지 같이 막힌다.
        private void SetText(string childName, string body)
        {
            Transform child = transform.Find(childName);
            if (child == null)
            {
                return;
            }

            Text legacy = child.GetComponent<Text>();
            if (legacy != null)
            {
                legacy.text = body;
                return;
            }

            var tmp = child.GetComponent<TMPro.TMP_Text>();
            if (tmp != null)
            {
                tmp.text = body;
            }
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

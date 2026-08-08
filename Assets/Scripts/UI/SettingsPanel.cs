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
            "WASD  Move\n" +
            "Space  Attack (0.5s cooldown, bar at bottom left)\n" +
            "E  Capture (weakened slimes only)\n" +
            "I  Inventory - click a slot to send that slime out as companion\n" +
            "B  Run satchel (what you caught this dive)\n" +
            "U  Breeding window (ranch only) - pick two, press Breed\n" +
            "Z / X / C  Rest area: switch / assign / collect";

        private const string DescriptionBody =
            "Dive into a biome, weaken wild slimes (Space) and capture them (E).\n" +
            "A captured slime is not yours yet - it goes into the run satchel (B),\n" +
            "and only reaches your roster if you walk out through the extraction\n" +
            "point alive. Die on the way and you lose the whole satchel.\n" +
            "\n" +
            "Companion: one slime from the inventory can follow you.\n" +
            "It fights with you, but if it falls it is gone from the roster forever.\n" +
            "\n" +
            "Rest area: assign a hurt slime (X) and it heals 10% of max HP every\n" +
            "2 seconds (about 20s from zero). Collect it when healed (C). While it\n" +
            "rests it leaves the roster and cannot breed or follow - this is the\n" +
            "only way to heal.\n" +
            "\n" +
            "Breeding pen: press U, pick two slimes, an egg appears and hatches\n" +
            "after a while. The child inherits one parent's species and takes that\n" +
            "species' stat bias. The more corrupted the biome, the more mutants\n" +
            "(inverted stats) and shinies you get.";

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
            // 이름으로 손자까지 훑는다. 본문은 ScrollView/Content 밑으로 들어가
            // 직계 자식이 아니고, 앞으로 한 겹 더 감싸도 배선이 안 끊긴다.
            Transform child = null;
            foreach (Transform candidate in GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == childName)
                {
                    child = candidate;
                    break;
                }
            }

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

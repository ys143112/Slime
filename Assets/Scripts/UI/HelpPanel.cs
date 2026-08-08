using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: 게임 설명 패널 - 조작키와 규칙. 부팅하면 한 번 저절로 뜨고, 닫은
    // 뒤에는 시작 화면의 "How to Play" 버튼으로 다시 연다. 원래 설정 패널
    // 안에 있었는데, 볼륨 조절하러 들어간 사람에게 규칙 설명을 같이 들이미는
    // 구성이라 갈랐다.
    public sealed class HelpPanel : MonoBehaviour
    {
        // 문구는 씬이 아니라 여기에 둔다. 씬의 Text 에 직접 적으면 키가 바뀔
        // 때마다 .unity 가 더러워져 병합 사고 위험이 생긴다.
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

        private void Awake()
        {
            Transform close = FindDeep("CloseButton");
            if (close != null)
            {
                close.GetComponent<Button>().onClick.AddListener(Close);
            }

            SetText("ControlsText", ControlsBody);
            SetText("DescriptionText", DescriptionBody);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        // 이름으로 손자까지 훑는다. 본문은 ScrollView/Content 밑이라 직계 자식이
        // 아니고, 앞으로 한 겹 더 감싸도 배선이 안 끊긴다.
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

        // 칸이 없어도 조용히 넘어간다 — 문구 하나 때문에 패널이 안 열리면
        // 닫기 버튼까지 같이 막힌다.
        private void SetText(string childName, string body)
        {
            Transform child = FindDeep(childName);
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
    }
}

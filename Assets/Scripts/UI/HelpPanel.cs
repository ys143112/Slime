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

        private const string ControlsBodyKo =
            "WASD  이동\n" +
            "Space  공격 (쿨다운 0.5초, 화면 왼쪽 아래 막대로 확인)\n" +
            "E  포획 (약화된 슬라임만)\n" +
            "I  인벤토리 — 슬롯을 누르면 그 슬라임이 동행으로 나간다\n" +
            "B  런 배낭 (다이브 중 잡은 것)\n" +
            "U  교배 창 (목장에서만) — 두 마리를 골라 교배 버튼\n" +
            "Z / X / C  휴식소: 선택 전환 / 눕히기 / 회수";

        private const string DescriptionBodyKo =
            "바이옴에 다이브해 야생 슬라임을 약화시키고(Space) 잡는다(E).\n" +
            "잡은 슬라임은 곧바로 내 것이 아니다 — 런 배낭(B)에 쌓이고, 추출 지점까지\n" +
            "살아서 돌아와야 보유 목록에 들어간다. 도중에 죽으면 전부 몰수다.\n" +
            "\n" +
            "동행: 인벤토리에서 슬라임 한 마리를 데리고 다닐 수 있다(한 마리까지).\n" +
            "같이 싸워 주지만 쓰러지면 보유 목록에서 영구히 사라진다.\n" +
            "\n" +
            "휴식소: 다친 슬라임을 눕히면(X) 2초마다 최대 체력의 10%씩 낫는다\n" +
            "(0에서 완치까지 약 20초). 다 나으면 회수한다(C). 눕혀 둔 동안에는 보유\n" +
            "목록에서 빠져 교배·동행에 못 쓴다 — 이것이 유일한 회복 수단이다.\n" +
            "\n" +
            "교배장: U 로 창을 열어 두 마리를 고르면 알이 생기고, 시간이 지나면 부화한다.\n" +
            "자손은 부모 중 한쪽의 종을 물려받고 스탯은 그 종의 성격을 따른다.\n" +
            "오염된 바이옴일수록 돌연변이(스탯 반전)와 이로치가 잘 나온다.";

        // 한글 글리프가 든 폰트. 기본 폰트(Kenney Pixel)에도 내장 Arial 에도
        // 한글이 없어서, 이게 없으면 한국어를 켜 봐야 빈칸만 나온다. WebGL 에는
        // OS 폰트 폴백도 없다. 파일이 없으면 언어 버튼 자체를 숨긴다.
        private const string KoreanFontResource = "Fonts/KoreanFont";

        private Font _koreanFont;
        private Font _defaultFont;
        private bool _korean;
        private Text _langLabel;

        private void Awake()
        {
            Transform close = FindDeep("CloseButton");
            if (close != null)
            {
                close.GetComponent<Button>().onClick.AddListener(Close);
            }

            _koreanFont = Resources.Load<Font>(KoreanFontResource);

            Transform lang = FindDeep("LanguageButton");
            if (lang != null)
            {
                if (_koreanFont == null)
                {
                    lang.gameObject.SetActive(false);
                }
                else
                {
                    _langLabel = lang.GetComponentInChildren<Text>(true);
                    lang.GetComponent<Button>().onClick.AddListener(ToggleLanguage);
                }
            }

            Text body = FindDeep("DescriptionText") != null
                ? FindDeep("DescriptionText").GetComponent<Text>()
                : null;
            _defaultFont = body != null ? body.font : null;

            ApplyLanguage();
        }

        private void ToggleLanguage()
        {
            _korean = !_korean;
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            SetText("ControlsText", _korean ? ControlsBodyKo : ControlsBody);
            SetText("DescriptionText", _korean ? DescriptionBodyKo : DescriptionBody);

            // 제목까지 바꾸면 한글 폰트가 없을 때 제목만 빈칸이 된다 — 본문
            // 두 칸만 폰트를 갈아 끼운다.
            Font font = _korean ? _koreanFont : _defaultFont;
            SetFont("ControlsText", font);
            SetFont("DescriptionText", font);

            if (_langLabel != null)
            {
                // 버튼은 "누르면 무엇이 되는지" 를 보여준다. 한국어일 때 영어로
                // 적혀 있어야 영어로 돌아갈 수 있다는 것이 읽힌다.
                _langLabel.text = _korean ? "English" : "KOR";
            }
        }

        private void SetFont(string childName, Font font)
        {
            if (font == null)
            {
                return;
            }

            Transform child = FindDeep(childName);
            Text text = child != null ? child.GetComponent<Text>() : null;
            if (text != null)
            {
                text.font = font;
            }
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

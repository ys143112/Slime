using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Gameplay.Tests
{
    // 기능: spec-011 검증 (런 전리품, 사망 몰수, 추출 정산)
    public sealed class RunSatchelTests
    {
        private const string RosterSaveKey = "player_roster";

        // Hub 씬 지오메트리와 섞이지 않게 멀리 떨어진 곳에서 논다.
        private static readonly Vector3 Arena = new Vector3(1000f, 1000f, 0f);

        private string _rosterSavePath;
        private byte[] _rosterBackup;
        private bool _hadRosterSave;

        private GameObject _rosterObject;
        private GameObject _managerObject;
        private GameObject _player;
        private GameObject _slime;
        private GameObject _counterObject;

        [SetUp]
        public void SetUp()
        {
            _rosterSavePath = Path.Combine(Application.persistentDataPath, "Saves", RosterSaveKey + ".json");
            _hadRosterSave = File.Exists(_rosterSavePath);
            if (_hadRosterSave)
            {
                _rosterBackup = File.ReadAllBytes(_rosterSavePath);
            }

            // 배낭은 정적 상태다. 앞선 테스트가 남긴 것을 비우고 시작하지 않으면
            // 개수 비교가 전부 어긋난다.
            RunSatchel.Discard();

            _rosterObject = new GameObject("TestPlayerRoster");
            _rosterObject.AddComponent<PlayerRoster>();
        }

        [TearDown]
        public void TearDown()
        {
            RunSatchel.Discard();

            DestroyIfAlive(_counterObject);
            DestroyIfAlive(_slime);
            DestroyIfAlive(_player);
            DestroyIfAlive(_managerObject);
            DestroyIfAlive(_rosterObject);

            if (_hadRosterSave)
            {
                File.WriteAllBytes(_rosterSavePath, _rosterBackup);
            }
            else if (File.Exists(_rosterSavePath))
            {
                File.WriteAllText(_rosterSavePath, "{\"value\":[]}");
            }
        }

        private static void DestroyIfAlive(GameObject go)
        {
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }
        }

        private static SlimeInstance MakeSlime()
        {
            return new SlimeInstance("slime_basic", new SlimeStatBlock(20, 5, 4, 3));
        }

        private GameManager MakeGameManager()
        {
            _managerObject = new GameObject("TestGameManager");
            return _managerObject.AddComponent<GameManager>();
        }

        [UnityTest]
        public IEnumerator Test_Capture_Goes_To_Satchel()
        {
            _player = new GameObject("TestPlayer");
            _player.transform.position = Arena;
            CaptureTool tool = _player.AddComponent<CaptureTool>();

            _slime = new GameObject("TestSlime");
            _slime.transform.position = Arena;
            var body = _slime.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            _slime.AddComponent<CircleCollider2D>().radius = 0.5f;
            WildSlimeAgent agent = _slime.AddComponent<WildSlimeAgent>();
            agent.ApplyDamage(agent.Instance.baseStats.maxHp, this);
            Assert.IsTrue(agent.Instance.weakened, "사전 조건: 포획하려면 슬라임이 약화 상태여야 합니다.");

            int rosterBefore = PlayerRoster.Instance.Roster.Count;
            Physics2D.SyncTransforms();

            typeof(CaptureTool)
                .GetMethod("TryCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(tool, null);

            Assert.AreEqual(1, RunSatchel.Count, "런 중 포획한 슬라임이 배낭에 담기지 않았습니다.");
            Assert.AreEqual(rosterBefore, PlayerRoster.Instance.Roster.Count,
                "런 중 포획이 보유 목록에 곧바로 들어갔습니다 — 죽어도 잃는 게 없어집니다.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_Extraction_Commits_Satchel()
        {
            GameManager manager = MakeGameManager();
            for (int i = 0; i < 3; i++)
            {
                RunSatchel.Add(MakeSlime());
            }

            int rosterBefore = PlayerRoster.Instance.Roster.Count;

            manager.EndRun(RunEndCause.Extraction);

            Assert.AreEqual(rosterBefore + 3, PlayerRoster.Instance.Roster.Count,
                "추출했는데 배낭 3마리가 보유 목록에 확정되지 않았습니다.");
            Assert.AreEqual(0, RunSatchel.Count, "정산 후에도 배낭이 비지 않았습니다.");

            // EndRun 이 예약한 Hub 씬 로드를 이 테스트 안에서 소화한다.
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_Death_Forfeits_Satchel()
        {
            GameManager manager = MakeGameManager();
            for (int i = 0; i < 3; i++)
            {
                RunSatchel.Add(MakeSlime());
            }

            int rosterBefore = PlayerRoster.Instance.Roster.Count;

            manager.EndRun(RunEndCause.Death);

            Assert.AreEqual(rosterBefore, PlayerRoster.Instance.Roster.Count,
                "사망했는데 배낭 내용이 보유 목록에 들어갔습니다.");
            Assert.AreEqual(0, RunSatchel.Count, "사망 후에도 배낭이 비지 않았습니다.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_Satchel_Counter_Matches_Within_One_Frame()
        {
            _counterObject = new GameObject("TestSatchelCounter");
            var text = _counterObject.AddComponent<Text>();
            SatchelCounterUI counter = _counterObject.AddComponent<SatchelCounterUI>();
            typeof(SatchelCounterUI)
                .GetField("counterText", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(counter, text);

            RunSatchel.Add(MakeSlime());
            RunSatchel.Add(MakeSlime());

            yield return null;

            StringAssert.Contains("2", text.text,
                "포획 1프레임 뒤 표시가 실제 소지 수와 다릅니다: " + text.text);
        }
    }
}

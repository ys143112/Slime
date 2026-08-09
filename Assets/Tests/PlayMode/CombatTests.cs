using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Gameplay.Tests
{
    // 기능: spec-007 검증 (플레이어 전투와 야생 슬라임 약화)
    // 콘솔 에러 0개 기준은 테스트 러너가 대신 본다 — 예상하지 않은 LogError·예외가
    // 하나라도 나오면 해당 테스트가 실패한다.
    public sealed class CombatTests
    {
        // 사망 테스트는 GameManager 를 거치며 Hub 씬을 띄운다. 그 씬의 벽·장식과
        // 테스트용 충돌이 섞이지 않도록 실제 레벨에서 멀리 떨어진 곳에서 논다.
        private static readonly Vector3 Arena = new Vector3(1000f, 1000f, 0f);

        private readonly SoloPlayerTag _soloPlayer = new SoloPlayerTag();

        private GameObject _player;
        private GameObject _slime;
        private GameObject _managerObject;

        [SetUp]
        public void SetUp()
        {
            _soloPlayer.SilenceExisting();

            _player = MakePhysicsObject("TestPlayer");
            _player.tag = "Player";
            _player.transform.position = Arena;
            _player.AddComponent<PlayerHealth>();

            _slime = MakePhysicsObject("TestSlime");
            _slime.transform.position = Arena + new Vector3(50f, 0f, 0f);
            _slime.AddComponent<WildSlimeAgent>();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyIfAlive(_slime);
            DestroyIfAlive(_player);
            DestroyIfAlive(_managerObject);
            _soloPlayer.Restore();
        }

        private static GameObject MakePhysicsObject(string name)
        {
            var go = new GameObject(name);
            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            go.AddComponent<CircleCollider2D>().radius = 0.5f;
            return go;
        }

        private static void DestroyIfAlive(GameObject go)
        {
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }
        }

        // 사망 처리는 GameManager 를 거친다. 씬 로드까지 따라가면 테스트 오브젝트가
        // 같이 날아가므로, 이벤트를 확인할 때까지는 프레임을 넘기지 않는다.
        private GameManager MakeGameManager()
        {
            _managerObject = new GameObject("TestGameManager");
            return _managerObject.AddComponent<GameManager>();
        }

        [UnityTest]
        public IEnumerator Test_Player_Takes_Slime_Attack_Damage_On_Contact()
        {
            PlayerHealth health = _player.GetComponent<PlayerHealth>();
            WildSlimeAgent agent = _slime.GetComponent<WildSlimeAgent>();
            int fullHp = health.MaxHp;
            int expectedDamage = agent.Instance.baseStats.attack;

            _player.transform.position = Arena;
            _slime.transform.position = Arena + new Vector3(0.4f, 0f, 0f);

            // 충돌이 몇 번째 물리 프레임에 잡히는지는 보장되지 않는다. 고정 프레임
            // 수로 기다리면 에디터가 느린 순간에만 실패하는 테스트가 된다.
            for (int frame = 0; frame < 30 && health.CurrentHp == fullHp; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.AreEqual(fullHp - expectedDamage, health.CurrentHp,
                "접촉 후 플레이어 체력이 슬라임 공격력만큼 줄지 않았습니다.");
        }

        [Test]
        public void Test_Slime_Weakened_On_Zero_HP()
        {
            WildSlimeAgent agent = _slime.GetComponent<WildSlimeAgent>();
            var damageLogs = new List<string>();
            Application.logMessageReceived += CollectDamageLog;

            try
            {
                agent.ApplyDamage(agent.Instance.baseStats.maxHp, this);

                Assert.AreEqual(0, agent.Instance.currentHp, "체력이 0 아래로 내려갔거나 0 에 닿지 않았습니다.");
                Assert.IsTrue(agent.Instance.weakened, "체력이 0 이 됐는데 약화 표시가 서지 않았습니다.");
                Assert.AreEqual(1, damageLogs.Count, "피격 로그가 한 번 남아야 합니다.");

                // 이미 약화된 개체를 또 때려도 두 번째 약화 처리가 일어나면 안 된다.
                agent.ApplyDamage(999, this);

                Assert.IsTrue(agent.Instance.weakened, "추가 피격 후 약화 표시가 풀렸습니다.");
                Assert.AreEqual(1, damageLogs.Count, "약화된 개체가 다시 피해를 받아 약화가 두 번 처리됐습니다.");
            }
            finally
            {
                Application.logMessageReceived -= CollectDamageLog;
            }

            void CollectDamageLog(string condition, string stackTrace, LogType type)
            {
                if (type == LogType.Log && condition.StartsWith("slime_damaged"))
                {
                    damageLogs.Add(condition);
                }
            }
        }

        // P0-1: 동행 슬라임이 들어오기 전에 진영 필터가 서 있어야 한다. 없으면
        // 동행이 플레이어를, 플레이어 공격이 동행을 그대로 때린다.
        [Test]
        public void Test_Same_Faction_Damage_Is_Ignored()
        {
            PlayerHealth health = _player.GetComponent<PlayerHealth>();
            WildSlimeAgent agent = _slime.GetComponent<WildSlimeAgent>();
            PlayerMeleeAttack attack = _player.AddComponent<PlayerMeleeAttack>();

            health.ApplyDamage(5, attack);
            Assert.AreEqual(health.MaxHp, health.CurrentHp, "같은 진영(플레이어) 출처의 피해가 들어갔습니다.");

            int slimeHp = agent.Instance.currentHp;
            agent.ApplyDamage(5, agent);
            Assert.AreEqual(slimeHp, agent.Instance.currentHp, "같은 진영(야생) 출처의 피해가 들어갔습니다.");

            // 진영을 안 밝히는 출처(테스트·환경 피해)는 계속 통해야 한다.
            agent.ApplyDamage(5, this);
            Assert.AreEqual(slimeHp - 5, agent.Instance.currentHp,
                "진영 없는 출처의 피해까지 막혔습니다 — 피해가 조용히 사라집니다.");
        }

        [UnityTest]
        public IEnumerator Test_Player_Death_Fires_RunEnded_Once()
        {
            MakeGameManager();
            PlayerHealth health = _player.GetComponent<PlayerHealth>();
            var ended = new List<GameEvent>();
            void OnRunEnded(GameEvent e) => ended.Add(e);
            EventBus.Subscribe(GameEventId.RunEnded, OnRunEnded);

            try
            {
                health.ApplyDamage(health.MaxHp, this);

                Assert.AreEqual(1, ended.Count, "체력이 0 이 된 프레임에 RunEnded 가 정확히 1회 발행돼야 합니다.");
                Assert.AreEqual(RunEndCause.Death, ended[0].Payload,
                    "RunEnded 의 사유가 Death 가 아닙니다.");

                health.ApplyDamage(10, this);

                Assert.AreEqual(1, ended.Count, "사망 이후 추가 피격으로 RunEnded 가 다시 발행됐습니다.");
            }
            finally
            {
                EventBus.Unsubscribe(GameEventId.RunEnded, OnRunEnded);
            }

            // EndRun 이 예약한 Hub 씬 로드를 이 테스트 안에서 소화한다. 남겨두면
            // 다음 테스트가 프레임을 넘기는 순간 그 테스트의 오브젝트가 지워진다.
            yield return SceneLoadWait.UntilRunEndSceneLoaded();
        }
    }
}

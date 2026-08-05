using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Gameplay.Tests
{
    // 기능: spec-012 검증 (야생 슬라임 추격과 방향 공격)
    public sealed class PursuitTests
    {
        // Hub 씬 지오메트리와 섞이지 않게 멀리 떨어진 곳에서 논다.
        private static readonly Vector3 Arena = new Vector3(1000f, 1000f, 0f);

        private const int ObservedFrames = 60;

        private GameObject _player;
        private GameObject _slime;

        [SetUp]
        public void SetUp()
        {
            _player = MakePhysicsObject("TestPlayer");
            _player.tag = "Player";
            _player.transform.position = Arena;

            _slime = MakePhysicsObject("TestSlime");
            _slime.transform.position = Arena;
        }

        [TearDown]
        public void TearDown()
        {
            DestroyIfAlive(_slime);
            DestroyIfAlive(_player);
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

        // 슬라임을 플레이어에서 distance 만큼 떨어뜨려 놓고 붙인다. 컴포넌트를
        // 붙이는 순간 Awake 가 도므로 위치를 먼저 잡는다.
        private WildSlimeAgent PlaceSlime(float distance)
        {
            _slime.transform.position = Arena + new Vector3(distance, 0f, 0f);
            return _slime.AddComponent<WildSlimeAgent>();
        }

        private static IEnumerator Observe()
        {
            for (int i = 0; i < ObservedFrames; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        [UnityTest]
        public IEnumerator Test_Idle_Slime_Does_Not_Move()
        {
            // 감지 반경 기본값 4 보다 한참 밖.
            WildSlimeAgent agent = PlaceSlime(50f);
            Vector3 start = _slime.transform.position;

            yield return Observe();

            Assert.AreEqual(WildSlimeState.Resting, agent.State, "범위 밖인데 추격 상태입니다.");
            Assert.AreEqual(0f, Vector3.Distance(start, _slime.transform.position), 0.001f,
                "플레이어가 감지 범위 밖인데 슬라임이 움직였습니다.");
        }

        [UnityTest]
        public IEnumerator Test_Slime_Pursues_Player()
        {
            var stateLogs = new List<string>();
            void Collect(string condition, string stackTrace, LogType type)
            {
                if (type == LogType.Log && condition.StartsWith("slime_state_changed"))
                {
                    stateLogs.Add(condition);
                }
            }

            Application.logMessageReceived += Collect;
            WildSlimeAgent agent = PlaceSlime(2f);
            float startDistance = Vector3.Distance(_player.transform.position, _slime.transform.position);

            yield return Observe();

            float endDistance = Vector3.Distance(_player.transform.position, _slime.transform.position);
            Application.logMessageReceived -= Collect;

            Assert.AreEqual(WildSlimeState.Pursuing, agent.State, "범위 안인데 추격 상태가 아닙니다.");
            Assert.Less(endDistance, startDistance,
                "플레이어가 감지 범위 안인데 슬라임이 가까워지지 않았습니다.");
            Assert.IsTrue(
                stateLogs.Exists(line => line.Contains("from=Resting") && line.Contains("to=Pursuing")),
                "상태 전이 로그에 이전·다음 상태가 남지 않았습니다: " + string.Join(" | ", stateLogs));
        }

        [UnityTest]
        public IEnumerator Test_Weakened_Slime_Stops()
        {
            WildSlimeAgent agent = PlaceSlime(2f);
            agent.ApplyDamage(agent.Instance.baseStats.maxHp, this);
            Assert.IsTrue(agent.Instance.weakened, "사전 조건: 슬라임이 약화 상태여야 합니다.");

            Vector3 start = _slime.transform.position;

            yield return Observe();

            Assert.AreEqual(0f, Vector3.Distance(start, _slime.transform.position), 0.001f,
                "약화된 슬라임이 플레이어를 쫓아 움직였습니다 — 포획할 수 없게 됩니다.");
        }

        [Test]
        public void Test_Attack_Respects_Facing()
        {
            // 슬라임을 플레이어 아래쪽에 붙여 두고, 아래를 보고 때린 경우와 위를
            // 보고 때린 경우를 비교한다. 물리 시뮬레이션은 돌리지 않는다 — 한
            // 프레임이라도 흐르면 슬라임이 추격을 시작해 거리가 달라진다.
            WildSlimeAgent agent = PlaceSlime(0f);
            _slime.transform.position = Arena + new Vector3(0f, -0.9f, 0f);

            PlayerMovement movement = _player.AddComponent<PlayerMovement>();
            PlayerMeleeAttack attack = _player.AddComponent<PlayerMeleeAttack>();
            int damage = (int)typeof(PlayerMeleeAttack)
                .GetField("attackDamage", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(attack);
            Physics2D.SyncTransforms();

            int fullHp = agent.Instance.currentHp;

            SetFacing(movement, Vector2.up);
            Invoke(attack, "PerformAttack");
            Assert.AreEqual(fullHp, agent.Instance.currentHp,
                "반대쪽을 보고 때렸는데 슬라임이 피해를 입었습니다.");

            SetFacing(movement, Vector2.down);
            Invoke(attack, "PerformAttack");
            Assert.AreEqual(fullHp - damage, agent.Instance.currentHp,
                "슬라임을 보고 때렸는데 공격력만큼 피해가 들어가지 않았습니다.");
        }

        private static void SetFacing(PlayerMovement movement, Vector2 direction)
        {
            typeof(PlayerMovement)
                .GetProperty("LastDirection", BindingFlags.Public | BindingFlags.Instance)
                .SetValue(movement, direction);
        }

        private static void Invoke(object target, string methodName)
        {
            target.GetType()
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(target, null);
        }
    }
}

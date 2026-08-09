using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Gameplay.Tests
{
    /// <summary>
    /// 회피 대상 판정 검증. 예전엔 타일맵 콜라이더만 벽으로 쳐서, 씬에 손으로 놓은
    /// 장식·시설에는 슬라임이 정면으로 박혀 제자리걸음을 했다(팀 QA, 2026-08-09).
    /// </summary>
    public sealed class SteeringTests
    {
        // 다른 테스트의 지오메트리와 안 섞이게 멀리 둔다.
        private static readonly Vector3 Arena = new Vector3(2000f, 2000f, 0f);

        private GameObject _mover;
        private GameObject _obstacle;

        [TearDown]
        public void TearDown()
        {
            if (_mover != null) Object.DestroyImmediate(_mover);
            if (_obstacle != null) Object.DestroyImmediate(_obstacle);
        }

        [UnityTest]
        public IEnumerator StaticProp_TurnsVelocityAside()
        {
            Collider2D self = MakeMover();
            MakeObstacle(rigidbody: false);

            // 콜라이더는 물리 스텝을 한 번 밟아야 질의에 잡힌다.
            yield return new WaitForFixedUpdate();

            Vector2 steered = Steering.SlideAlongWalls(self, Vector2.right * 3f);

            Assert.That(steered.magnitude, Is.EqualTo(3f).Within(0.01f),
                "속도 크기는 유지돼야 한다 — 줄이면 벽 앞에서 느려진다.");
            Assert.That(Mathf.Abs(steered.normalized.x), Is.LessThan(0.9f),
                "정면(오른쪽)으로 그대로 나가면 프롭에 박혀 제자리걸음이 된다.");
        }

        [UnityTest]
        public IEnumerator DynamicSlime_IsNotAvoided()
        {
            Collider2D self = MakeMover();
            MakeObstacle(rigidbody: true);

            yield return new WaitForFixedUpdate();

            Vector2 steered = Steering.SlideAlongWalls(self, Vector2.right * 3f);

            // 슬라임끼리는 서로 밀어내면 그만이다. 여기서 같이 피하면 무리 전체가
            // 대상을 놓고 빙빙 돈다.
            Assert.That(steered, Is.EqualTo(Vector2.right * 3f).Using(new Vector2Comparer(0.01f)));
        }

        private Collider2D MakeMover()
        {
            _mover = new GameObject("Mover");
            _mover.transform.position = Arena;
            var body = _mover.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            return _mover.AddComponent<CircleCollider2D>();
        }

        // 진행 방향 바로 앞(0.6유닛)에 세운다. Steering 의 LookAhead 는 0.7 이다.
        private void MakeObstacle(bool rigidbody)
        {
            _obstacle = new GameObject("Obstacle");
            _obstacle.transform.position = Arena + new Vector3(1.6f, 0f, 0f);
            _obstacle.AddComponent<BoxCollider2D>();

            if (!rigidbody)
            {
                return;
            }

            Rigidbody2D body = _obstacle.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.bodyType = RigidbodyType2D.Dynamic;
        }

        private sealed class Vector2Comparer : System.Collections.IComparer
        {
            private readonly float _tolerance;

            public Vector2Comparer(float tolerance) => _tolerance = tolerance;

            public int Compare(object x, object y)
            {
                return Vector2.Distance((Vector2)x, (Vector2)y) <= _tolerance ? 0 : 1;
            }
        }
    }
}

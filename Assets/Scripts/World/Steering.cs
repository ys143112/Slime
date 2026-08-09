using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.Gameplay
{
    /// <summary>벽에 정면으로 박는 대신 벽을 따라 미끄러지게 속도를 꺾는다.</summary>
    /// <remarks>
    /// 야생·동행 둘 다 "대상 쪽으로 직선" 으로만 움직여서, 사이에 벽이 있으면
    /// 벽을 밀며 제자리걸음을 했다(팀 QA, 2026-08-08: "벽에 막히는 AI 수준").
    ///
    /// <b>길찾기를 넣지 않는다.</b> 방이 넓고 복도가 3칸이라 막히는 상황은 거의
    /// 벽 한 면에 비스듬히 붙는 경우다 — 접선으로 꺾어 주면 벽을 훑고 돌아나간다.
    /// A* 는 지도가 매 런 새로 생성되므로 그래프를 다시 굽는 비용까지 붙는다.
    ///
    /// ponytail: 벽면을 따라 미끄러지는 것뿐이라 ㄷ 자로 파인 막다른 곳에서는
    /// 여전히 갇힌다. 그런 지형이 실제로 생기면 그때 길찾기를 넣는다.
    /// </remarks>
    public static class Steering
    {
        // 얼마나 앞을 보는가. 슬라임 지름(1.0)보다 짧으면 이미 박은 뒤에 꺾는다.
        private const float LookAhead = 0.7f;

        private static readonly List<RaycastHit2D> Hits = new List<RaycastHit2D>();
        private static ContactFilter2D _filter = new ContactFilter2D { useTriggers = false };

        public static Vector2 SlideAlongWalls(Collider2D self, Vector2 velocity)
        {
            if (self == null || velocity.sqrMagnitude < 0.0001f)
            {
                return velocity;
            }

            Vector2 direction = velocity.normalized;
            if (!TryFindWall(self, direction, out Vector2 normal))
            {
                return velocity;
            }

            // 벽면을 따라가는 두 방향 중 지금 가려던 쪽에 가까운 것을 고른다.
            Vector2 tangent = Vector2.Perpendicular(normal);
            if (Vector2.Dot(tangent, direction) < 0f)
            {
                tangent = -tangent;
            }

            // 벽에서 살짝 떼어 놓는다. 순수 접선만 주면 마찰로 계속 벽에 붙어
            // 코너에서 빠져나오질 못한다.
            return (tangent + normal * 0.15f).normalized * velocity.magnitude;
        }

        private static bool TryFindWall(Collider2D self, Vector2 direction, out Vector2 normal)
        {
            normal = Vector2.zero;

            // 자기 몸통 크기로 쏜다 — 점으로 쏘면 어깨가 걸리는 경우를 놓친다.
            float radius = Mathf.Max(0.05f, self.bounds.extents.x * 0.9f);
            Hits.Clear();
            Physics2D.CircleCast(self.bounds.center, radius, direction, _filter, Hits, LookAhead);

            float nearest = float.MaxValue;
            foreach (RaycastHit2D hit in Hits)
            {
                // 벽만 본다. 슬라임끼리는 서로 밀어내면 그만이고, 여기서 같이
                // 피하게 하면 무리 전체가 대상을 놓고 빙빙 돈다.
                if (hit.collider == null || !IsWall(hit.collider))
                {
                    continue;
                }

                // 이미 파묻힌 상태면 법선이 0 으로 온다 — 그 값으로 꺾으면
                // 속도가 통째로 사라져 완전히 멈춘다.
                if (hit.normal.sqrMagnitude < 0.0001f || hit.distance >= nearest)
                {
                    continue;
                }

                nearest = hit.distance;
                normal = hit.normal;
            }

            return nearest < float.MaxValue;
        }

        /// <summary>피해서 돌아가야 하는 고정 장애물인가.</summary>
        /// <remarks>
        /// 예전엔 타일맵 콜라이더만 벽으로 쳤다. 그래서 씬에 손으로 놓은 오브젝트
        /// (별 모양 장식, 휴식소 웅덩이, 교배장 같은 것)에는 접선 회피가 안 걸려
        /// 슬라임이 정면으로 밀며 제자리걸음을 했다 — "별 오브젝트에 끼여서 안
        /// 움직인다"(팀 QA, 2026-08-09).
        ///
        /// 판정 기준을 타입이 아니라 <b>움직이지 않는가</b>로 바꾼다. 리지드바디가
        /// 없거나 Static 이면 밀어도 안 비키므로 돌아가는 수밖에 없다. 슬라임끼리
        /// (Dynamic)와 플레이어는 그대로 제외된다 — 서로 밀어내면 그만이고, 여기서
        /// 같이 피하게 하면 무리 전체가 대상을 놓고 빙빙 돈다.
        /// </remarks>
        private static bool IsWall(Collider2D collider)
        {
            if (collider.isTrigger)
            {
                return false;
            }

            Rigidbody2D body = collider.attachedRigidbody;
            return body == null || body.bodyType == RigidbodyType2D.Static;
        }
    }
}

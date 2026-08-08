using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Gameplay.Tests
{
    // 기능: 스테이지 맵 생성 검증 (STAGE_A_DESIGN.md §8)
    // StageLayout 은 Tilemap 도 GameObject 도 안 쓰므로 씬 없이 돈다 —
    // 지도가 이상할 때 계산이 틀렸는지 그리기가 틀렸는지 여기서 갈린다.
    public sealed class StageLayoutTests
    {
        private const string Home = "biome_default";
        private static readonly string[] Foreign = { "biome_marsh", "biome_ashfall" };

        private static StageLayout Build(int seed) =>
            StageLayout.Generate(new StageLayoutSettings(), seed, Home, Foreign);

        // 1. 같은 시드는 같은 지도. 버그 재현이 여기에 달려 있다.
        [Test]
        public void Test_StageLayout_Deterministic()
        {
            StageLayout a = Build(12345);
            StageLayout b = Build(12345);

            Assert.AreEqual(a.Rooms.Count, b.Rooms.Count, "방 개수가 시드에 대해 안 정해진다");
            for (int i = 0; i < a.Rooms.Count; i++)
            {
                Assert.AreEqual(a.Rooms[i].bounds, b.Rooms[i].bounds, $"방 {i} 위치가 다르다");
                Assert.AreEqual(a.Rooms[i].pattern, b.Rooms[i].pattern, $"방 {i} 패턴이 다르다");
                Assert.AreEqual(a.Rooms[i].biomeId, b.Rooms[i].biomeId, $"방 {i} 구역이 다르다");
            }

            for (int x = 0; x < a.Width; x++)
            {
                for (int y = 0; y < a.Height; y++)
                {
                    if (a.IsFloor(x, y) != b.IsFloor(x, y))
                    {
                        Assert.Fail($"바닥이 시드에 대해 안 정해진다 ({x},{y})");
                    }
                }
            }
        }

        // 2. 시작 방에서 걸어서 모든 방에 닿아야 한다. 못 닿는 방은 그 안의
        //    슬라임·전리품이 통째로 없는 것과 같다.
        [Test]
        public void Test_All_Rooms_Reachable()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                StageLayout layout = Build(seed);
                HashSet<Vector2Int> walkable = FloodFill(layout, layout.PlayerSpawn);

                for (int i = 0; i < layout.Rooms.Count; i++)
                {
                    Assert.IsTrue(
                        HasCellIn(walkable, layout.Rooms[i].bounds),
                        $"seed={seed} 방 {i} 에 걸어서 못 간다");
                }
            }
        }

        // 3. 추출구가 벽 밖이면 트리거가 멀쩡해도 도달할 수 없다. 세 씬 전부
        //    이 사고를 겪었다.
        [Test]
        public void Test_Extraction_Is_On_Floor()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                StageLayout layout = Build(seed);
                Vector2Int point = layout.ExtractionPoint;

                Assert.IsTrue(layout.IsFloor(point.x, point.y), $"seed={seed} 추출구가 바닥이 아니다");
                Assert.IsTrue(
                    FloodFill(layout, layout.PlayerSpawn).Contains(point),
                    $"seed={seed} 추출구에 걸어서 못 간다");
            }
        }

        // 4. 스폰 후보는 전부 바닥이고 벽에 안 붙어 있어야 한다 — 벽에 낀
        //    슬라임은 밀려나는 것처럼 보인다.
        [Test]
        public void Test_Spawn_Points_On_Floor()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                StageLayout layout = Build(seed);
                for (int i = 0; i < layout.Rooms.Count; i++)
                {
                    foreach (Vector2Int cell in layout.SpawnCandidates(i))
                    {
                        Assert.IsTrue(layout.IsFloor(cell.x, cell.y), $"seed={seed} 스폰 후보가 벽이다 {cell}");
                        Assert.IsFalse(
                            TouchesNonFloor(layout, cell),
                            $"seed={seed} 스폰 후보가 벽에 붙어 있다 {cell}");
                    }
                }
            }
        }

        // 5. 홈 바이옴이 과반이어야 "늪지 씬" 이라는 말이 성립한다.
        [Test]
        public void Test_Home_Biome_Majority()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                StageLayout layout = Build(seed);
                int home = 0;
                foreach (StageRoom room in layout.Rooms)
                {
                    Assert.IsNotNull(room.biomeId, $"seed={seed} 구역이 안 정해진 방이 있다");
                    if (room.biomeId == Home)
                    {
                        home++;
                    }
                }

                Assert.GreaterOrEqual(
                    (float)home / layout.Rooms.Count,
                    0.6f,
                    $"seed={seed} 홈 바이옴 비중이 60% 미만");
            }
        }

        // 6. 방이 너무 적으면 좁고, 너무 많으면 다 작아진다.
        [Test]
        public void Test_Room_Count_In_Range()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                int count = Build(seed).Rooms.Count;
                Assert.That(count, Is.InRange(14, 20), $"seed={seed} 방 개수 {count}");
            }
        }

        // 7. 기둥이 문 앞에 몰리면 방이 통째로 끊긴다. 전체 연결성(2번)은 방
        //    "사이" 만 보므로 방 "안" 이 끊기는 건 여기서만 잡힌다.
        [Test]
        public void Test_Pattern_Never_Blocks_Doors()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                StageLayout layout = Build(seed);
                HashSet<Vector2Int> walkable = FloodFill(layout, layout.PlayerSpawn);

                foreach (StageRoom room in layout.Rooms)
                {
                    if (room.pattern != RoomPattern.Pillar)
                    {
                        continue;
                    }

                    // 기둥 방의 바닥 칸은 전부 시작 지점과 이어져 있어야 한다.
                    // 하나라도 떨어지면 기둥이 방을 갈랐다는 뜻이다.
                    for (int x = room.bounds.x; x < room.bounds.xMax; x++)
                    {
                        for (int y = room.bounds.y; y < room.bounds.yMax; y++)
                        {
                            if (layout.IsFloor(x, y) && !walkable.Contains(new Vector2Int(x, y)))
                            {
                                Assert.Fail($"seed={seed} 기둥이 방을 갈랐다 ({x},{y})");
                            }
                        }
                    }
                }
            }
        }

        // 8. 전부 위험하면 위험이 안 느껴진다. 조용한 방이 있어야 다음 방이 무섭다.
        [Test]
        public void Test_Safe_Rooms_Guaranteed()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                StageLayout layout = Build(seed);

                Assert.AreEqual(
                    RoomPattern.Safe,
                    layout.Rooms[layout.StartRoomIndex].pattern,
                    $"seed={seed} 시작 방이 쉼터가 아니다");

                int safe = 0;
                foreach (StageRoom room in layout.Rooms)
                {
                    if (room.pattern == RoomPattern.Safe)
                    {
                        safe++;
                    }
                }

                Assert.GreaterOrEqual(safe, 2, $"seed={seed} 쉼터가 {safe}개뿐");
            }
        }

        // 9. 씬의 홈 바이옴인데 시작 방이 외지로 나오면 "늪지 씬인데 시작하자마자
        //    화산재 바닥" 이 된다. 실측(2026-08-08)으로 잡은 버그.
        [Test]
        public void Test_Start_Room_Is_Home_Biome()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                StageLayout layout = Build(seed);
                Assert.AreEqual(
                    Home,
                    layout.Rooms[layout.StartRoomIndex].biomeId,
                    $"seed={seed} 시작 방이 홈 바이옴이 아니다");
            }
        }

        private static HashSet<Vector2Int> FloodFill(StageLayout layout, Vector2Int from)
        {
            var visited = new HashSet<Vector2Int> { from };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(from);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                Step(layout, visited, queue, current.x + 1, current.y);
                Step(layout, visited, queue, current.x - 1, current.y);
                Step(layout, visited, queue, current.x, current.y + 1);
                Step(layout, visited, queue, current.x, current.y - 1);
            }

            return visited;
        }

        private static void Step(
            StageLayout layout, HashSet<Vector2Int> visited, Queue<Vector2Int> queue, int x, int y)
        {
            var cell = new Vector2Int(x, y);
            if (!layout.IsFloor(x, y) || !visited.Add(cell))
            {
                return;
            }

            queue.Enqueue(cell);
        }

        private static bool HasCellIn(HashSet<Vector2Int> cells, RectInt bounds)
        {
            for (int x = bounds.x; x < bounds.xMax; x++)
            {
                for (int y = bounds.y; y < bounds.yMax; y++)
                {
                    if (cells.Contains(new Vector2Int(x, y)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TouchesNonFloor(StageLayout layout, Vector2Int cell)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (!layout.IsFloor(cell.x + dx, cell.y + dy))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

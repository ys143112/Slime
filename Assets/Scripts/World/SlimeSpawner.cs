using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    // 기능: 스테이지 맵 생성 (STAGE_A_DESIGN.md §6)
    // StageMapGenerator 가 그린 방 목록을 받아 방마다 야생 슬라임을 흩는다.
    // WildSlimeAgent.Initialize(species, tier) 를 호출만 한다 — 그 메서드
    // 안의 스탯 계산 순서(티어 배율 → 종 편향)는 여기서 다시 안 건드린다.
    // 밖에서 배율을 또 곱하면 두 번 곱해진다(WildSlimeAgent.cs 주석 참고).
    public sealed class SlimeSpawner : MonoBehaviour
    {
        [SerializeField] private StageMapGenerator generator;
        [SerializeField] private GameObject wildSlimePrefab;

        [SerializeField] private int perRoomMin = 3;
        [SerializeField] private int perRoomMax = 6;

        // 방 패턴별 배율(§5-6). Nest 는 3 — 안 들어가도 되는 방이 제일 위험해야
        // "욕심"이 결정이 된다. 동시 상한은 없음(사용자 결정, 2026-08-08).
        //
        // Safe 는 0 이었다. 조용한 방이 있어야 다음 방이 무섭다는 뜻이었는데,
        // 방이 16개나 되고 그중 Safe 가 여럿이라 지도의 넓은 구역이 통째로
        // 비었다 — "특정 구간에만 슬라임이 나온다" 로 신고됨(2026-08-08).
        // 0.5 면 다른 방의 절반이라 여전히 한숨 돌리는 자리로 읽힌다.
        [SerializeField] private float safeMultiplier = 0.5f;
        [SerializeField] private float pillarMultiplier = 1f;
        [SerializeField] private float nestMultiplier = 3f;

        // 둥지 방은 티어도 +1 — 배낭이 이미 죽으면 몰수라, 스탯으로도 더
        // 위험해야 그 결정이 의미가 있다.
        [SerializeField] private int nestTierBonus = 1;

        [SerializeField] private float minSlimeSpacing = 1.5f;

        // 이번 지도에 우리가 세운 개체들. R 로 다시 생성할 때 걷어내려고 들고 있다.
        private readonly List<GameObject> _spawned = new List<GameObject>();

        // 구독은 Awake 에서 한다. 예전에는 Start 에서 "생성기 Start 가 먼저
        // 돌았겠지" 하고 Layout 을 읽었는데, Unity 는 다른 GameObject 의 Start
        // 순서를 보장하지 않아 뒤집히면 슬라임이 0마리가 된다(팀 리뷰 D7,
        // 2026-08-08). 모든 Awake 는 모든 Start 보다 먼저 도므로 이러면 순서가
        // 보장된다.
        private void Awake()
        {
            if (generator != null)
            {
                generator.MapGenerated += Spawn;
            }
        }

        private void OnDestroy()
        {
            if (generator != null)
            {
                generator.MapGenerated -= Spawn;
            }
        }

        public void Spawn()
        {
            if (generator == null || generator.Layout == null)
            {
                Debug.LogError("SlimeSpawner: StageMapGenerator.Layout 이 아직 없습니다.");
                return;
            }

            // 지도를 다시 그리면 옛 개체는 새 지도의 벽 속에 박힌다. 추격이
            // 직선이라 거기서 영영 못 나온다(팀 리뷰 A4).
            foreach (GameObject old in _spawned)
            {
                if (old != null)
                {
                    Destroy(old);
                }
            }

            _spawned.Clear();

            StageLayout layout = generator.Layout;
            BiomeCatalog catalog = GameManager.Instance != null ? GameManager.Instance.Catalog : null;
            var placed = new List<Vector2>();
            int spawned = 0;

            // 시작 방을 통째로 비우지 않는다. StageLayout.SpawnCandidates 가 이미
            // 플레이어 스폰 반경 안을 걷어내므로 "첫 3초에 죽는" 일은 그쪽에서
            // 막힌다. 방까지 통째로 비우면 지도가 120x90 이라 가장 가까운 슬라임이
            // 20유닛 밖에 서고, 화면이 17.8x10 이라 다이브 직후 화면에 아무것도
            // 없다 — "스폰이 안 된다" 로 보였던 것이 이것이다(2026-08-08 실측:
            // 45마리가 떴는데 최근접이 19.4).
            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                StageRoom room = layout.Rooms[i];
                float multiplier = MultiplierFor(room.pattern);
                if (multiplier <= 0f)
                {
                    continue;
                }

                int baseCount = UnityEngine.Random.Range(perRoomMin, perRoomMax + 1);
                int count = Mathf.RoundToInt(baseCount * multiplier);
                int tier = CorruptionTierFor(room.biomeId) + (room.pattern == RoomPattern.Nest ? nestTierBonus : 0);
                string[] pool = SpeciesPoolFor(catalog, room.biomeId);

                spawned += SpawnInRoom(layout.SpawnCandidates(i), count, tier, pool, placed, _spawned);
            }

            SpawnWantedBoss(layout, placed);

            Debug.Log($"slime_spawner_done count={spawned}");
        }

        private int SpawnInRoom(List<Vector2Int> candidates, int count, int tier, string[] pool, List<Vector2> placed, List<GameObject> spawnedObjects)
        {
            if (candidates.Count == 0)
            {
                return 0;
            }

            int spawned = 0;
            int attempts = 0;
            int maxAttempts = count * 10; // 후보가 모자라면 포기하고 다음 방으로.

            while (spawned < count && attempts < maxAttempts)
            {
                attempts++;
                Vector2Int cell = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                Vector2 world = generator.GroundTilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));

                if (TooClose(world, placed))
                {
                    continue;
                }

                string species = pool.Length > 0 ? pool[UnityEngine.Random.Range(0, pool.Length)] : null;
                GameObject instance = Instantiate(wildSlimePrefab, world, Quaternion.identity);
                var agent = instance.GetComponent<WildSlimeAgent>();
                if (agent != null)
                {
                    agent.Initialize(species, tier);
                }

                placed.Add(world);
                spawnedObjects.Add(instance);
                spawned++;
            }

            return spawned;
        }

        /// <summary>현상수배 대상을 지도에 한 마리 세운다.</summary>
        /// <remarks>
        /// 가장 위험한 방(Nest, 없으면 마지막 방)에 놓는다 — 시작 방에 세우면
        /// 다이브하자마자 마주쳐 목표가 아니라 사고가 된다.
        /// </remarks>
        private void SpawnWantedBoss(StageLayout layout, List<Vector2> placed)
        {
            string biomeId = GameManager.Instance != null ? GameManager.Instance.CurrentBiomeId : null;
            if (wildSlimePrefab == null || !WantedBoard.PendingForBiome(biomeId))
            {
                return;
            }

            int roomIndex = -1;
            for (int i = layout.Rooms.Count - 1; i >= 0; i--)
            {
                if (layout.Rooms[i].pattern == RoomPattern.Nest)
                {
                    roomIndex = i;
                    break;
                }
            }

            if (roomIndex < 0)
            {
                roomIndex = layout.Rooms.Count - 1;
            }

            if (roomIndex < 0)
            {
                return;
            }

            List<Vector2Int> candidates = layout.SpawnCandidates(roomIndex);
            if (candidates.Count == 0)
            {
                return;
            }

            Vector2Int cell = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            Vector2 world = generator.GroundTilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));

            GameObject boss = Instantiate(wildSlimePrefab, world, Quaternion.identity);
            var agent = boss.GetComponent<WildSlimeAgent>();
            if (agent != null)
            {
                // 티어 배율·종 편향은 이 호출이 맡는다. 보스 배율은 그 결과 위에
                // 한 번만 더 얹는다(WantedBoard.Promote).
                agent.Initialize(WantedBoard.TargetSpeciesId,
                    CorruptionTierFor(layout.Rooms[roomIndex].biomeId) + WantedBoard.BossTierBonus);
                WantedBoard.Promote(agent);
            }

            placed.Add(world);
            _spawned.Add(boss);
        }

        private bool TooClose(Vector2 point, List<Vector2> placed)
        {
            foreach (Vector2 other in placed)
            {
                if (Vector2.Distance(point, other) < minSlimeSpacing)
                {
                    return true;
                }
            }

            return false;
        }

        private float MultiplierFor(RoomPattern pattern)
        {
            switch (pattern)
            {
                case RoomPattern.Safe: return safeMultiplier;
                case RoomPattern.Pillar: return pillarMultiplier;
                case RoomPattern.Nest: return nestMultiplier;
                default: return 0f;
            }
        }

        private static int CorruptionTierFor(string biomeId)
        {
            return BiomeStigmaManager.Instance != null
                ? BiomeStigmaManager.Instance.GetCorruptionTier(biomeId)
                : 0;
        }

        private static string[] SpeciesPoolFor(BiomeCatalog catalog, string biomeId)
        {
            return catalog != null ? catalog.WildSpeciesPool(biomeId) : Array.Empty<string>();
        }
    }
}

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

        [SerializeField] private int perRoomMin = 2;
        [SerializeField] private int perRoomMax = 4;

        // 방 패턴별 배율(§5-6). Safe 는 0 — 조용한 방이 있어야 다음 방이
        // 무섭다. Nest 는 3 — 안 들어가도 되는 방이 제일 위험해야 "욕심"이
        // 결정이 된다. 동시 상한은 없음(사용자 결정, 2026-08-08).
        [SerializeField] private float safeMultiplier;
        [SerializeField] private float pillarMultiplier = 1f;
        [SerializeField] private float nestMultiplier = 3f;

        // 둥지 방은 티어도 +1 — 배낭이 이미 죽으면 몰수라, 스탯으로도 더
        // 위험해야 그 결정이 의미가 있다.
        [SerializeField] private int nestTierBonus = 1;

        [SerializeField] private float minSlimeSpacing = 1.5f;

        private void Start()
        {
            // StageMapGenerator.Start 가 먼저 돌아야 Layout 이 있다. 생성기와
            // 스포너가 늘 한 세트로만 쓰이므로 스크립트 실행 순서 설정 없이
            // 여기서 직접 부른다 — 인스펙터 순서가 바뀌면 여기도 깨진다는
            // 뜻이니, 둘을 분리하게 되면 이벤트로 바꿀 것.
            Spawn();
        }

        public void Spawn()
        {
            if (generator == null || generator.Layout == null)
            {
                Debug.LogError("SlimeSpawner: StageMapGenerator.Layout 이 아직 없습니다.");
                return;
            }

            StageLayout layout = generator.Layout;
            BiomeCatalog catalog = GameManager.Instance != null ? GameManager.Instance.Catalog : null;
            var placed = new List<Vector2>();
            int spawned = 0;

            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                if (i == layout.StartRoomIndex)
                {
                    continue; // 첫 3초에 죽으면 안 된다 — 시작 방은 비운다.
                }

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

                spawned += SpawnInRoom(layout.SpawnCandidates(i), count, tier, pool, placed);
            }

            Debug.Log($"slime_spawner_done count={spawned}");
        }

        private int SpawnInRoom(List<Vector2Int> candidates, int count, int tier, string[] pool, List<Vector2> placed)
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
                spawned++;
            }

            return spawned;
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
            BiomeEntry entry = catalog != null ? catalog.Find(biomeId) : null;
            return entry != null && entry.speciesPool != null ? entry.speciesPool : Array.Empty<string>();
        }
    }
}

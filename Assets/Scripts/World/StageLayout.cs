using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    // 기능: 스테이지 맵 생성 (STAGE_A_DESIGN.md §5)
    // 방의 성격. 모양이 다른 건 Pillar 하나뿐이고 Safe/Nest 는 스포너 배율만
    // 다르다 — 그래서 별도 클래스가 아니라 값 하나로 둔다.
    public enum RoomPattern
    {
        Safe,
        Pillar,
        Nest,
    }

    // 인스펙터에서 조절할 수 있어야 하는 값 전부. 밸런싱에 재컴파일이 걸리면
    // 안 된다.
    [Serializable]
    public sealed class StageLayoutSettings
    {
        public int width = 120;
        public int height = 90;

        // BSP 조각의 최소 크기. 이 값이 방 개수를 정한다 — 120x90 에서
        // 20x17 이면 14~20 방이 나온다(시드 100개 실측).
        public int minLeafWidth = 20;
        public int minLeafHeight = 17;

        public int roomMinWidth = 12;
        public int roomMinHeight = 10;
        public int roomMaxWidth = 22;
        public int roomMaxHeight = 18;

        // 2 칸이면 슬라임 두 마리에 길이 막힌다.
        public int corridorWidth = 3;

        // 바닥에서 이만큼까지만 벽을 깐다. 나머지는 타일 없음(=배경 검정).
        // 1 칸이면 카메라가 지도 밖을 봐서 "잘렸다" 로 읽힌다.
        public int wallBand = 3;

        // 트리 연결만 쓰면 막다른 길뿐인 지도가 된다.
        public int extraConnections = 3;

        public int minSafeRooms = 2;

        // 기둥 개수 범위. Pillar 방 하나가 이만큼 놓는다.
        public int pillarMin = 4;
        public int pillarMax = 8;

        // 구역 시드 개수(홈 2 + 외지 1). 홈 비중이 아래에 못 미치면 홈 시드를
        // 하나 더 넣고 다시 배정한다.
        public int regionSeedCount = 3;
        [Range(0f, 1f)] public float homeShareMin = 0.6f;

        // 스폰 후보에서 제외할 플레이어 주변 반경(유닛 = 타일).
        public int spawnSafeRadius = 10;
    }

    public sealed class StageRoom
    {
        public RectInt bounds;
        public RoomPattern pattern;
        public string biomeId;

        // 시작 방에서 복도 몇 개를 건너야 닿는가. 패턴 페이싱의 기준.
        public int distanceFromStart;

        // 이어진 방이 하나뿐인 방. "안 들어가도 되는 방" 이라 Nest 를 몰아준다.
        public bool isDeadEnd;

        public Vector2Int Center =>
            new Vector2Int(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2);
    }

    // Tilemap 도 GameObject 도 안 쓴다 — 지도가 이상할 때 계산이 틀렸는지
    // 그리기가 틀렸는지 즉시 갈리게 하려는 것이다. 테스트도 씬 없이 돈다.
    public sealed class StageLayout
    {
        private readonly StageLayoutSettings _settings;
        private readonly System.Random _rng;
        private readonly bool[,] _floor;
        private readonly bool[,] _wall;
        private readonly List<StageRoom> _rooms = new List<StageRoom>();
        private readonly List<List<int>> _links = new List<List<int>>();

        public int Width => _settings.width;
        public int Height => _settings.height;
        public IReadOnlyList<StageRoom> Rooms => _rooms;

        // 시작 방은 항상 BSP 의 첫 잎이다. 플레이어는 여기 중심에 선다.
        public int StartRoomIndex => 0;
        public int ExtractionRoomIndex { get; private set; }

        public Vector2Int PlayerSpawn => _rooms[StartRoomIndex].Center;
        public Vector2Int ExtractionPoint => _rooms[ExtractionRoomIndex].Center;

        public bool IsFloor(int x, int y) =>
            x >= 0 && y >= 0 && x < Width && y < Height && _floor[x, y];

        public bool IsWall(int x, int y) =>
            x >= 0 && y >= 0 && x < Width && y < Height && _wall[x, y];

        private StageLayout(StageLayoutSettings settings, int seed)
        {
            _settings = settings;
            _rng = new System.Random(seed);
            _floor = new bool[settings.width, settings.height];
            _wall = new bool[settings.width, settings.height];
        }

        // homeBiomeId 는 씬이 정하는 바이옴, foreignBiomeIds 는 섞어 넣을 외지
        // 바이옴 후보다. 표가 비어 있으면 전 구역이 홈이 된다.
        public static StageLayout Generate(
            StageLayoutSettings settings,
            int seed,
            string homeBiomeId = "biome_default",
            IReadOnlyList<string> foreignBiomeIds = null)
        {
            var layout = new StageLayout(settings, seed);
            layout.Build(homeBiomeId, foreignBiomeIds);
            return layout;
        }

        private void Build(string homeBiomeId, IReadOnlyList<string> foreignBiomeIds)
        {
            // 순서가 중요하다. 방 → 복도 → (문이 정해짐) → 패턴 → 벽.
            // 패턴이 문을 막는지 보려면 복도가 먼저 뚫려 있어야 하고, 벽은
            // 기둥이 파낸 자리까지 포함해야 하므로 맨 끝이다.
            var root = new RectInt(1, 1, _settings.width - 2, _settings.height - 2);
            Split(root);
            AssignDistances();
            AssignPatterns();
            AssignRegions(homeBiomeId, foreignBiomeIds);
            ApplyPatterns();
            BuildWallBand();
        }

        // ── BSP ──────────────────────────────────────────────────────────
        // 조각 안에만 방을 그리므로 방이 겹치는 일이 원리적으로 없다. 무작위
        // 배치 + 겹침 검사보다 짧고 무한루프가 없다.
        private int Split(RectInt area)
        {
            bool canSplitX = area.width >= _settings.minLeafWidth * 2;
            bool canSplitY = area.height >= _settings.minLeafHeight * 2;

            if (!canSplitX && !canSplitY)
            {
                return CarveRoom(area);
            }

            // 긴 축을 자른다. 둘 다 가능하면 더 긴 쪽.
            bool splitVertical = canSplitX && (!canSplitY || area.width >= area.height);

            RectInt a;
            RectInt b;
            if (splitVertical)
            {
                int cut = RandomCut(area.width);
                a = new RectInt(area.x, area.y, cut, area.height);
                b = new RectInt(area.x + cut, area.y, area.width - cut, area.height);
            }
            else
            {
                int cut = RandomCut(area.height);
                a = new RectInt(area.x, area.y, area.width, cut);
                b = new RectInt(area.x, area.y + cut, area.width, area.height - cut);
            }

            int left = Split(a);
            int right = Split(b);

            // 형제끼리 잇는다. 트리라서 이것만으로 모든 방이 이어진다 —
            // 연결성 보정 코드가 따로 필요 없다.
            Connect(left, right);
            return left;
        }

        // 40~60% 지점. 한쪽이 지나치게 얇아지면 방이 최소 크기에 못 미친다.
        private int RandomCut(int length) => Mathf.RoundToInt(length * (0.4f + (float)_rng.NextDouble() * 0.2f));

        private int CarveRoom(RectInt leaf)
        {
            int w = Mathf.Clamp(leaf.width - RandomRange(4, 9), _settings.roomMinWidth, _settings.roomMaxWidth);
            int h = Mathf.Clamp(leaf.height - RandomRange(4, 9), _settings.roomMinHeight, _settings.roomMaxHeight);
            w = Mathf.Min(w, leaf.width - 4);
            h = Mathf.Min(h, leaf.height - 4);

            int x = leaf.x + RandomRange(2, leaf.width - w - 1);
            int y = leaf.y + RandomRange(2, leaf.height - h - 1);

            var room = new StageRoom { bounds = new RectInt(x, y, w, h) };
            FillFloor(room.bounds);

            _rooms.Add(room);
            _links.Add(new List<int>());
            return _rooms.Count - 1;
        }

        // ── 복도 ─────────────────────────────────────────────────────────
        private void Connect(int a, int b)
        {
            if (a == b || _links[a].Contains(b))
            {
                return;
            }

            Vector2Int from = _rooms[a].Center;
            Vector2Int to = _rooms[b].Center;

            // ㄱ자. 가로 먼저 뚫고 세로로 꺾는다.
            CarveCorridor(new Vector2Int(from.x, from.y), new Vector2Int(to.x, from.y));
            CarveCorridor(new Vector2Int(to.x, from.y), new Vector2Int(to.x, to.y));

            _links[a].Add(b);
            _links[b].Add(a);
        }

        private void CarveCorridor(Vector2Int from, Vector2Int to)
        {
            int half = _settings.corridorWidth / 2;
            int stepX = Math.Sign(to.x - from.x);
            int stepY = Math.Sign(to.y - from.y);

            var cursor = from;
            while (true)
            {
                FillFloor(new RectInt(
                    cursor.x - half,
                    cursor.y - half,
                    _settings.corridorWidth,
                    _settings.corridorWidth));

                if (cursor == to)
                {
                    break;
                }

                cursor = new Vector2Int(
                    stepX != 0 ? cursor.x + stepX : cursor.x,
                    stepY != 0 ? cursor.y + stepY : cursor.y);
            }
        }

        // ── 방 그래프 ────────────────────────────────────────────────────
        private void AssignDistances()
        {
            AddExtraConnections();

            var distance = new int[_rooms.Count];
            for (int i = 0; i < distance.Length; i++)
            {
                distance[i] = int.MaxValue;
            }

            var queue = new Queue<int>();
            distance[StartRoomIndex] = 0;
            queue.Enqueue(StartRoomIndex);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (int next in _links[current])
                {
                    if (distance[next] != int.MaxValue)
                    {
                        continue;
                    }

                    distance[next] = distance[current] + 1;
                    queue.Enqueue(next);
                }
            }

            ExtractionRoomIndex = StartRoomIndex;
            for (int i = 0; i < _rooms.Count; i++)
            {
                _rooms[i].distanceFromStart = distance[i] == int.MaxValue ? 0 : distance[i];
                _rooms[i].isDeadEnd = i != StartRoomIndex && _links[i].Count == 1;

                // 추출구는 시작 방에서 제일 먼 방. 게이트가 없으므로 "돌아가는
                // 거리" 가 깊이를 대신한다.
                if (_rooms[i].distanceFromStart > _rooms[ExtractionRoomIndex].distanceFromStart)
                {
                    ExtractionRoomIndex = i;
                }
            }
        }

        private void AddExtraConnections()
        {
            for (int i = 0; i < _settings.extraConnections && _rooms.Count > 2; i++)
            {
                int a = _rng.Next(_rooms.Count);
                int b = NearestUnlinked(a);
                if (b >= 0)
                {
                    Connect(a, b);
                }
            }
        }

        private int NearestUnlinked(int from)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < _rooms.Count; i++)
            {
                if (i == from || _links[from].Contains(i))
                {
                    continue;
                }

                float d = Vector2Int.Distance(_rooms[from].Center, _rooms[i].Center);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = i;
                }
            }

            return best;
        }

        // ── 패턴 ─────────────────────────────────────────────────────────
        // 방마다 따로 굴리면 둥지 3개가 나란히 나온다. 시작 방에서의 거리로
        // 등급을 매겨 굴린다.
        private void AssignPatterns()
        {
            int maxDistance = 1;
            foreach (StageRoom room in _rooms)
            {
                maxDistance = Mathf.Max(maxDistance, room.distanceFromStart);
            }

            for (int i = 0; i < _rooms.Count; i++)
            {
                StageRoom room = _rooms[i];

                if (i == StartRoomIndex)
                {
                    // 첫 3초에 죽으면 안 된다.
                    room.pattern = RoomPattern.Safe;
                    continue;
                }

                if (i == ExtractionRoomIndex)
                {
                    // 뻥 뚫려 있으면 그냥 걸어 나간다.
                    room.pattern = RoomPattern.Pillar;
                    continue;
                }

                float band = (float)room.distanceFromStart / maxDistance;
                float safeWeight = band < 0.34f ? 40f : band < 0.67f ? 30f : 0f;
                float pillarWeight = band < 0.34f ? 60f : band < 0.67f ? 70f : 60f;
                float nestWeight = band < 0.67f ? 0f : 40f;

                // 안 들어가도 되는 방이 제일 위험해야 "욕심" 이 결정이 된다.
                if (room.isDeadEnd)
                {
                    nestWeight *= 3f;
                }

                room.pattern = PickWeighted(safeWeight, pillarWeight, nestWeight);
            }

            GuaranteeSafeRooms();
        }

        private RoomPattern PickWeighted(float safe, float pillar, float nest)
        {
            float roll = (float)_rng.NextDouble() * (safe + pillar + nest);
            if (roll < safe)
            {
                return RoomPattern.Safe;
            }

            return roll < safe + pillar ? RoomPattern.Pillar : RoomPattern.Nest;
        }

        // 전부 위험하면 위험이 안 느껴진다. 모자라면 시작에서 가까운 방부터
        // 쉼터로 바꾼다.
        private void GuaranteeSafeRooms()
        {
            int safeCount = 0;
            foreach (StageRoom room in _rooms)
            {
                if (room.pattern == RoomPattern.Safe)
                {
                    safeCount++;
                }
            }

            while (safeCount < _settings.minSafeRooms)
            {
                int target = -1;
                for (int i = 0; i < _rooms.Count; i++)
                {
                    if (i == StartRoomIndex || i == ExtractionRoomIndex ||
                        _rooms[i].pattern == RoomPattern.Safe)
                    {
                        continue;
                    }

                    if (target < 0 || _rooms[i].distanceFromStart < _rooms[target].distanceFromStart)
                    {
                        target = i;
                    }
                }

                if (target < 0)
                {
                    // 바꿀 방이 없다(방이 2개뿐인 극단적 시드). 더 돌면 무한이다.
                    break;
                }

                _rooms[target].pattern = RoomPattern.Safe;
                safeCount++;
            }
        }

        private void ApplyPatterns()
        {
            foreach (StageRoom room in _rooms)
            {
                if (room.pattern != RoomPattern.Pillar)
                {
                    continue;
                }

                // 방 중심에는 추출구나 플레이어가 설 수 있다. 기둥이 그 칸을
                // 덮으면 추출구가 벽에 묻혀 도달 불가가 된다(시드 100개 중
                // 12개에서 실제로 났다).
                Vector2Int center = room.Center;
                var keepClear = new RectInt(center.x - 2, center.y - 2, 5, 5);

                int count = RandomRange(_settings.pillarMin, _settings.pillarMax + 1);
                for (int i = 0; i < count; i++)
                {
                    // 테두리에서 2칸 안쪽에만 놓는다 — 문에 바로 붙는 것을 줄인다.
                    int x = room.bounds.x + RandomRange(2, Mathf.Max(3, room.bounds.width - 3));
                    int y = room.bounds.y + RandomRange(2, Mathf.Max(3, room.bounds.height - 3));
                    var block = new RectInt(x, y, 2, 2);
                    if (block.Overlaps(keepClear))
                    {
                        continue;
                    }

                    ClearFloor(block);
                }

                // 보험. 기둥이 방을 갈라 놓으면 통째로 되돌린다. 전체 연결성
                // 검사는 방 "사이" 만 보므로 이건 여기서만 잡힌다.
                if (!RoomFullyConnected(room.bounds))
                {
                    FillFloor(room.bounds);
                }
            }
        }

        // 방 안에서만 플러드필해서 바닥 칸이 전부 한 덩어리인지 본다.
        // 문만 검사하면 구석에 갇힌 바닥 웅덩이를 놓친다 — 거기 스폰된
        // 슬라임은 영영 못 잡는다(시드 100개 중 4개에서 실제로 났다).
        private bool RoomFullyConnected(RectInt room)
        {
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            int floorCount = 0;
            bool started = false;

            for (int x = room.x; x < room.xMax; x++)
            {
                for (int y = room.y; y < room.yMax; y++)
                {
                    if (!IsFloor(x, y))
                    {
                        continue;
                    }

                    floorCount++;
                    if (!started)
                    {
                        started = true;
                        var first = new Vector2Int(x, y);
                        visited.Add(first);
                        queue.Enqueue(first);
                    }
                }
            }

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                TryVisit(room, visited, queue, current.x + 1, current.y);
                TryVisit(room, visited, queue, current.x - 1, current.y);
                TryVisit(room, visited, queue, current.x, current.y + 1);
                TryVisit(room, visited, queue, current.x, current.y - 1);
            }

            return visited.Count == floorCount;
        }

        private void TryVisit(RectInt room, HashSet<Vector2Int> visited, Queue<Vector2Int> queue, int x, int y)
        {
            var cell = new Vector2Int(x, y);
            if (!room.Contains(cell) || !IsFloor(x, y) || !visited.Add(cell))
            {
                return;
            }

            queue.Enqueue(cell);
        }

        // ── 구역(바이옴 섞기) ────────────────────────────────────────────
        // 칸이 아니라 방 단위로 나눈다. 그래야 "이 방은 용암지" 가 딱 떨어져
        // 낙인 트리거가 방마다 콜라이더 하나로 끝난다. 칸 단위면 경계에서
        // CurrentBiomeId 가 초당 수십 번 흔들린다.
        private void AssignRegions(string homeBiomeId, IReadOnlyList<string> foreignBiomeIds)
        {
            int homeSeeds = Mathf.Max(1, _settings.regionSeedCount - 1);
            bool hasForeign = foreignBiomeIds != null && foreignBiomeIds.Count > 0;

            for (int attempt = 0; attempt < 4; attempt++)
            {
                var seeds = new List<(Vector2Int center, string biomeId)>();
                for (int i = 0; i < homeSeeds; i++)
                {
                    seeds.Add((_rooms[_rng.Next(_rooms.Count)].Center, homeBiomeId));
                }

                if (hasForeign)
                {
                    seeds.Add((
                        _rooms[_rng.Next(_rooms.Count)].Center,
                        foreignBiomeIds[_rng.Next(foreignBiomeIds.Count)]));
                }

                int homeRooms = 0;
                foreach (StageRoom room in _rooms)
                {
                    room.biomeId = NearestSeedBiome(room, seeds);
                    if (room.biomeId == homeBiomeId)
                    {
                        homeRooms++;
                    }
                }

                // 시작 방은 항상 홈이어야 한다 — 씨드가 근처에 하나도 안 박히면
                // "홈 씬인데 시작하자마자 외지 바닥" 이 나온다(실측으로 잡음).
                // 나머지 방(추출구 포함)은 그대로 최근접 규칙을 따른다.
                if (_rooms[StartRoomIndex].biomeId != homeBiomeId)
                {
                    _rooms[StartRoomIndex].biomeId = homeBiomeId;
                    homeRooms++;
                }

                if (!hasForeign || (float)homeRooms / _rooms.Count >= _settings.homeShareMin)
                {
                    return;
                }

                // 홈이 모자라면 홈 시드를 하나 더 얹고 다시 굴린다.
                homeSeeds++;
            }
        }

        private static string NearestSeedBiome(StageRoom room, List<(Vector2Int center, string biomeId)> seeds)
        {
            string best = seeds[0].biomeId;
            float bestDistance = float.MaxValue;
            foreach ((Vector2Int center, string biomeId) in seeds)
            {
                float d = Vector2Int.Distance(room.Center, center);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = biomeId;
                }
            }

            return best;
        }

        // ── 벽 ───────────────────────────────────────────────────────────
        // 바닥 아닌 칸 전부를 벽으로 채우면 10,800칸 중 7,000칸이 벽이 되어
        // 콜라이더 병합 비용이 그만큼 든다. 바닥 둘레만 두른다.
        private void BuildWallBand()
        {
            int band = _settings.wallBand;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (_floor[x, y] || !HasFloorWithin(x, y, band))
                    {
                        continue;
                    }

                    _wall[x, y] = true;
                }
            }
        }

        private bool HasFloorWithin(int x, int y, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (IsFloor(x + dx, y + dy))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // 어떤 칸이든 구역(바이옴) 하나로 답한다. 방 안이면 그 방의 구역,
        // 복도 칸(방에 안 속함)이면 가장 가까운 방의 구역을 따른다 — §5-4.
        public string BiomeIdAt(int x, int y)
        {
            var cell = new Vector2Int(x, y);
            foreach (StageRoom room in _rooms)
            {
                if (room.bounds.Contains(cell))
                {
                    return room.biomeId;
                }
            }

            string nearest = _rooms[0].biomeId;
            float bestDistance = float.MaxValue;
            foreach (StageRoom room in _rooms)
            {
                float d = Vector2Int.Distance(cell, room.Center);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    nearest = room.biomeId;
                }
            }

            return nearest;
        }

        // ── 스폰 후보 ────────────────────────────────────────────────────
        // 벽에 붙은 칸은 뺀다 — 슬라임이 벽에 끼여 밀려나는 것처럼 보인다.
        public List<Vector2Int> SpawnCandidates(int roomIndex)
        {
            var candidates = new List<Vector2Int>();
            RectInt bounds = _rooms[roomIndex].bounds;
            Vector2Int player = PlayerSpawn;

            for (int x = bounds.x; x < bounds.xMax; x++)
            {
                for (int y = bounds.y; y < bounds.yMax; y++)
                {
                    if (!IsFloor(x, y) || !AllNeighboursFloor(x, y))
                    {
                        continue;
                    }

                    if (Vector2Int.Distance(new Vector2Int(x, y), player) < _settings.spawnSafeRadius)
                    {
                        continue;
                    }

                    candidates.Add(new Vector2Int(x, y));
                }
            }

            return candidates;
        }

        private bool AllNeighboursFloor(int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (!IsFloor(x + dx, y + dy))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // ── 격자 조작 ────────────────────────────────────────────────────
        private void FillFloor(RectInt area) => SetFloor(area, true);

        private void ClearFloor(RectInt area) => SetFloor(area, false);

        private void SetFloor(RectInt area, bool value)
        {
            for (int x = area.x; x < area.xMax; x++)
            {
                for (int y = area.y; y < area.yMax; y++)
                {
                    if (x < 0 || y < 0 || x >= Width || y >= Height)
                    {
                        continue;
                    }

                    _floor[x, y] = value;
                }
            }
        }

        private int RandomRange(int minInclusive, int maxExclusive) =>
            maxExclusive <= minInclusive ? minInclusive : _rng.Next(minInclusive, maxExclusive);
    }
}

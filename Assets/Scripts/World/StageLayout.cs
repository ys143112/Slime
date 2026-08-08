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

        // 복도가 진행 방향 직각으로 흔들리는 최대 칸수(사행). 0 이면 예전처럼
        // 완벽한 직선이다.
        public int corridorMeander = 2;

        // 사행·폭이 몇 칸마다 바뀌는지. 작을수록 자주 꿈틀댄다.
        public int corridorMeanderScale = 7;

        // 방 경계 칸이 침식될 확률. 침식 뒤 셀룰러 오토마타로 다듬어서
        // 자글거림 대신 굴곡이 되게 한다.
        [Range(0f, 1f)] public float edgeErosion = 0.4f;

        // 침식 결과를 다듬는 횟수. 다수결 규칙으로 바꾼 뒤에는 3 회쯤에서
        // 경계가 더 안 움직인다(2 회는 긴 변에 돌기가 몇 개 남았다).
        public int erosionSmoothPasses = 3;

        // 벽 밴드 두께가 흔들리는 폭(±칸). 0 이면 어디나 균일한 wallBand.
        //
        // 0 으로 둔다(사용자, 2026-08-08). 두께를 흔들면 숲 바깥선이 덜 인공적
        // 이지만, 실제로는 "어떤 데는 너무 두껍고 어떤 데는 적당" 으로 읽혔다 —
        // 밴드가 곧 콜라이더라 두께가 들쭉날쭉하면 막히는 느낌도 같이 흔들린다.
        public int wallBandJitter;

        // 바닥에서 이만큼까지만 벽을 깐다. 나머지는 타일 없음(=배경 검정).
        // 1 칸이면 카메라가 지도 밖을 봐서 "잘렸다" 로 읽힌다.
        //
        // 지터를 뺀 만큼 3 으로 올린다. 예전 2 는 지터가 얹혀 2~4 로 나왔는데,
        // 균일하게 2 만 남기면 §5-3 의 "3칸" 보장이 깨져 플레이어가 벽에 붙었을
        // 때 카메라가 지도 밖 빈 공간을 본다.
        public int wallBand = 3;

        // 트리 연결만 쓰면 막다른 길뿐인 지도가 된다.
        public int extraConnections = 3;

        public int minSafeRooms = 2;

        // 모서리 하나가 깎일 확률. 네 모서리를 따로 굴리므로 0.5 면 방 하나당
        // 평균 2개가 깎여 실루엣이 방마다 갈린다.
        [Range(0f, 1f)] public float cornerNotchChance = 0.5f;

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
        private readonly int _seed;
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
            _seed = seed;
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
            ErodeRoomEdges();
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
            NotchCorners(room.bounds);

            _rooms.Add(room);
            _links.Add(new List<int>());
            return _rooms.Count - 1;
        }

        // 모서리를 삼각형으로 깎아 방마다 실루엣을 다르게 만든다. 20개 방이 전부
        // 똑같은 직사각형이면 "아까 그 방인가?" 가 구분이 안 된다 — 안개로 시야가
        // 좁은 지도일수록 방 모양이 유일한 위치 단서다.
        //
        // 복도보다 **먼저** 깎는 것이 핵심이다. 복도는 이 뒤에 FillFloor 로
        // 뚫리므로, 깎아낸 자리를 지나가야 하면 알아서 다시 메운다 — 연결성이
        // 저절로 보장된다(깎은 뒤에 복도를 뚫으면 이 순서가 깨져 길이 끊긴다).
        private void NotchCorners(RectInt room)
        {
            // 네 모서리를 각각 독립적으로 굴린다. 전부 깎이면 팔각형, 하나도 안
            // 깎이면 원래 직사각형 — 그 사이 16가지 실루엣이 나온다.
            var corners = new (int x, int y, int dx, int dy)[]
            {
                (room.x, room.y, 1, 1),
                (room.xMax - 1, room.y, -1, 1),
                (room.x, room.yMax - 1, 1, -1),
                (room.xMax - 1, room.yMax - 1, -1, -1),
            };

            // 방의 짧은 변 기준으로 깊이를 제한한다 — 작은 방에서 크게 깎으면
            // 남는 바닥이 마름모가 되어 싸울 자리가 사라진다.
            int maxDepth = Mathf.Clamp(Mathf.Min(room.width, room.height) / 4, 2, 5);

            foreach ((int cx, int cy, int dx, int dy) in corners)
            {
                if (_rng.NextDouble() >= _settings.cornerNotchChance)
                {
                    continue;
                }

                int depth = RandomRange(2, maxDepth + 1);
                for (int j = 0; j < depth; j++)
                {
                    for (int i = 0; i < depth - j; i++)
                    {
                        SetFloor(new RectInt(cx + dx * i, cy + dy * j, 1, 1), false);
                    }
                }
            }
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
            var corner = new Vector2Int(to.x, from.y);
            CarveCorridor(from, corner);
            CarveCorridor(corner, to);

            // 꺾이는 지점을 넉넉히 메운다. 두 구간이 서로 다른 축으로 사행하므로
            // 이음매에서 어긋나 한 칸만 겹치거나 아예 끊길 수 있다 — 여기만은
            // 사행 없이 통째로 뚫어 확실히 잇는다.
            int pad = _settings.corridorWidth / 2 + _settings.corridorMeander;
            FillFloor(new RectInt(
                corner.x - pad, corner.y - pad, pad * 2 + 1, pad * 2 + 1));

            _links[a].Add(b);
            _links[b].Add(a);
        }

        // 완벽한 직선 + 고정 폭이면 "방을 파이프로 이은" 인공물처럼 보인다.
        // 진행 방향과 직각으로 노이즈만큼 흔들고(사행), 폭도 칸마다 바꾼다.
        //
        // 연결성은 저절로 보장된다: 한 걸음이 1칸이고 흔들림도 한 걸음에 최대
        // 1칸이므로, 연속한 두 걸음이 칠하는 사각형이 반드시 겹친다.
        private void CarveCorridor(Vector2Int from, Vector2Int to)
        {
            int stepX = Math.Sign(to.x - from.x);
            int stepY = Math.Sign(to.y - from.y);

            var cursor = from;
            int previousOffset = 0;
            while (true)
            {
                // 노이즈에 커서의 **두 좌표를 다** 넣는다. 예전엔 진행축 하나를
                // 두 번 넣어서(CoarseNoise(t, t, ...)) 사행 프로파일이 x 하나의
                // 함수였다 — 서로 다른 y 에 있는 가로 복도가 전부 똑같이 꿈틀대
                // 지도를 넓게 보면 복도들이 동기화되어 물결쳤다. 자연스럽게
                // 만들려던 장치가 "기계가 그렸다" 는 신호를 냈다
                // (팀 리뷰 STAGE_MAP_REVIEW.md C1, 2026-08-08).
                int rawOffset = Mathf.RoundToInt(
                    (CoarseNoise(cursor.x, cursor.y, _settings.corridorMeanderScale, 5501) - 0.5f) * 2f
                    * _settings.corridorMeander);

                // 한 걸음에 1칸 넘게 못 뛰게 묶는다 — 뛰면 칠한 사각형이 끊긴다.
                int offset = Mathf.Clamp(rawOffset, previousOffset - 1, previousOffset + 1);
                previousOffset = offset;

                int width = _settings.corridorWidth +
                    (CoarseNoise(cursor.x, cursor.y, _settings.corridorMeanderScale, 9203) < 0.35f ? -1 : 0);
                width = Mathf.Max(2, width);
                int half = width / 2;

                int cx = cursor.x + (stepX != 0 ? 0 : offset);
                int cy = cursor.y + (stepX != 0 ? offset : 0);
                FillFloor(new RectInt(cx - half, cy - half, width, width));

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

        // ── 가장자리 침식 ────────────────────────────────────────────────
        // 방이 완벽한 직사각형이면 자로 그은 듯한 직선 경계가 나온다 — 지도가
        // 인공물처럼 보이는 가장 큰 원인이다. 경계 칸을 노이즈로 갉아낸 뒤
        // 셀룰러 오토마타로 다듬어 "숲속 빈터" 모양으로 바꾼다.
        //
        // 칸 단위로만 깎으므로 콜라이더는 보이는 그림과 정확히 일치한다
        // (그림의 알파로 깎으면 §9-1 의 "밟힐 것 같은데 안 밟힘" 이 재발한다).
        //
        // 복도는 건드리지 않는다 — 폭 3이 2나 1로 좁아지면 길이 막힌다.
        // 방 안쪽만 깎고, 복도와 맞닿은 칸(문)은 남긴다.
        private void ErodeRoomEdges()
        {
            if (_settings.edgeErosion <= 0f)
            {
                return;
            }

            // 되돌릴 수 있게 원본을 떠 둔다. 침식이 방을 끊어 놓으면 통째로
            // 복구한다 — 생성이 3ms 라 되돌리는 비용이 사실상 공짜다.
            bool[,] backup = (bool[,])_floor.Clone();

            foreach (StageRoom room in _rooms)
            {
                ErodeRoom(room.bounds);
                for (int pass = 0; pass < _settings.erosionSmoothPasses; pass++)
                {
                    SmoothRoom(room.bounds);
                }
            }

            if (!IsEveryRoomReachable())
            {
                Array.Copy(backup, _floor, backup.Length);
                return;
            }

            PruneDisconnectedFloor();
        }

        // 침식은 방 가장자리를 갉으면서 본체에서 떨어져 나온 바닥 조각을 남긴다
        // (실측: 시드 200개에서 방 81개). 거기 스폰된 슬라임은 영영 못 잡으므로
        // 도달 못 하는 바닥은 전부 벽으로 되돌린다.
        //
        // 셀룰러 오토마타를 쓸 때 반드시 따라와야 하는 연결성 패스다 — 규칙만
        // 돌리고 끝내면 갈 수 없는 웅덩이가 남는 게 이 기법의 알려진 함정이다.
        private void PruneDisconnectedFloor()
        {
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            Vector2Int start = _rooms[StartRoomIndex].Center;

            visited.Add(start);
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                TryVisitGlobal(visited, queue, c.x + 1, c.y);
                TryVisitGlobal(visited, queue, c.x - 1, c.y);
                TryVisitGlobal(visited, queue, c.x, c.y + 1);
                TryVisitGlobal(visited, queue, c.x, c.y - 1);
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (_floor[x, y] && !visited.Contains(new Vector2Int(x, y)))
                    {
                        _floor[x, y] = false;
                    }
                }
            }
        }

        private void ErodeRoom(RectInt room)
        {
            var doomed = new List<Vector2Int>();
            for (int x = room.x; x < room.xMax; x++)
            {
                for (int y = room.y; y < room.yMax; y++)
                {
                    if (!IsFloor(x, y) || !IsEdgeFloor(x, y) || TouchesCorridor(x, y, room))
                    {
                        continue;
                    }

                    if (Noise(x, y, 1301) < _settings.edgeErosion)
                    {
                        doomed.Add(new Vector2Int(x, y));
                    }
                }
            }

            // 한 번에 지운다 — 훑는 도중에 지우면 앞서 지운 칸이 뒤 칸의 판정을
            // 바꿔 한쪽 방향으로만 깎여 나간다.
            foreach (Vector2Int cell in doomed)
            {
                _floor[cell.x, cell.y] = false;
            }
        }

        // 셀룰러 오토마타 다듬기. 칸별 백색잡음으로 깎으면 가장자리가 소금 뿌린
        // 듯 자글거린다 — 이웃 수를 세어 튀어나온 칸은 깎고 움푹 팬 칸은 메우면
        // 그 자글거림이 완만한 굴곡으로 바뀐다.
        private void SmoothRoom(RectInt room)
        {
            var toFloor = new List<Vector2Int>();
            var toWall = new List<Vector2Int>();

            for (int x = room.x; x < room.xMax; x++)
            {
                for (int y = room.y; y < room.yMax; y++)
                {
                    if (TouchesCorridor(x, y, room))
                    {
                        continue;
                    }

                    // 다수결(4-5 규칙). 예전엔 3 이하만 깎고 6 이상만 메워서
                    // 이웃이 4~5인 칸이 손도 안 닿은 채 남았다 — 그 칸들이
                    // 경계에 한 칸씩 튀어나와 벽 테두리가 사방으로 들쭉날쭉해
                    // 보였다(팀 QA, 2026-08-08: "여러 방향과 타일이 섞였음").
                    // 빈 구간 없이 이웃 수만으로 가르면 경계가 직선이나 완만한
                    // 곡선으로 수렴한다. 면적은 거의 안 변한다(대칭 규칙).
                    int neighbours = CountFloorNeighbours(x, y);
                    if (IsFloor(x, y))
                    {
                        if (neighbours <= 4) toWall.Add(new Vector2Int(x, y));
                    }
                    else
                    {
                        if (neighbours >= 5) toFloor.Add(new Vector2Int(x, y));
                    }
                }
            }

            foreach (Vector2Int c in toWall) _floor[c.x, c.y] = false;
            foreach (Vector2Int c in toFloor) _floor[c.x, c.y] = true;
        }

        private int CountFloorNeighbours(int x, int y)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (IsFloor(x + dx, y + dy)) count++;
                }
            }

            return count;
        }

        private bool IsEdgeFloor(int x, int y)
        {
            return !IsFloor(x + 1, y) || !IsFloor(x - 1, y) ||
                !IsFloor(x, y + 1) || !IsFloor(x, y - 1);
        }

        // 방 밖의 바닥 = 복도. 그 옆 칸을 깎으면 문이 막힌다.
        private bool TouchesCorridor(int x, int y, RectInt room)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    if (IsFloor(nx, ny) && !room.Contains(new Vector2Int(nx, ny)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsEveryRoomReachable()
        {
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            Vector2Int start = _rooms[StartRoomIndex].Center;
            if (!IsFloor(start.x, start.y))
            {
                return false;
            }

            visited.Add(start);
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                TryVisitGlobal(visited, queue, c.x + 1, c.y);
                TryVisitGlobal(visited, queue, c.x - 1, c.y);
                TryVisitGlobal(visited, queue, c.x, c.y + 1);
                TryVisitGlobal(visited, queue, c.x, c.y - 1);
            }

            foreach (StageRoom room in _rooms)
            {
                bool reached = false;
                for (int x = room.bounds.x; x < room.bounds.xMax && !reached; x++)
                {
                    for (int y = room.bounds.y; y < room.bounds.yMax && !reached; y++)
                    {
                        if (visited.Contains(new Vector2Int(x, y)))
                        {
                            reached = true;
                        }
                    }
                }

                if (!reached)
                {
                    return false;
                }
            }

            return true;
        }

        private void TryVisitGlobal(HashSet<Vector2Int> visited, Queue<Vector2Int> queue, int x, int y)
        {
            var cell = new Vector2Int(x, y);
            if (!IsFloor(x, y) || !visited.Add(cell))
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
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (_floor[x, y] || !HasFloorWithin(x, y, BandRadiusAt(x, y)))
                    {
                        continue;
                    }

                    _wall[x, y] = true;
                }
            }
        }

        // 두께가 어디나 3칸으로 똑같으면 숲 바깥선이 자로 잰 듯 평행하게 흐른다.
        // 덩어리 단위 노이즈로 흔들어 두껍고 얇은 데를 만든다 — 밴드는 바깥으로만
        // 자라므로 걸어다닐 수 있는 범위(바닥)는 이 값과 무관하다.
        private int BandRadiusAt(int x, int y)
        {
            if (_settings.wallBandJitter <= 0)
            {
                return _settings.wallBand;
            }

            // 두껍게만 흔든다(0 ~ +jitter). 얇아지면 §5-3 의 "3칸" 보장이 깨져
            // 플레이어가 벽에 붙었을 때 카메라가 지도 밖 빈 공간을 본다.
            float n = CoarseNoise(x, y, 6, 4409);
            return _settings.wallBand + Mathf.RoundToInt(n * _settings.wallBandJitter);
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

        // 직교 4방향만 본다. 예전엔 대각선까지 8방향 전부를 요구했는데, 가장자리
        // 침식(§9-6)으로 경계가 들쭉날쭉해지자 후보가 방당 1칸까지 떨어졌다 —
        // 대각선이 벽이어도 슬라임은 끼지 않으므로 그 조건은 과했다.
        private bool AllNeighboursFloor(int x, int y)
        {
            return IsFloor(x + 1, y) && IsFloor(x - 1, y) &&
                IsFloor(x, y + 1) && IsFloor(x, y - 1);
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

        // 좌표 기반 해시 노이즈. _rng(System.Random)는 "부르는 순서"에 결과가
        // 달리지만 이건 좌표만 보므로, 어느 패스에서 언제 물어도 같은 칸은 같은
        // 값이 나온다 — 침식·밴드 두께처럼 격자를 훑는 작업에 필요하다.
        private float Noise(int x, int y, int salt)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + (_seed + salt) * -2048144777;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        // 칸 하나가 아니라 scale 칸짜리 덩어리 단위로 값이 바뀌는 노이즈.
        // 칸별 백색잡음을 그대로 쓰면 가장자리가 소금 뿌린 듯 자글거린다 —
        // 자연스러운 굴곡은 여러 칸이 함께 움직여야 나온다.
        private float CoarseNoise(int x, int y, int scale, int salt)
        {
            int cellX = Mathf.FloorToInt((float)x / scale);
            int cellY = Mathf.FloorToInt((float)y / scale);
            return Noise(cellX, cellY, salt);
        }

        private int RandomRange(int minInclusive, int maxExclusive) =>
            maxExclusive <= minInclusive ? minInclusive : _rng.Next(minInclusive, maxExclusive);
    }
}

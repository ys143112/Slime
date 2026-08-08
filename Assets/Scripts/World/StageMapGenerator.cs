using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace Game.Gameplay
{
    // 기능: 스테이지 맵 생성 (STAGE_A_DESIGN.md §4, §7)
    // StageLayout 의 계산 결과를 실제 타일맵에 그리고 플레이어·추출구를 그
    // 위로 옮긴다. 그리기만 담당한다 — 방/복도 계산은 StageLayout, 슬라임
    // 배치는 SlimeSpawner(§10 5단계)가 이 오브젝트의 Layout 을 읽어 이어받는다.
    public sealed class StageMapGenerator : MonoBehaviour
    {
        [SerializeField] private Tilemap groundTilemap;
        [SerializeField] private Tilemap wallsTilemap;
        [SerializeField] private Tilemap fogTilemap;
        [SerializeField] private Tilemap decorTilemap;

        [Tooltip("나무 프롭용. Individual 모드 + sortingOrder 0 이라야 Y 정렬이 먹는다.")]
        [SerializeField] private Tilemap propsTilemap;

        [Tooltip("이 씬의 홈 바이옴. biomeId 가 BiomeCatalog 와 일치해야 한다.")]
        [SerializeField] private StagePalette homePalette;

        [Tooltip("섞어 넣을 외지 바이옴 후보. 방의 40% 이내를 나눠 갖는다.")]
        [SerializeField] private StagePalette[] foreignPalettes = Array.Empty<StagePalette>();

        [SerializeField] private Transform player;
        [SerializeField] private GameObject extractionPoint;

        [SerializeField] private StageLayoutSettings settings = new StageLayoutSettings();

        [Tooltip("켜면 fixedSeed 로 고정된다 — 같은 지도를 반복 재현할 때만 쓴다.")]
        [SerializeField] private bool useFixedSeed;
        [SerializeField] private int fixedSeed;

        // 두 방의 "가장 가까운 방까지 거리" 차이가 이 값보다 작은 칸(복도·벽
        // 밴드에서만)은 두 바이옴을 섞어 찍는다 — 하드 경계 대신 점묘 전환을
        // 만든다(§9-4). 방 안쪽은 절대 안 섞는다: 방 하나는 늘 한 바이옴이어야
        // SlimeSpawner·낙인이 보는 room.biomeId 와 눈으로 보이는 그림이 어긋나지
        // 않는다.
        [SerializeField] private float regionBlendRadius = 5f;

        [Tooltip("방 바닥 중 장식이 놓이는 비율. 0.05 = 20칸에 1개꼴.")]
        [Range(0f, 0.3f)] [SerializeField] private float decorDensity = 0.06f;

        [Tooltip("나무 프롭 사이 최소 간격(칸). 캐노피가 겹치되 뭉개지지 않을 값.")]
        [SerializeField] private int treeSpacing = 3;

        private Dictionary<string, StagePalette> _palettesByBiome;
        private Transform _regionsParent;
        private int _currentSeed;

        public StageLayout Layout { get; private set; }

        /// <summary>지도를 다 그린 뒤에 한 번. 스포너가 이걸 듣는다.</summary>
        /// <remarks>
        /// 예전엔 <c>SlimeSpawner.Start()</c> 가 "생성기 Start 가 먼저 돌았겠지"
        /// 하고 <c>Layout</c> 을 읽었다. Unity 는 다른 GameObject 의 Start 순서를
        /// 보장하지 않으므로, 뒤집히면 에러 한 줄 남기고 슬라임이 0마리가 된다
        /// (팀 리뷰 STAGE_MAP_REVIEW.md D7, 2026-08-08). 구독은 Awake 에서 하고
        /// 발행은 Start 에서 하므로 순서가 보장된다.
        /// </remarks>
        public event Action MapGenerated;

        // SlimeSpawner 가 셀 좌표를 월드 좌표로 바꿀 때 쓴다. PlaceActors 가
        // 플레이어·추출구를 놓을 때 쓰는 것과 같은 API 로 통일한다.
        public Tilemap GroundTilemap => groundTilemap;

        private void Start()
        {
            Generate();
        }

#if UNITY_EDITOR
        // §8 "런타임 재생성 키" — Play 중 R 로 시드를 다시 굴려 눈으로 확인한다.
        // 씬 편집 없이 지도 결과만 반복해서 볼 수 있게 하려는 것이다.
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                useFixedSeed = false;
                Generate();
            }
        }
#endif

        public void Generate()
        {
            BuildPaletteLookup();

            int seed = useFixedSeed ? fixedSeed : Environment.TickCount;
            _currentSeed = seed; // PaletteAt 의 경계 디더링이 같은 시드로 매번 같은 무늬를 내게 한다
            var foreignIds = new string[foreignPalettes.Length];
            for (int i = 0; i < foreignPalettes.Length; i++)
            {
                foreignIds[i] = foreignPalettes[i].biomeId;
            }

            Layout = StageLayout.Generate(settings, seed, homePalette.biomeId, foreignIds);

            groundTilemap.ClearAllTiles();
            wallsTilemap.ClearAllTiles();
            fogTilemap.ClearAllTiles();

            // 손배치 시절(24×16 맵) 장식이 좌표만 남아 지금 지도의 벽 속에
            // 유령처럼 박혀 있었다 — 여기서 안 지우면 생성 결과에 계속 섞인다.
            if (decorTilemap != null)
            {
                decorTilemap.ClearAllTiles();
            }

            if (propsTilemap != null)
            {
                propsTilemap.ClearAllTiles();
            }

            PaintFloorAndFog();
            PaintWalls();
            PaintDecor();
            PaintTreeProps();
            PlaceActors();
            PlaceRegionTriggers();

            Debug.Log($"stage_generated seed={seed} rooms={Layout.Rooms.Count}");
            MapGenerated?.Invoke();
        }

        private void BuildPaletteLookup()
        {
            _palettesByBiome = new Dictionary<string, StagePalette> { [homePalette.biomeId] = homePalette };
            foreach (StagePalette palette in foreignPalettes)
            {
                _palettesByBiome[palette.biomeId] = palette;
            }
        }

        // 지형은 벽 칸만 따로 칠하지 않고, 지도 전체를 wang 16장으로 한 번에
        // 그린다. 이 타일셋의 upper 지형이 곧 "둔덕" 이다 — 흙·바위 턱이 그림에
        // 이미 들어 있어서, 경계 칸에 블렌드 그림이 와야 땅이 솟아오른 것으로
        // 읽힌다. 예전처럼 바닥은 [0], 벽은 단색 그림자로 칠하면 턱이 한 번도
        // 안 나와 평평한 카펫으로 보였다(사용자 신고, 2026-08-08).
        //
        // 벽 밴드가 좌우 반 칸씩 바닥 쪽으로 번져 보이지만 충돌은 IsWall 그대로다
        // — 번지는 부분이 턱의 비탈이라 걸어 들어갈 수 있는 게 맞다.
        private void PaintFloorAndFog()
        {
            for (int x = 0; x < Layout.Width; x++)
            {
                for (int y = 0; y < Layout.Height; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    StagePalette palette = PaletteAt(x, y);
                    groundTilemap.SetTile(cell, palette.wangBlend[WangIndexAt(x, y)]);

                    // FogOfWarReveal 은 "이미 타일이 있는 칸만" 걷어낸다. 여기서
                    // 안 채우면 에러 없이 안개가 통째로 사라진다(STAGE_A_DESIGN §9).
                    // 가장 짙은 단계로 깔아 시작 시점엔 전부 안 보이게 한다.
                    //
                    // 벽 칸까지 덮는다. 예전엔 바닥에만 깔았는데, 벽이 단색
                    // 그림자였을 때는 티가 안 났지만 이제 벽이 숲 그림이라
                    // 걸어보지도 않은 지도의 숲 윤곽이 처음부터 다 보인다 —
                    // 가려야 할 것이 오히려 안 가려진다.
                    if (palette.fogLevels.Length > 0)
                    {
                        fogTilemap.SetTile(cell, palette.fogLevels[palette.fogLevels.Length - 1]);
                    }
                }
            }
        }

        // wang 코너 인덱스: index = NW*8 + NE*4 + SW*2 + SE*1, 비트 1 = upper(둔덕).
        //
        // 코너 하나는 그 코너에 닿는 **네 칸이 전부** 벽일 때만 upper 다.
        //
        // 4코너 wang 은 그림 경계가 충돌 경계와 반 칸 어긋나는 것이 구조적이라,
        // 어느 쪽으로 어긋나게 할지만 고를 수 있다. 두 선택지를 다 만들어 보고
        // 사용자가 이쪽을 골랐다(2026-08-08):
        //
        // - "하나라도 벽": 그림이 바닥 쪽으로 번진다 → 벽 그림 안으로 반 칸
        //   걸어 들어가진다. 오목한 모서리에서 특히 심해 "벽에 파고든다" 로 보인다.
        // - "네 칸 전부"(지금): 그림이 벽 쪽으로 물러난다 → 벽 앞에서 반 칸
        //   못 미쳐 멈춘다. 걸을 수 있어 보이는 곳이 안 걸릴 뿐, 벽 속에 들어가진
        //   않는다.
        //
        // 두께 1칸짜리 벽은 어느 코너도 네 칸을 못 채워 바닥으로 그려진다 —
        // 그 칸은 PaintWalls 가 wallShadow 로 따로 덮는다.
        private int WangIndexAt(int x, int y)
        {
            int nw = CornerUpper(x, y + 1) ? 8 : 0;
            int ne = CornerUpper(x + 1, y + 1) ? 4 : 0;
            int sw = CornerUpper(x, y) ? 2 : 0;
            int se = CornerUpper(x + 1, y) ? 1 : 0;
            return nw + ne + sw + se;
        }

        private bool CornerUpper(int cx, int cy)
        {
            return CellUpper(cx - 1, cy - 1) && CellUpper(cx, cy - 1) &&
                CellUpper(cx - 1, cy) && CellUpper(cx, cy);
        }

        // 지도 밖은 둔덕으로 친다 — 그래야 바깥 테두리가 열린 채로 끝나지 않는다.
        //
        // **`IsWall` 이 아니라 `IsFloor` 를 본다.** `_wall` 은 성능 때문에 바닥
        // 둘레 3칸만 채운 *띠* 라서, 띠 바깥 칸은 바닥도 벽도 아니다. 그 칸을
        // "안 막힘" 으로 읽으면 wang 인덱스 0 이 나오고, 인덱스 0 은 걸어다니는
        // 바닥과 **정확히 같은 그림**이다 — 갈 수 없는 곳이 갈 수 있는 풀밭으로
        // 보였다(팀 리뷰 STAGE_MAP_REVIEW.md A1, 2026-08-08).
        //
        // 걸을 수 있는가만 보면 띠 바깥이 전부 막힌 그림이 된다. 콜라이더는
        // 그대로 띠에만 있지만, 그 칸들은 띠 **뒤**라 애초에 도달할 수 없다 —
        // 플레이어가 갈 수 있는 모든 자리에서 "보이는 대로 막힌다" 가 유지된다.
        //
        // 프롭 후보(SurroundedByWall)도 같이 넓어진다. 예전엔 후보가 띠 두께에
        // 묶여 폭 1~3칸짜리 리본이었다(같은 문서 B1).
        private bool CellUpper(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Layout.Width || y >= Layout.Height)
            {
                return true;
            }

            return !Layout.IsFloor(x, y);
        }

        private void PaintWalls()
        {
            // 벽 그림은 대부분 Ground 의 wang 이 그린다. 이 타일맵은 충돌을 맡고,
            // **wang 이 못 덮은 칸에서만** 그림도 맡는다 — 그래서 렌더러를 켜 둔다.
            var renderer = wallsTilemap.GetComponent<TilemapRenderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
            }

            // **콜라이더를 그림에 맞춘다.** 기준은 `_wall`(콜라이더용 밴드)이
            // 아니라 "이 칸이 벽으로 그려졌는가"(wang 인덱스 != 0)다.
            //
            // 코너 규칙이 "네 칸 전부" 라 벽 그림은 벽 영역보다 한 칸 안쪽에서
            // 시작한다. 예전처럼 바닥에 맞닿은 겹에 콜라이더를 깔면 그 한 겹이
            // **아무것도 안 그려진 채 막는** 투명한 벽 링이 된다 — 빈 잔디에서
            // 부딪혀 죽었다는 신고가 이것이다(2026-08-08).
            //
            // 그림 기준으로 깔면 플레이어는 벽 영역 첫 칸까지 걸어 들어간 뒤
            // 보이는 벽에서 멈춘다. 걸을 수 있어 보이는 곳은 전부 걸어진다.
            int cells = 0;
            for (int x = 0; x < Layout.Width; x++)
            {
                for (int y = 0; y < Layout.Height; y++)
                {
                    // **완전히 벽으로 그려진 칸(인덱스 15)만** 막는다.
                    //
                    // 인덱스 1~14 는 코너 한두 개만 벽인 블렌드 칸이라 그림의
                    // 대부분이 바닥이다. 거기에 콜라이더를 깔면 칸 전체가 막혀
                    // "빈 잔디에서 부딪힌다" 가 된다 — 콜라이더는 칸 단위인데
                    // 그림은 칸보다 잘게 갈리는 것이 원인이라, 어느 쪽으로 반올림
                    // 할지만 고를 수 있다. 안 막는 쪽으로 반올림하면 블렌드 띠는
                    // 걸어 들어갈 수 있는 비탈이 되고, 멈추는 자리는 그림이 온통
                    // 벽인 곳이다(사용자 신고 두 번 끝에 정한 규칙, 2026-08-08).
                    bool solid = WangIndexAt(x, y) == 15;
                    bool pillar = Layout.IsWall(x, y) && NeedsShadow(x, y);
                    if (!solid && !pillar)
                    {
                        continue;
                    }

                    wallsTilemap.SetTile(new Vector3Int(x, y, 0),
                        pillar ? WallTileAt(x, y) : InvisibleTileAt(x, y));
                    cells++;
                }
            }

            Debug.Log($"stage_walls collider_cells={cells}");
        }

        /// <summary>이 벽 칸에 그림자를 따로 깔아야 하는가.</summary>
        /// <remarks>
        /// 조건이 예전엔 "이 칸의 wang 인덱스가 0" 이었다. 코너 규칙을 "네 칸
        /// 전부" 로 바꾼 뒤로는 <b>바닥과 맞닿은 벽 칸이 전부 인덱스 0</b> 이 되고,
        /// 콜라이더도 그 한 겹만 깐다 — 두 조건이 겹쳐 <b>경계 전체에 그림자
        /// 사각형이 줄줄이 찍혔다</b>(잿벌 스크린샷의 주황 네모들, 2026-08-08).
        ///
        /// 진짜로 필요한 경우는 "그려진 벽이 근처에 아예 없는 칸" — 두께 1칸짜리
        /// 기둥이다. 이웃에 인덱스가 0 이 아닌 칸이 하나라도 있으면 그쪽 그림이
        /// 이 자리를 벽으로 읽히게 해 주므로 그림자가 필요 없다.
        /// </remarks>
        private bool NeedsShadow(int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (WangIndexAt(x + dx, y + dy) != 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // 충돌만 내고 그림은 없는 타일. 팔레트에 없으면 그림자를 그대로 쓴다 —
        // 겹쳐 보이더라도 못 지나간다는 사실이 안 보이는 것보다 낫다.
        private TileBase InvisibleTileAt(int x, int y)
        {
            StagePalette palette = PaletteAt(x, y);
            return palette.invisibleWall != null ? palette.invisibleWall : palette.wallShadow;
        }

        // 벽은 코너마다 그림을 바꾸지 않는다 — 진짜 바깥 모서리 칸(방마다 4개)이
        // "절반 잔디/절반 나무" 블렌드 그림을 그대로 썼더니, 바닥 바로 옆에서
        // 체크무늬로 보여 "바닥에 나무가 자란 것처럼 부자연스럽다" 는 신고를
        // 받았다(실측, 2026-08-08). 벽 밴드 전체를 `wallShadow` 하나로 통일한다
        // — 나무 프롭(Props 층)이 경계의 시각 디테일을 대신 맡고, 벽 자체는
        // "그 뒤가 안 보이는 그림자" 로만 읽히면 된다. §9-6 의 가장자리 침식으로
        // 방 윤곽 자체는 이미 들쭉날쭉하므로, 채우는 그림이 단색이어도 실루엣은
        // 여전히 자연스럽다.
        private TileBase WallTileAt(int x, int y) => PaletteAt(x, y).wallShadow;

        // 돌·풀 같은 작은 장식도 나무와 같이 벽 덩어리 안쪽에만 놓는다(사용자
        // 요청, 2026-08-08). 예전엔 방 바닥에 흩뿌리면서 벽에 붙은 칸을 3배
        // 우선했는데, 걸어다니는 바닥에 그림이 얹히면 밟고 지나갈 수 있는
        // 장식인지 막힌 곳인지 구분이 안 됐다 — 안개 때문에 멀리 못 보는
        // 상황에서 특히. 이제 바닥은 전부 비고, 장식은 벽의 질감이 된다.
        //
        // 방별 밀도 조절(시작 방 절반, 둥지 1.5배)은 같이 없앴다. 그건 "이 방에
        // 뭐가 사는지" 를 바닥 어질러진 정도로 알리는 장치였는데, 장식이 벽으로
        // 옮겨간 이상 방 안에 보이지도 않는다.
        private void PaintDecor()
        {
            if (decorTilemap == null)
            {
                return;
            }

            for (int x = 0; x < Layout.Width; x++)
            {
                for (int y = 0; y < Layout.Height; y++)
                {
                    if (!SurroundedByWall(x, y))
                    {
                        continue;
                    }

                    StagePalette palette = PaletteAt(x, y);
                    if (palette.decorTiles == null || palette.decorTiles.Length == 0)
                    {
                        continue;
                    }

                    // ×5 배수는 후보가 벽 띠 중심선뿐이던 시절의 보정이었다.
                    // CellUpper 를 IsFloor 기준으로 바꾸면서 후보가 면(面)이 됐으니
                    // 같이 뺀다 — 안 빼면 장식이 폭발한다(팀 리뷰 B6).
                    if (CellNoise(x, y, _currentSeed + 7717) >= decorDensity)
                    {
                        continue;
                    }

                    int pick = Mathf.FloorToInt(
                        CellNoise(x, y, _currentSeed + 3391) * palette.decorTiles.Length);
                    pick = Mathf.Clamp(pick, 0, palette.decorTiles.Length - 1);

                    decorTilemap.SetTile(new Vector3Int(x, y, 0), palette.decorTiles[pick]);
                }
            }
        }

        // 벽 덩어리 위에 나무를 세운다. 프롭이 없으면(자산 대기 중) 조용히
        // 넘어가고 지금처럼 캐노피 텍스처만 남는다.
        //
        // 바깥 테두리(바닥과 맞닿은 벽 칸)를 우선한다 — 레퍼런스처럼 캐노피가
        // 걸어다니는 쪽으로 넘어와야 숲 윤곽이 흐려진다. 안쪽은 성기게 채워
        // 덩어리가 뚫려 보이지만 않으면 된다.
        private void PaintTreeProps()
        {
            if (propsTilemap == null)
            {
                return;
            }

            var placed = new List<Vector2Int>();

            for (int x = 0; x < Layout.Width; x++)
            {
                for (int y = 0; y < Layout.Height; y++)
                {
                    // 벽 밴드 안쪽에만 세운다. 예전엔 바닥과 맞닿은 칸을 오히려
                    // 우선했는데, 캐노피가 걸어다니는 쪽으로 넘어와 "바닥에
                    // 나무가 서 있다" 로 보였다(사용자 요청, 2026-08-08).
                    // 여덟 방향이 전부 벽인 칸만 남기면 그림이 벽 밖으로 안 샌다.
                    if (!SurroundedByWall(x, y))
                    {
                        continue;
                    }

                    StagePalette palette = PaletteAt(x, y);
                    if (palette.treeProps == null || palette.treeProps.Length == 0)
                    {
                        continue;
                    }

                    if (CellNoise(x, y, _currentSeed + 6421) >= 0.75f)
                    {
                        continue;
                    }

                    var cell = new Vector2Int(x, y);
                    if (TooCloseToTree(cell, placed))
                    {
                        continue;
                    }

                    int pick = Mathf.FloorToInt(
                        CellNoise(x, y, _currentSeed + 1187) * palette.treeProps.Length);
                    pick = Mathf.Clamp(pick, 0, palette.treeProps.Length - 1);

                    propsTilemap.SetTile(new Vector3Int(x, y, 0), palette.treeProps[pick]);
                    placed.Add(cell);
                }
            }
        }

        // 8방향 이웃 중 하나라도 바닥인가. 대각까지 보는 이유는 대각으로 빠져나가는
        // 경로를 막기 위해서다 — 직교 4방향만 보면 벽 모서리에 1칸짜리 구멍이 남는다.
        private bool TouchesFloor(int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (Layout.IsFloor(x + dx, y + dy))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // 자기 자신과 8방향 이웃이 전부 벽인가. 지도 밖은 벽으로 친다.
        private bool SurroundedByWall(int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (!CellUpper(x + dx, y + dy))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool TooCloseToTree(Vector2Int cell, List<Vector2Int> placed)
        {
            foreach (Vector2Int other in placed)
            {
                if (Mathf.Abs(other.x - cell.x) < treeSpacing &&
                    Mathf.Abs(other.y - cell.y) < treeSpacing)
                {
                    return true;
                }
            }

            return false;
        }

        private void PlaceActors()
        {
            if (player != null)
            {
                player.position = FloorCenter(Layout.PlayerSpawn, "player");
            }

            if (extractionPoint != null)
            {
                extractionPoint.transform.position = FloorCenter(Layout.ExtractionPoint, "extraction");
            }
        }

        // 방 중심이 바닥이 아닐 경로는 지금 로직상 막혀 있다고 읽히지만, "지금
        // 로직상 안 걸린다" 와 "영원히 안 걸린다" 는 다르다. 세 씬 전부 겪었던
        // "추출구가 벽 밖" 사고의 재발 비용에 비하면 이 검사가 훨씬 싸다
        // (팀 리뷰 STAGE_MAP_REVIEW.md A5, 2026-08-08).
        private Vector3 FloorCenter(Vector2Int cell, string what)
        {
            if (!Layout.IsFloor(cell.x, cell.y))
            {
                Vector2Int rescued = NearestFloor(cell);
                Debug.LogError($"stage_actor_off_floor what={what} cell={cell} -> {rescued}");
                cell = rescued;
            }

            return groundTilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
        }

        private Vector2Int NearestFloor(Vector2Int from)
        {
            int max = Mathf.Max(Layout.Width, Layout.Height);
            for (int r = 1; r < max; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        // 껍질만 훑는다 — 안쪽은 더 작은 r 에서 이미 봤다.
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r)
                        {
                            continue;
                        }

                        if (Layout.IsFloor(from.x + dx, from.y + dy))
                        {
                            return new Vector2Int(from.x + dx, from.y + dy);
                        }
                    }
                }
            }

            return from;
        }

        // 방마다 트리거 하나 — 밟은 구역을 GameManager.CurrentBiomeId 로 흘려
        // 낙인(spec-004)·오염 유전(spec-006)이 "지금 서 있는 곳" 을 보게 한다.
        // 복도는 트리거가 없다 — 직전 방의 구역이 그대로 유지된다(§9 부채).
        private void PlaceRegionTriggers()
        {
            if (_regionsParent != null)
            {
                Destroy(_regionsParent.gameObject);
            }

            _regionsParent = new GameObject("Regions").transform;
            _regionsParent.SetParent(transform);

            foreach (StageRoom room in Layout.Rooms)
            {
                var go = new GameObject($"Region_{room.biomeId}");
                go.transform.SetParent(_regionsParent);

                Vector3 min = groundTilemap.GetCellCenterWorld(new Vector3Int(room.bounds.x, room.bounds.y, 0));
                Vector3 max = groundTilemap.GetCellCenterWorld(
                    new Vector3Int(room.bounds.xMax - 1, room.bounds.yMax - 1, 0));
                go.transform.position = (min + max) / 2f;

                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(room.bounds.width, room.bounds.height);

                go.AddComponent<StageRegionTrigger>().BiomeId = room.biomeId;
            }
        }

        // 방 안이면 그 방의 팔레트를 확정으로 쓴다 — 방 하나가 두 그림으로
        // 섞이면 room.biomeId(스포너·낙인이 읽는 값)와 눈으로 보이는 바닥이
        // 어긋난다. 방 밖(복도·벽 밴드)만 가장 가까운 두 방 사이를 디더링해서
        // 하드 경계를 없앤다(§9-4) — Layout.BiomeIdAt 은 그대로 두고(방 트리거가
        // 여전히 그 단순한 최근접 규칙을 쓴다), 그리기 전용으로 여기서만 섞는다.
        private StagePalette PaletteAt(int x, int y)
        {
            var cell = new Vector2Int(x, y);
            foreach (StageRoom room in Layout.Rooms)
            {
                if (room.bounds.Contains(cell))
                {
                    return LookupPalette(room.biomeId);
                }
            }

            StageRoom nearest = null;
            StageRoom second = null;
            float nearestDist = float.MaxValue;
            float secondDist = float.MaxValue;
            foreach (StageRoom room in Layout.Rooms)
            {
                float d = Vector2Int.Distance(cell, room.Center);
                if (d < nearestDist)
                {
                    second = nearest;
                    secondDist = nearestDist;
                    nearest = room;
                    nearestDist = d;
                }
                else if (d < secondDist)
                {
                    second = room;
                    secondDist = d;
                }
            }

            if (second == null || nearest.biomeId == second.biomeId ||
                secondDist - nearestDist >= regionBlendRadius)
            {
                return LookupPalette(nearest.biomeId);
            }

            // 두 방 거리 차가 0에 가까울수록 절반절반(0.5), regionBlendRadius 에
            // 가까울수록 가까운 방 쪽으로 기운다(1.0) — 경계 한가운데가 가장
            // 뒤섞이고 멀어질수록 한쪽으로 수렴한다.
            float nearProbability = 0.5f + 0.5f * ((secondDist - nearestDist) / regionBlendRadius);
            float noise = CellNoise(x, y, _currentSeed);
            return LookupPalette(noise < nearProbability ? nearest.biomeId : second.biomeId);
        }

        private StagePalette LookupPalette(string biomeId) =>
            _palettesByBiome.TryGetValue(biomeId, out StagePalette palette) ? palette : homePalette;

        // 상태 없는 해시 — 같은 (x,y,seed) 는 언제 불러도 같은 값을 낸다. 방향
        // 상관없이 안정적이어야 해서(바닥 먼저, 벽 나중에 훑어도 같은 칸은 같은
        // 결과) System.Random 대신 이 방식을 쓴다.
        private static float CellNoise(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * -2048144777; // 2246822519 를 int 로 감쌈
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)int.MaxValue;
            }
        }
    }
}

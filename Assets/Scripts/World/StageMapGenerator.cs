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

        private Dictionary<string, StagePalette> _palettesByBiome;
        private Transform _regionsParent;

        public StageLayout Layout { get; private set; }

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
            var foreignIds = new string[foreignPalettes.Length];
            for (int i = 0; i < foreignPalettes.Length; i++)
            {
                foreignIds[i] = foreignPalettes[i].biomeId;
            }

            Layout = StageLayout.Generate(settings, seed, homePalette.biomeId, foreignIds);

            groundTilemap.ClearAllTiles();
            wallsTilemap.ClearAllTiles();
            fogTilemap.ClearAllTiles();

            PaintFloorAndFog();
            PaintWalls();
            PlaceActors();
            PlaceRegionTriggers();

            Debug.Log($"stage_generated seed={seed} rooms={Layout.Rooms.Count}");
        }

        private void BuildPaletteLookup()
        {
            _palettesByBiome = new Dictionary<string, StagePalette> { [homePalette.biomeId] = homePalette };
            foreach (StagePalette palette in foreignPalettes)
            {
                _palettesByBiome[palette.biomeId] = palette;
            }
        }

        private void PaintFloorAndFog()
        {
            for (int x = 0; x < Layout.Width; x++)
            {
                for (int y = 0; y < Layout.Height; y++)
                {
                    if (!Layout.IsFloor(x, y))
                    {
                        continue;
                    }

                    var cell = new Vector3Int(x, y, 0);
                    StagePalette palette = PaletteAt(x, y);
                    groundTilemap.SetTile(cell, palette.floorTile);

                    // FogOfWarReveal 은 "이미 타일이 있는 칸만" 걷어낸다. 여기서
                    // 안 채우면 에러 없이 안개가 통째로 사라진다(STAGE_A_DESIGN §9).
                    // 가장 짙은 단계로 깔아 시작 시점엔 전부 안 보이게 한다.
                    if (palette.fogLevels.Length > 0)
                    {
                        fogTilemap.SetTile(cell, palette.fogLevels[palette.fogLevels.Length - 1]);
                    }
                }
            }
        }

        private void PaintWalls()
        {
            for (int x = 0; x < Layout.Width; x++)
            {
                for (int y = 0; y < Layout.Height; y++)
                {
                    if (!Layout.IsWall(x, y))
                    {
                        continue;
                    }

                    wallsTilemap.SetTile(new Vector3Int(x, y, 0), PaletteAt(x, y).wallTile);
                }
            }
        }

        private void PlaceActors()
        {
            if (player != null)
            {
                player.position = groundTilemap.GetCellCenterWorld(
                    new Vector3Int(Layout.PlayerSpawn.x, Layout.PlayerSpawn.y, 0));
            }

            if (extractionPoint != null)
            {
                extractionPoint.transform.position = groundTilemap.GetCellCenterWorld(
                    new Vector3Int(Layout.ExtractionPoint.x, Layout.ExtractionPoint.y, 0));
            }
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

        private StagePalette PaletteAt(int x, int y)
        {
            string biomeId = Layout.BiomeIdAt(x, y);
            return _palettesByBiome.TryGetValue(biomeId, out StagePalette palette) ? palette : homePalette;
        }
    }
}

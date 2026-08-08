using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.Gameplay
{
    // 기능: 스테이지 맵 생성 (STAGE_A_DESIGN.md §4)
    // 구역 하나의 그림 묶음. StageMapGenerator 가 이름을 하드코딩하지 않고
    // 이 참조로만 타일을 찾게 한다 — 늪지만 파일 이름이 MarshTile_* 로 다른데,
    // 팔레트를 거치면 그 차이가 여기 한 곳에만 남는다.
    [CreateAssetMenu(fileName = "StagePalette", menuName = "SlimeRanch/Stage Palette")]
    public sealed class StagePalette : ScriptableObject
    {
        [Tooltip("BiomeCatalog 의 biomeId 와 일치해야 한다 — 낙인·종 추첨이 이 값을 본다.")]
        public string biomeId = "biome_default";

        [Tooltip("바닥 1종. wang 16장 블렌드는 나중 에셋 작업(§9 부채).")]
        public TileBase floorTile;

        [Tooltip("벽 1종, 두께 3칸. 8방향 모서리는 나중(§9 부채).")]
        public TileBase wallTile;

        [Tooltip("안개 5단계, 옅음→짙음. FogOfWarReveal.fogLevels 와 같은 순서.")]
        public TileBase[] fogLevels = new TileBase[5];
    }
}

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

        // 기존 GroundBlend wang 세트(Tools/apply_tile_sheets.py 산출물)를 그대로
        // 쓴다. index = NW*8+NE*4+SW*2+SE*1, 0=완전 open, 15=완전 upper 지형
        // (초원=나무숲, 늪지=진흙탕, 화산재=용암) — 바이옴별로 "막힘" 이 다르게
        // 생겨서 오히려 자연스럽다. 바닥은 늘 [0], 벽 밴드는 코너 블렌드로 그린다.
        [Tooltip("GroundBlend wang 16장. [0]=완전 open(바닥), [15]=완전 막힘(벽 안쪽). 바닥에만 쓴다.")]
        public TileBase[] wangBlend = new TileBase[16];

        // wangBlend 와 그림·색은 완전히 같지만 콜라이더 타입만 다른 16장. 원본
        // Tile_GroundBlend_* 는 바닥용이라 m_ColliderType=None 이다 — 벽에도
        // 그 애셋을 그대로 쓰면 그림은 막힌 듯 보여도 실제로는 통과된다(실측
        // 버그, 2026-08-08). 그렇다고 원본 애셋의 콜라이더 타입을 바꾸면 바닥
        // 쪽(같은 애셋 공유)까지 막혀버린다 — 그래서 벽 전용으로 따로 만든다.
        //
        // §9-9(2026-08-08) 이후로는 [15] 만 쓴다 — 코너 블렌드를 벽에 적용하면
        // 바깥 모서리 칸이 "절반 잔디" 로 보여 바닥과 헷갈렸다. 나머지 15장은
        // 죽은 데이터지만, 안개 색 틴트(§9-2)처럼 같은 스프라이트를 재사용해
        // 만든 애셋이라 지우는 비용이 만드는 비용보다 커서 그냥 둔다.
        [Tooltip("wangBlend 와 그림 동일, 콜라이더 있음(Sprite). [15] 만 실사용.")]
        public TileBase[] wangBlendCollidable = new TileBase[16];

        // 벽 밴드 전체에 통일해서 쓰는 그림자 타일. wangBlendCollidable[15] 를
        // 어둡게 틴트한 것 — "그 뒤가 안 보인다" 는 느낌을 주려는 것이다(사용자
        // 요청, 2026-08-08). 콜라이더는 원본과 같은 Grid.
        [Tooltip("벽 전체에 쓰는 그림자 타일. wangBlendCollidable[15] 를 어둡게 틴트.")]
        public TileBase wallShadow;

        // 그림 없이 충돌만 내는 타일. 벽 그림을 Ground 의 wang 이 다 그리게 된
        // 뒤로 벽 타일맵은 대부분 이걸 깐다 — 그림자를 그대로 쓰면 둔덕 위에
        // 어두운 사각형이 겹쳐 찍힌다. wang 이 못 덮은 칸(두께 1칸 기둥)에서만
        // wallShadow 로 돌아간다.
        [Tooltip("그림 없이 충돌만 내는 타일(Tile_Invisible). 벽 밴드 대부분이 이걸 쓴다.")]
        public TileBase invisibleWall;

        [Tooltip("안개 5단계, 옅음→짙음. FogOfWarReveal.fogLevels 와 같은 순서.")]
        public TileBase[] fogLevels = new TileBase[5];

        // 바닥 위에 흩뿌리는 장식(꽃·바위·갈대·통나무 등). 전부 콜라이더 None 이라
        // 순수 시각 요소다 — 길을 막지 않으므로 연결성 검사에 영향이 없다.
        // 균일한 바닥은 넓을수록 지루해진다. 이게 "여기가 어디쯤인지" 를 알려주는
        // 유일한 시각 단서다(안개 때문에 멀리 못 보므로 더 중요하다).
        [Tooltip("바닥에 흩뿌릴 장식. 콜라이더 없는 순수 시각 요소.")]
        public TileBase[] decorTiles = new TileBase[0];

        // 벽 덩어리 위에 얹는 나무 프롭. 벽 칸을 캐노피 텍스처로 칠하기만 하면
        // 평면 카펫처럼 보인다 — 레퍼런스(2026-08-08 사용자 제공)처럼 3/4 부감
        // 나무를 개별로 세워야 숲으로 읽힌다.
        //
        // 그림 규약: 피벗 = 줄기 바닥 중앙, 캐노피는 그 위·좌우로 뻗는다.
        // 프롭은 Props 타일맵(Individual 모드 + Y 정렬)에 놓이므로 아래쪽 나무가
        // 플레이어 앞에 그려진다 — 나무 뒤로 지나가는 깊이감이 여기서 나온다.
        // 충돌은 프롭이 아니라 벽 칸이 계속 담당한다(그림과 정확히 일치).
        [Tooltip("벽 위에 세울 나무 프롭. 피벗은 줄기 바닥 중앙.")]
        public TileBase[] treeProps = new TileBase[0];
    }
}

using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>발밑 타원 그림자를 붙인다. 슬라임이 바닥에 안 붙어 떠 보이던 것을 잡는다.</summary>
    /// <remarks>
    /// 플레이어 프리팹에는 <c>Shadow</c> 자식이 손으로 들어 있지만 슬라임 세
    /// 프리팹(야생·동행·배치)에는 없었다. 프리팹 셋을 따로 고치면 종이 늘 때마다
    /// 같은 실수를 반복하므로, 그림을 입히는 자리(<see cref="SlimeAppearance"/>)가
    /// 한 번에 붙인다.
    ///
    /// 스프라이트는 자산이 아니라 코드로 굽는다 — 타원 하나 때문에 PNG 를 넣고
    /// 세 프리팹에 배선할 이유가 없다. 텍스처는 정적으로 한 장만 만들어 모든
    /// 개체가 나눠 쓴다.
    /// </remarks>
    public static class ActorShadow
    {
        private const string ChildName = "Shadow";
        private static Sprite _ellipse;

        /// <summary>이미 있으면 아무것도 안 한다 — 스폰마다 그림자가 겹쳐 쌓이면 안 된다.</summary>
        public static void Attach(Transform host, SpriteRenderer body)
        {
            if (host == null || host.Find(ChildName) != null)
            {
                return;
            }

            Bounds bounds = FootprintOf(host, body);

            // 그림자 높이는 몸 높이가 아니라 **폭**에서 낸다 — 키가 큰 배우에
            // 몸 높이를 곱하면 발밑에 세로로 긴 얼룩이 생긴다. 폭도 실루엣보다
            // 좁게 잡는다: 발이 닿는 자리는 어깨너비가 아니라 두 발 사이다
            // (사용자, 2026-08-09: 그림자가 너무 크고 아래에 있다).
            float width = bounds.size.x * 0.6f;
            float height = width * 0.32f;

            var go = new GameObject(ChildName, typeof(SpriteRenderer));
            go.transform.SetParent(host, false);

            // 타원의 세로 한가운데를 실루엣 바닥에 맞춘다 — 바닥에 위 끝을 맞추면
            // 그림자 전체가 발 아래로 내려가 붕 뜬 것처럼 보인다.
            go.transform.localPosition = new Vector3(bounds.center.x, bounds.min.y + height * 0.5f, 0f);
            go.transform.localScale = new Vector3(width, height, 1f);

            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = Ellipse();
            renderer.color = new Color(0f, 0f, 0f, 0.4f);

            // 몸보다 한 단계 뒤. 같은 order 면 Y 정렬이 그림자를 몸 위로 올린다.
            renderer.sortingLayerID = body != null ? body.sortingLayerID : renderer.sortingLayerID;
            renderer.sortingOrder = (body != null ? body.sortingOrder : 0) - 1;
        }

        /// <summary>실제로 그려진 부분(실루엣)의 크기와 바닥.</summary>
        /// <remarks>
        /// <b>스프라이트가 Tight 메시로 임포트돼 있어야 한다.</b> Full Rect 면
        /// <c>vertices</c> 가 캔버스 네 귀퉁이 4개뿐이라 아래 계산이 캔버스 크기로
        /// 떨어진다(플레이어 그림자가 발보다 한참 아래에 크게 깔리던 원인,
        /// 2026-08-09). <c>Assets/Art/SlimeStrips/player__*.png</c> 33장을 Tight 로
        /// 바꿔 뒀다 — 실루엣이 x±0.56 / y[-1.04, 0.96] 로 잡힌다.
        ///
        /// <see cref="Sprite.bounds"/> 는 투명 여백까지 포함한 캔버스 전체다 —
        /// 플레이어는 104px 캔버스에 인물이 절반뿐(PPU 27, 실루엣 50px)이라 그
        /// 값으로 놓으면 그림자가 발보다 1유닛 아래에 깔린다. 반대로 콜라이더
        /// (반지름 0.5 원)는 몸 한가운데라 가슴께에 뜬다. 둘 다 안 맞는다.
        ///
        /// <see cref="Sprite.vertices"/> 는 알파 외곽을 따라 잘린 tight 메시라
        /// 여백이 빠진 진짜 실루엣이다. 텍스처를 읽지 않으므로
        /// <c>Read/Write Enabled</c> 없이도 된다.
        /// </remarks>
        private static Bounds FootprintOf(Transform host, SpriteRenderer body)
        {
            Sprite sprite = body != null ? body.sprite : null;
            if (sprite == null)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }

            Vector2[] vertices = sprite.vertices;
            if (vertices == null || vertices.Length == 0)
            {
                return sprite.bounds;
            }

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (Vector2 vertex in vertices)
            {
                if (vertex.x < minX) minX = vertex.x;
                if (vertex.x > maxX) maxX = vertex.x;
                if (vertex.y < minY) minY = vertex.y;
                if (vertex.y > maxY) maxY = vertex.y;
            }

            return new Bounds(
                new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f),
                new Vector3(maxX - minX, maxY - minY, 1f));
        }

        // 64×64 짜리 흰 타원. 가장자리를 부드럽게 깎아 픽셀 계단이 안 보이게 한다.
        private static Sprite Ellipse()
        {
            if (_ellipse != null)
            {
                return _ellipse;
            }

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false) { filterMode = FilterMode.Bilinear };
            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half;
                    float dy = (y + 0.5f - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01((1f - d) * 3f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();

            // PPU = size 라 스프라이트가 정확히 1×1 유닛이다 — 위 localScale 이
            // 그대로 유닛 크기가 된다.
            _ellipse = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            return _ellipse;
        }
    }
}

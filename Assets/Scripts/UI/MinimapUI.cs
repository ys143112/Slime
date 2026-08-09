using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    /// <summary>왼쪽 위 미니맵 — 지도 모양·내 자리·추출구를 한 화면에 보여준다.</summary>
    /// <remarks>
    /// 추출구 방향 화살표(<see cref="ExtractionDirectionArrow"/>)만으로는 벽을 낀
    /// 길을 못 읽어 탈출이 어렵다(사용자, 2026-08-09). 화살표는 그대로 두고 —
    /// 미니맵은 "어디로" 를, 화살표는 화면 밖 목표의 "어느 쪽" 을 맡는다.
    ///
    /// 지도는 <see cref="StageLayout"/> 에서 텍스처 한 장으로 굽는다(지도 하나당
    /// 한 번). 매 프레임 바뀌는 건 점 두 개뿐이라 텍스처를 다시 굽지 않는다 —
    /// 120×90 짜리 <c>SetPixels</c> 를 60fps 로 돌릴 이유가 없다.
    ///
    /// 안개(<see cref="FogOfWarReveal"/>)는 반영하지 않는다. 걸어본 곳만 그리면
    /// 미니맵이 탈출에 도움이 안 된다 — 지금 문제가 바로 "길이 안 보인다" 다.
    /// </remarks>
    public sealed class MinimapUI : MonoBehaviour
    {
        private const float PanelSize = 240f;
        private const float MarkerSize = 10f;

        private static readonly Color FloorColor = new Color(0.62f, 0.72f, 0.5f, 1f);
        private static readonly Color WallColor = new Color(0.13f, 0.14f, 0.17f, 1f);

        // 아직 안 밝힌 칸. 벽보다 더 어둡게 둬서 "여긴 아직 모른다" 가 읽히게 한다.
        private static readonly Color UnknownColor = new Color(0.06f, 0.06f, 0.08f, 1f);

        private static MinimapUI _instance;

        private StageMapGenerator _generator;
        private RectTransform _canvasArea;
        private RectTransform _playerMarker;
        private RectTransform _extractionMarker;
        private Transform _player;
        private Transform _extraction;
        private Texture2D _texture;
        private FogOfWarReveal _fog;

        /// <summary>지도를 다 그린 뒤 <see cref="StageMapGenerator"/> 가 부른다.</summary>
        public static void Show(StageMapGenerator generator)
        {
            if (generator == null || generator.Layout == null)
            {
                return;
            }

            if (_instance == null)
            {
                var go = new GameObject("Minimap", typeof(RectTransform), typeof(MinimapUI));
                go.transform.SetParent(HudRoot.Get(), false);
                HudRoot.Stretch(go.GetComponent<RectTransform>());
                _instance = go.GetComponent<MinimapUI>();
                _instance.Build();
            }

            _instance.Bind(generator);
        }

        private void Build()
        {
            GameObject frame = HudRoot.Frame("Frame", transform);
            var frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = new Vector2(0f, 1f);
            frameRect.anchorMax = new Vector2(0f, 1f);
            frameRect.pivot = new Vector2(0f, 1f);

            // 화면 왼쪽 위 맨 위. 배낭 명패가 이 아래로 내려간다(사용자, 2026-08-09)
            // — 지도가 자주 보는 쪽이라 구석에 붙인다.
            frameRect.anchoredPosition = new Vector2(24f, -24f);
            frameRect.sizeDelta = new Vector2(PanelSize, PanelSize);

            var areaGo = new GameObject("Map", typeof(RectTransform), typeof(Image));
            areaGo.transform.SetParent(frame.transform, false);
            _canvasArea = areaGo.GetComponent<RectTransform>();
            _canvasArea.anchorMin = Vector2.zero;
            _canvasArea.anchorMax = Vector2.one;

            // 창틀 테두리 안쪽으로 들인다 — 지도가 테두리를 밟으면 모서리가 지저분해진다.
            _canvasArea.offsetMin = new Vector2(18f, 16f);
            _canvasArea.offsetMax = new Vector2(-18f, -22f);

            var image = areaGo.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;

            _extractionMarker = BuildMarker("Extraction", new Color(1f, 0.85f, 0.25f, 1f));
            _playerMarker = BuildMarker("Player", Color.white);
        }

        private RectTransform BuildMarker(string name, Color color)
        {
            GameObject marker = HudRoot.Panel(name, _canvasArea, color);
            var rect = marker.GetComponent<RectTransform>();

            // 왼쪽 아래 기준으로 자리를 잡는다 — 셀 좌표를 그대로 비율로 쓸 수 있다.
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(MarkerSize, MarkerSize);
            return rect;
        }

        private void Bind(StageMapGenerator generator)
        {
            Unsubscribe();

            _generator = generator;
            _player = null;
            _extraction = null;

            var image = _canvasArea.GetComponent<Image>();
            Sprite previous = image.sprite;
            image.sprite = BakeSprite(generator.Layout);
            _texture = image.sprite.texture;
            FitFrameToMap(generator.Layout);

            // 지도마다 텍스처를 새로 굽는다 — 옛것을 안 버리면 다이브할 때마다
            // 120×90 텍스처가 메모리에 쌓인다.
            if (previous != null)
            {
                Destroy(previous.texture);
                Destroy(previous);
            }
        }

        /// <summary>창을 지도 비율에 맞춘다 — 남는 여백 없이 딱 차게.</summary>
        /// <remarks>
        /// 정사각 창에 4:3 지도를 <c>preserveAspect</c> 로 넣으면 위아래에 검은 띠가
        /// 남아 "작은 카메라" 처럼 보였다(사용자, 2026-08-09). 폭은 고정하고 높이만
        /// 지도 비율로 줄인다. 지도 크기는 <see cref="StageLayoutSettings"/> 가
        /// 정하므로(기본 120×90) 바뀌면 창도 따라 바뀐다.
        /// </remarks>
        private void FitFrameToMap(StageLayout layout)
        {
            var frame = (RectTransform)_canvasArea.parent;

            float insetX = _canvasArea.offsetMin.x - _canvasArea.offsetMax.x;
            float insetY = _canvasArea.offsetMin.y - _canvasArea.offsetMax.y;

            float innerWidth = PanelSize - insetX;
            float innerHeight = innerWidth * layout.Height / Mathf.Max(1, layout.Width);

            frame.sizeDelta = new Vector2(PanelSize, innerHeight + insetY);
        }

        /// <summary>처음엔 통째로 "모르는 곳" 이다 — 걸어본 만큼만 칠해진다.</summary>
        private static Sprite BakeSprite(StageLayout layout)
        {
            var texture = new Texture2D(layout.Width, layout.Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color32[layout.Width * layout.Height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = UnknownColor;
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, layout.Width, layout.Height),
                new Vector2(0.5f, 0.5f), 1f);
        }

        /// <summary>안개가 걷힌 자리만 칠한다.</summary>
        /// <remarks>
        /// 지도 전체가 아니라 방금 밝힌 중심 둘레만 훑는다 — 반경 6이면 169칸이라
        /// 플레이어가 칸을 옮길 때마다 돌려도 싸다. 안개는 한 번 걷히면 다시
        /// 덮이지 않으므로 지우는 경우는 없다.
        /// </remarks>
        private void Repaint(Vector3Int center)
        {
            FogOfWarReveal fog = FogOfWarReveal.Instance;
            if (fog == null || _texture == null || _generator == null || _generator.Layout == null)
            {
                return;
            }

            StageLayout layout = _generator.Layout;
            int radius = fog.OuterRadius;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int x = center.x + dx;
                    int y = center.y + dy;
                    if (x < 0 || y < 0 || x >= layout.Width || y >= layout.Height)
                    {
                        continue;
                    }

                    if (!fog.IsExplored(new Vector3Int(x, y, center.z)))
                    {
                        continue;
                    }

                    _texture.SetPixel(x, y, layout.IsFloor(x, y) ? FloorColor : WallColor);
                }
            }

            _texture.Apply();
        }

        private void Unsubscribe()
        {
            if (_fog != null)
            {
                _fog.Revealed -= Repaint;
                _fog = null;
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void LateUpdate()
        {
            // 생성기는 바이옴 씬과 함께 사라진다. 미니맵은 씬을 넘어가는 HudRoot
            // 밑이라 같이 안 사라지므로, 목장으로 돌아오면 스스로 치운다.
            if (_generator == null)
            {
                _instance = null;
                Destroy(gameObject);
                return;
            }

            // 안개는 씬이 열린 뒤에 붙는다 — 첫 프레임에 못 찾을 수 있어 매번 확인한다.
            if (_fog == null && FogOfWarReveal.Instance != null)
            {
                _fog = FogOfWarReveal.Instance;
                _fog.Revealed += Repaint;
            }

            if (_player == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                _player = player != null ? player.transform : null;
            }

            if (_extraction == null)
            {
                ExtractionPoint point = Object.FindAnyObjectByType<ExtractionPoint>();
                _extraction = point != null ? point.transform : null;
            }

            Place(_playerMarker, _player);

            // 추출구는 그 자리를 밝히기 전에도 보여준다 — 어디로 나가야 하는지가
            // 미니맵을 넣은 이유다(사용자, 2026-08-09).
            Place(_extractionMarker, _extraction);
        }

        // 월드 좌표 → 셀 → 지도 안 비율. 타일맵이 셀 변환을 알고 있으므로
        // 셀 크기·원점을 여기서 다시 계산하지 않는다.
        private void Place(RectTransform marker, Transform target)
        {
            if (target == null)
            {
                marker.gameObject.SetActive(false);
                return;
            }

            StageLayout layout = _generator.Layout;
            Vector3Int cell = _generator.GroundTilemap.WorldToCell(target.position);

            float u = Mathf.Clamp01((cell.x + 0.5f) / layout.Width);
            float v = Mathf.Clamp01((cell.y + 0.5f) / layout.Height);

            // 그림은 preserveAspect 로 가운데 정렬돼 있어, 지도가 실제로 차지하는
            // 영역을 다시 재야 점이 안 어긋난다.
            Vector2 area = _canvasArea.rect.size;
            float scale = Mathf.Min(area.x / layout.Width, area.y / layout.Height);
            Vector2 drawn = new Vector2(layout.Width * scale, layout.Height * scale);
            Vector2 origin = (area - drawn) * 0.5f;

            marker.gameObject.SetActive(true);
            marker.anchoredPosition = origin + new Vector2(u * drawn.x, v * drawn.y);
        }
    }
}

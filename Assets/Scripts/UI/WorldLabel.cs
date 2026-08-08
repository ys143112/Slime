using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// 월드에 떠 있는 글자 하나. 시설이 자기 머리 위에 이름과 조작키를 띄운다.
    /// </summary>
    /// <remarks>
    /// <b>Canvas 를 안 쓴다.</b> 프로젝트 관례가 월드 UI 는 Canvas 없이
    /// 프리팹·스프라이트로 처리하는 것이고(씬마다 UI 배선이 늘어나는 걸 피하려는
    /// 선택), <c>TextMesh</c> 는 그 관례에 그대로 맞는다 — 컴포넌트 하나면 끝이고
    /// 씬을 건드릴 필요도 없다.
    ///
    /// 목장 작업장이 바닥 타일 조각을 그림으로 쓰고 있어 배경에 묻혀 보이지
    /// 않았다(2026-08-08). 그림을 새로 만들기 전이라도 여기 뭐가 있는지는
    /// 글자로 알 수 있어야 한다.
    /// </remarks>
    public static class WorldLabel
    {
        // 슬라임(0)·시설 스프라이트보다 위. 체력바(200번대)보다는 아래다.
        private const int SortingOrder = 150;

        private const string PixelFontResource = "Fonts/Kenney Pixel";

        public static TextMesh Attach(Transform parent, string text, float height)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, height, 0f);

            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.LowerCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;
            // UI 전체가 Kenney Pixel 인데 여기만 내장 폰트라 월드 이름표만
            // 매끈한 글씨로 떠 있었다(사용자 신고, 2026-08-08). 없으면 내장
            // 폰트로 떨어진다 — 글자가 안 나오는 것보다는 낫다.
            Font pixel = Resources.Load<Font>(PixelFontResource);
            mesh.font = pixel != null ? pixel : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 글자를 크게 뽑아 놓고 스케일로 줄인다. fontSize 를 작게 잡으면
            // 확대됐을 때 글자가 뭉갠다.
            mesh.fontSize = 64;

            // 0.06 은 명패까지 붙자 웅덩이(2유닛)보다 넓어져 오브젝트와 플레이어를
            // 가렸다(사용자 신고, 2026-08-08). 0.035 면 두 줄짜리도 웅덩이 폭 안에
            // 들어온다.
            mesh.characterSize = 0.035f;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = mesh.font.material;
            renderer.sortingOrder = SortingOrder;

            AttachPlate(go.transform, renderer);
            return mesh;
        }

        // 글자만 허공에 떠 있으면 밝은 잔디 위에서 흰 글씨가 묻힌다. 인벤토리·
        // 교배창이 쓰는 것과 같은 나무 창틀을 뒤에 깔아 같은 톤으로 맞춘다.
        //
        // 9-slice 라 크기를 아무렇게나 잡아도 모서리가 안 늘어난다. 글자 길이가
        // 상태에 따라 바뀌므로(웅덩이는 비었을 때와 채웠을 때가 다르다) 크기는
        // 매 프레임 글자 경계에서 다시 잰다.
        private static void AttachPlate(Transform parent, Renderer text)
        {
            Sprite frame = Resources.Load<Sprite>(
                "UI/window_inventroy_UI_Farm_game-style_UI_a_style_fea/elements/Window");
            if (frame == null)
            {
                return;
            }

            var go = new GameObject("Plate");
            go.transform.SetParent(parent, false);

            var plate = go.AddComponent<SpriteRenderer>();
            plate.sprite = frame;
            plate.drawMode = SpriteDrawMode.Sliced;
            plate.sortingOrder = SortingOrder - 1;

            go.AddComponent<WorldLabelPlate>().Bind(plate, text);
        }
    }

    /// <summary>이름표 뒤 명패. 글자 경계를 따라 크기를 맞춘다.</summary>
    public sealed class WorldLabelPlate : MonoBehaviour
    {
        // 테두리 **바깥으로** 더 두는 여백(월드 유닛). 테두리 두께 자체는
        // 아래에서 스프라이트에서 읽어 더한다.
        private static readonly Vector2 Margin = new Vector2(0.14f, 0.10f);

        private SpriteRenderer _plate;
        private Renderer _text;
        private Vector2 _padding;

        public void Bind(SpriteRenderer plate, Renderer text)
        {
            _plate = plate;
            _text = text;

            // 9-slice 는 size 안쪽을 테두리가 먹는다. 여백을 테두리보다 작게
            // 잡으면 내부 폭이 글자보다 좁아져 글자가 액자 밖으로 삐져나온다
            // (사용자 신고, 2026-08-08). 테두리 두께를 스프라이트에서 읽어
            // 더하므로 그림을 바꿔도 다시 안 맞춰도 된다.
            Sprite sprite = plate.sprite;
            Vector4 border = sprite.border / sprite.pixelsPerUnit;
            _padding = new Vector2(
                Mathf.Max(border.x, border.z) + Margin.x,
                Mathf.Max(border.y, border.w) + Margin.y);
        }

        // LateUpdate 인 이유: TextMesh 는 text 를 바꾼 그 프레임의 렌더 직전에
        // 메시를 다시 만든다. Update 에서 재면 한 프레임 전 크기가 나와 글자가
        // 바뀔 때마다 명패가 한 박자 늦게 따라온다.
        private void LateUpdate()
        {
            if (_plate == null || _text == null)
            {
                return;
            }

            // 글자가 없으면 명패도 숨긴다 — 빈 나무판만 떠 있으면 뭔가 고장 난
            // 것처럼 보인다.
            Bounds bounds = _text.bounds;
            bool visible = bounds.size.x > 0.001f;
            if (_plate.enabled != visible)
            {
                _plate.enabled = visible;
            }

            if (!visible)
            {
                return;
            }

            _plate.size = (Vector2)bounds.size + _padding * 2f;

            // 글자는 LowerCenter 기준이라 부모 원점보다 위에 그려진다. 명패를
            // 그 중심으로 옮겨야 글자가 가운데 온다.
            Vector3 center = transform.parent.InverseTransformPoint(bounds.center);
            transform.localPosition = new Vector3(center.x, center.y, 0.01f);
        }
    }
}

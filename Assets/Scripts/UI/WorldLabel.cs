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
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 글자를 크게 뽑아 놓고 스케일로 줄인다. fontSize 를 작게 잡으면
            // 확대됐을 때 글자가 뭉갠다.
            mesh.fontSize = 64;
            mesh.characterSize = 0.06f;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = mesh.font.material;
            renderer.sortingOrder = SortingOrder;
            return mesh;
        }
    }
}

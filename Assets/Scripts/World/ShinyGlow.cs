using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>이로치 개체 뒤에 깔리는 흰 테두리. 색이 아니라 윤곽으로 구분시킨다.</summary>
    /// <remarks>
    /// 평상시 개체가 바이옴 색을 띠게 되면서 "색이 다르다" 만으로는 이로치를
    /// 못 알아보게 됐다 — 늪지의 초록 슬라임이 평범한 개체인지 이로치인지
    /// 구분이 안 된다(사용자 결정, 2026-08-08).
    ///
    /// <b>새 그림을 안 만든다.</b> 같은 스프라이트를 조금 키워 뒤에 흰색으로
    /// 깔면 실루엣을 따라 테두리가 생긴다. 애니메이터가 매 프레임 원본 스프라이트를
    /// 갈아 끼우므로 <see cref="LateUpdate"/> 에서 따라 베낀다 — 그 자리에서
    /// 한 번만 복사하면 걷는 순간 테두리만 정지 그림으로 남는다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ShinyGlow : MonoBehaviour
    {
        private const float Scale = 1.18f;
        private const string ChildName = "ShinyGlow";

        private SpriteRenderer _source;
        private SpriteRenderer _glow;

        /// <summary>이로치면 테두리를 붙이고, 아니면 떼어 낸다.</summary>
        public static void Apply(SpriteRenderer source, bool shiny)
        {
            if (source == null)
            {
                return;
            }

            var existing = source.GetComponent<ShinyGlow>();
            if (!shiny)
            {
                if (existing != null)
                {
                    existing.Clear();
                    Destroy(existing);
                }

                return;
            }

            if (existing == null)
            {
                existing = source.gameObject.AddComponent<ShinyGlow>();
            }

            existing._source = source;
            existing.EnsureChild();
        }

        private void EnsureChild()
        {
            if (_glow != null)
            {
                return;
            }

            Transform found = transform.Find(ChildName);
            if (found != null)
            {
                _glow = found.GetComponent<SpriteRenderer>();
                return;
            }

            var go = new GameObject(ChildName);
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * Scale;

            _glow = go.AddComponent<SpriteRenderer>();
            _glow.color = Color.white;

            // 본체와 같은 정렬 레이어에서 한 칸 뒤. 앞에 두면 본체를 덮는다.
            _glow.sortingLayerID = _source.sortingLayerID;
            _glow.sortingOrder = _source.sortingOrder - 1;
        }

        private void LateUpdate()
        {
            if (_source == null || _glow == null)
            {
                return;
            }

            _glow.sprite = _source.sprite;
            _glow.flipX = _source.flipX;
            _glow.flipY = _source.flipY;
        }

        private void Clear()
        {
            Transform found = transform.Find(ChildName);
            if (found != null)
            {
                Destroy(found.gameObject);
            }

            _glow = null;
        }
    }
}

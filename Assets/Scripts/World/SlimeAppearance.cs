using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-005 확장 (이로치 외형)
    // 개체 하나의 그림과 색을 그 개체에만 입힌다.
    //
    // 색을 SpriteRenderer.color 로 거는 것이 핵심이다 — 이 값은 렌더러마다
    // 따로 들고 있어 같은 프리팹에서 나온 다른 슬라임에 번지지 않는다.
    // sharedMaterial 의 색을 건드리면 그 머티리얼을 쓰는 개체가 전부 같이
    // 변하고, 그 변경은 에디터에서 자산 파일에까지 남는다.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SlimeAppearance : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;

        private void Reset()
        {
            target = GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            if (target == null)
            {
                target = GetComponent<SpriteRenderer>();
            }
        }

        /// <summary>개체의 종·이로치 여부에 맞춰 그림과 색을 입힌다.</summary>
        public void Apply(SlimeInstance instance)
        {
            if (instance == null || target == null)
            {
                return;
            }

            SlimeSpecies species = SlimeSpeciesCatalog.Lookup(instance.speciesId);
            if (species != null)
            {
                Sprite sprite = species.ResolveSprite(instance.shinyFlag);
                if (sprite != null)
                {
                    target.sprite = sprite;
                }
            }

            // 이로치 전용 그림이 따로 있으면 색까지 덧입힐 이유가 없다 — 그림이
            // 이미 그 색이므로 곱하면 두 번 어두워진다.
            bool hasDedicatedShinySprite = species != null && instance.shinyFlag && species.shinySprite != null;
            target.color = instance.shinyFlag && !hasDedicatedShinySprite ? instance.shinyTint : Color.white;
        }
    }
}

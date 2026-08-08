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
        [SerializeField] private Animator animator;

        private void Reset()
        {
            target = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
        }

        private void Awake()
        {
            if (target == null)
            {
                target = GetComponent<SpriteRenderer>();
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        /// <summary>개체의 종·이로치 여부에 맞춰 그림과 색을 입힌다.</summary>
        public void Apply(SlimeInstance instance)
        {
            if (instance == null || target == null)
            {
                return;
            }

            // Awake 에만 기대면 프리팹 인스펙터에 배선이 없는 채로 에디터에서
            // 부를 때(스폰 미리보기 등) 조용히 애니메이터를 못 찾는다.
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            SlimeSpecies species = SlimeSpeciesCatalog.Lookup(instance.speciesId);
            if (species != null)
            {
                // 종 전용 애니메이터가 있으면 그림은 그쪽이 굴린다.
                // 같은 컨트롤러를 다시 넣으면 재생 상태가 처음으로 튀므로 비교한다.
                if (species.animatorController != null && animator != null
                    && animator.runtimeAnimatorController != species.animatorController)
                {
                    animator.runtimeAnimatorController = species.animatorController;
                }

                // 애니메이터가 있어도 정지 그림을 같이 깔아 둔다. Motion 이 빈 상태에
                // 들어가면 애니메이터가 m_Sprite 를 안 굴리고 **프리팹에 저장된 값으로
                // 되돌리기** 때문이다 — 그 값이 예전 PNG 면 그게 그대로 튀어나온다.
                Sprite sprite = species.ResolveSprite(instance.shinyFlag);
                if (sprite != null)
                {
                    target.sprite = sprite;
                }
            }

            // 이로치 전용 그림이 따로 있으면 색까지 덧입힐 이유가 없다 — 그림이
            // 이미 그 색이므로 곱하면 두 번 어두워진다.
            bool hasDedicatedShinySprite = species != null && instance.shinyFlag && species.shinySprite != null;
            if (instance.shinyFlag)
            {
                target.color = hasDedicatedShinySprite ? Color.white : instance.shinyTint;
            }
            else
            {
                // 평상시 개체는 있던 바이옴의 색을 띤다 — 늪지의 파란 슬라임이
                // 초원과 똑같이 파랗면 어디서 잡은 건지 그림으로 안 읽힌다
                // (팀 QA, 2026-08-08). 잡은 뒤에는 capturedBiomeId 를 쓰므로
                // 목장에 데려와도 그 색을 유지한다.
                target.color = species != null ? species.ResolveBiomeTint(HomeBiomeOf(instance)) : Color.white;
            }

            ShinyGlow.Apply(target, instance.shinyFlag);
        }

        // 잡히기 전에는 지금 서 있는 바이옴이 곧 고향이다.
        private static string HomeBiomeOf(SlimeInstance instance)
        {
            if (!string.IsNullOrEmpty(instance.capturedBiomeId))
            {
                return instance.capturedBiomeId;
            }

            return GameManager.Instance != null ? GameManager.Instance.CurrentBiomeId : string.Empty;
        }
    }
}

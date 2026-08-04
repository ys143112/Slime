using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-007 (남은 체력 표시). 플레이어와 야생 슬라임이 같이 쓴다.
    // 월드 공간 막대라 Canvas 가 필요 없다 — 프리팹에 한 번 붙이면 네 씬에서
    // 모두 뜬다. Hub 씬에는 Canvas 자체가 없어 UI 경로는 씬마다 배선이 늘었다.
    public sealed class HealthBar : MonoBehaviour
    {
        [SerializeField] private Transform fill;

        // 가득 찬 막대는 정보가 0 이다. 야생 슬라임은 숨기고 플레이어는 항상
        // 보이게 둔다 — 자기 체력은 깎이기 전에도 알아야 한다.
        [SerializeField] private bool hideWhenFull = true;

        private void Awake()
        {
            if (fill == null)
            {
                Debug.LogWarning("HealthBar: fill 이 연결되지 않아 체력 표시를 비활성화합니다.");
                enabled = false;
            }
        }

        public void SetRatio(float ratio)
        {
            if (!enabled || fill == null)
            {
                return;
            }

            ratio = Mathf.Clamp01(ratio);

            // 스프라이트 피벗이 왼쪽이라 x 스케일만 줄이면 오른쪽에서 깎인다.
            Vector3 scale = fill.localScale;
            scale.x = ratio;
            fill.localScale = scale;

            bool visible = ratio > 0f && !(hideWhenFull && ratio >= 1f);
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }
    }
}

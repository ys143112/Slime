using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-004 (오염 티어 시각화). 카메라 자식으로 붙여 화면을 늘 덮게 하고,
    // 티어(0~5)에 비례해 알파를 올린다 — 방 크기에 맞춰 타일맵을 칠할 필요가 없다.
    // 매 프레임 갱신하므로 이벤트 구독 없이도 티어 변화가 바로 반영된다.
    public sealed class CorruptionOverlay : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private float maxAlpha = 0.55f;

        // 두근거림: 티어가 높을수록 더 빠르고 크게 맥동해 위험도를 체감시킨다.
        [SerializeField] private float pulseAmplitude = 0.12f;
        [SerializeField] private float pulseSpeedPerTier = 0.8f;

        // 채도: 티어가 낮으면 뿌옇게, 높으면 원색에 가깝게 — 알파만으론 두 티어가
        // 비슷한 흐림으로 보여 구분이 안 된다는 피드백으로 추가.
        [SerializeField] private float minSaturation = 0.15f;
        [SerializeField] private float maxSaturation = 1f;

        private float _hue;
        private float _value;

        private void Awake()
        {
            if (target == null)
            {
                target = GetComponent<SpriteRenderer>();
            }

            if (target != null)
            {
                Color.RGBToHSV(target.color, out _hue, out _, out _value);
            }
        }

        private void Update()
        {
            if (target == null || BiomeStigmaManager.Instance == null || GameManager.Instance == null)
            {
                return;
            }

            int tier = BiomeStigmaManager.Instance.GetCorruptionTier(GameManager.Instance.CurrentBiomeId);
            if (tier <= 0)
            {
                if (target.color.a != 0f)
                {
                    SetColor(0f, minSaturation);
                }

                return;
            }

            float tierRatio = tier / 5f;
            float baseAlpha = maxAlpha * tierRatio;
            float pulse = Mathf.Sin(Time.time * pulseSpeedPerTier * tier) * pulseAmplitude;
            float saturation = Mathf.Lerp(minSaturation, maxSaturation, tierRatio);

            SetColor(Mathf.Clamp01(baseAlpha + pulse), saturation);
        }

        private void SetColor(float alpha, float saturation)
        {
            Color rgb = Color.HSVToRGB(_hue, saturation, _value);
            rgb.a = alpha;
            target.color = rgb;
        }
    }
}

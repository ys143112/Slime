using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// 패시브가 닿는 범위를 바닥에 원으로 그린다. 회복이든 도트든 반경이 안
    /// 보이면 효과가 도는지, 얼마나 붙어야 하는지 알 방법이 없다.
    /// </summary>
    /// <remarks>
    /// <b>스프라이트를 안 만든다.</b> <see cref="LineRenderer"/> 로 점 32개를
    /// 둘러 그리면 자산 없이 어떤 반경에도 맞는 원이 나온다. 임시 그림을
    /// 만들어 넣으면 반경을 바꿀 때마다 스케일을 다시 맞춰야 한다.
    /// </remarks>
    public sealed class PassiveRangeRing : MonoBehaviour
    {
        private const int Segments = 32;

        private LineRenderer _line;
        private Color _color;
        private float _pulseUntil;

        public static PassiveRangeRing Attach(GameObject host, float radius, Color color)
        {
            var existing = host.GetComponent<PassiveRangeRing>();
            PassiveRangeRing ring = existing != null ? existing : host.AddComponent<PassiveRangeRing>();
            ring.Build(radius, color);
            return ring;
        }

        private void Build(float radius, Color color)
        {
            _color = color;

            if (_line == null)
            {
                _line = gameObject.AddComponent<LineRenderer>();
            }

            _line.useWorldSpace = false;
            _line.loop = true;
            _line.positionCount = Segments;
            _line.widthMultiplier = 0.06f;

            // 스프라이트용 기본 셰이더면 색이 그대로 나온다. 새 머티리얼을
            // 개체마다 하나씩 만드는 이유는 sharedMaterial 을 건드리면 같은 종
            // 전부의 색이 함께 변하기 때문이다.
            _line.material = new Material(Shader.Find("Sprites/Default"));

            // 슬라임 그림(0)보다 아래. 바닥에 깔린 것처럼 보여야 한다.
            _line.sortingOrder = -1;

            for (int i = 0; i < Segments; i++)
            {
                float angle = i / (float)Segments * Mathf.PI * 2f;
                _line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }

            ApplyColor(0.25f);
        }

        /// <summary>패시브가 한 번 돌 때마다 원을 잠깐 밝힌다.</summary>
        public void Pulse()
        {
            _pulseUntil = Time.time + 0.25f;
            ApplyColor(0.9f);
        }

        private void Update()
        {
            if (_pulseUntil > 0f && Time.time > _pulseUntil)
            {
                _pulseUntil = 0f;
                ApplyColor(0.25f);
            }
        }

        private void ApplyColor(float alpha)
        {
            if (_line == null)
            {
                return;
            }

            var color = new Color(_color.r, _color.g, _color.b, alpha);
            _line.startColor = color;
            _line.endColor = color;
        }
    }
}

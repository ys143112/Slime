using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// 머리 위로 떠오르며 사라지는 숫자. 피해와 회복이 실제로 들어갔는지
    /// 화면에서 확인할 방법이 로그밖에 없었다.
    /// </summary>
    /// <remarks>
    /// Canvas 를 안 쓰고 <c>TextMesh</c> 를 쓴다 — 월드 UI 관례이기도 하고,
    /// 월드 좌표에 그대로 띄우면 카메라를 따라 계산할 필요가 없다.
    /// </remarks>
    public sealed class FloatingText : MonoBehaviour
    {
        private static readonly Color DamageColor = new Color(1f, 0.35f, 0.3f);
        private static readonly Color HealColor = new Color(0.45f, 0.95f, 0.45f);

        private const float LifeSeconds = 0.8f;
        private const float RiseSpeed = 1.4f;

        private TextMesh _text;
        private float _bornAt;

        public static void Damage(Vector3 worldPosition, int amount)
        {
            Spawn(worldPosition, amount.ToString(), DamageColor);
        }

        public static void Heal(Vector3 worldPosition, int amount)
        {
            Spawn(worldPosition, "+" + amount, HealColor);
        }

        public static void Spawn(Vector3 worldPosition, string body, Color color)
        {
            if (string.IsNullOrEmpty(body))
            {
                return;
            }

            // 겹쳐 뜨면 앞의 숫자를 가린다. 조금씩 흩어 놓는다.
            Vector3 jitter = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0f, 0.2f), 0f);

            var go = new GameObject("FloatingText");
            go.transform.position = worldPosition + Vector3.up * 0.8f + jitter;

            var mesh = go.AddComponent<TextMesh>();
            mesh.text = body;
            mesh.color = color;
            mesh.anchor = TextAnchor.LowerCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 크게 뽑아 놓고 줄인다 — 작게 잡으면 확대될 때 글자가 뭉갠다.
            mesh.fontSize = 64;
            mesh.characterSize = 0.07f;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = mesh.font.material;

            // 체력바(200번대)보다 위. 숫자가 막대에 가리면 볼 이유가 없다.
            renderer.sortingOrder = 300;

            var floating = go.AddComponent<FloatingText>();
            floating._text = mesh;
            floating._bornAt = Time.time;
        }

        private void Update()
        {
            float age = (Time.time - _bornAt) / LifeSeconds;
            if (age >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += Vector3.up * (RiseSpeed * Time.deltaTime);

            Color color = _text.color;
            color.a = 1f - age * age;
            _text.color = color;
        }
    }
}

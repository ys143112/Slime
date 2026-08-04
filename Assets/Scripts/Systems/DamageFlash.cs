using System.Collections;
using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-007 (피격 표시). 플레이어와 야생 슬라임이 같이 쓴다 — 피해를
    // 입는 쪽은 IDamageable 구현 둘뿐이고 표시 방식도 같다.
    public sealed class DamageFlash : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private Color flashColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private float duration = 0.12f;

        private Color _baseColor = Color.white;
        private Coroutine _running;

        private void Awake()
        {
            if (target == null)
            {
                target = GetComponent<SpriteRenderer>();
            }

            if (target == null)
            {
                Debug.LogWarning("DamageFlash: SpriteRenderer 가 없어 피격 표시를 비활성화합니다.");
                enabled = false;
                return;
            }

            _baseColor = target.color;
        }

        public void Play()
        {
            if (!enabled || target == null)
            {
                return;
            }

            // 연타로 겹치면 이전 코루틴이 원래 색을 늦게 되돌려 계속 빨간 채로
            // 남는다. 앞선 것을 끊고 다시 시작한다.
            if (_running != null)
            {
                StopCoroutine(_running);
            }

            _running = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            target.color = flashColor;
            yield return new WaitForSeconds(duration);
            target.color = _baseColor;
            _running = null;
        }
    }
}

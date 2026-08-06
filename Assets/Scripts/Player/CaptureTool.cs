using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay
{
    // 기능: spec-002 (돌연변이 판정은 spec-005, 오염 유전자 태깅은 spec-006 CorruptedGeneTagger 가 SlimeCaptured 이벤트를 구독해 처리한다)
    public sealed class CaptureTool : MonoBehaviour
    {
        [SerializeField] private float captureRange = 1.2f;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                TryCapture();
            }
        }

        private void TryCapture()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, captureRange);
            foreach (Collider2D hit in hits)
            {
                WildSlimeAgent agent = hit.GetComponent<WildSlimeAgent>();
                if (agent == null)
                {
                    continue;
                }

                if (!agent.Instance.weakened)
                {
                    Debug.Log("capture_failed: 대상이 약화되지 않았습니다.");
                    PublishCaptureFailed("대상이 약화되지 않았습니다.");
                    continue;
                }

                string biomeId = GameManager.Instance != null ? GameManager.Instance.CurrentBiomeId : string.Empty;
                agent.Instance.capturedBiomeId = biomeId;

                int corruptionTier = BiomeStigmaManager.Instance != null
                    ? BiomeStigmaManager.Instance.GetCorruptionTier(biomeId)
                    : 0;

                // spec-005: 돌연변이가 뜨면 그 안에서 이로치를 다시 굴린다.
                MutantRollService.RollMutantAndShiny(agent.Instance, corruptionTier, biomeId);

                // spec-011: 런 중 포획은 곧바로 보유 목록에 들어가지 않는다.
                // 추출에 성공해야 확정되고, 죽으면 몰수된다.
                RunSatchel.Add(agent.Instance);
                Destroy(hit.gameObject);

                EventBus.Publish(new GameEvent(GameEventId.SlimeCaptured, biomeId, agent.Instance));
                return;
            }

            Debug.Log("capture_failed: 범위 안에 약화된 슬라임이 없습니다.");
            PublishCaptureFailed("범위 안에 약화된 슬라임이 없습니다.");
        }

        private static void PublishCaptureFailed(string reason)
        {
            string biomeId = GameManager.Instance != null ? GameManager.Instance.CurrentBiomeId : string.Empty;
            EventBus.Publish(new GameEvent(GameEventId.SlimeCaptureFailed, biomeId, reason));
        }
    }
}
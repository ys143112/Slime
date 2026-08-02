using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-006
    public sealed class CorruptedGeneTagger : MonoBehaviour
    {
        private void OnEnable()
        {
            EventBus.Subscribe(GameEventId.SlimeCaptured, OnSlimeCaptured);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe(GameEventId.SlimeCaptured, OnSlimeCaptured);
        }

        private void OnSlimeCaptured(GameEvent gameEvent)
        {
            if (BiomeStigmaManager.Instance == null)
            {
                Debug.LogError("CorruptedGeneTagger: BiomeStigmaManager 인스턴스가 없어 오염 유전자 판정을 할 수 없습니다.");
                return;
            }

            SlimeInstance instance = gameEvent.Payload as SlimeInstance;
            if (instance == null)
            {
                return;
            }

            int tier = BiomeStigmaManager.Instance.GetCorruptionTier(gameEvent.BiomeId);
            if (tier >= 1)
            {
                instance.corruptedGeneFlag = true;
            }
        }
    }
}
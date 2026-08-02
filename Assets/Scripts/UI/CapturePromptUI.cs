using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: spec-002
    public sealed class CapturePromptUI : MonoBehaviour
    {
        [SerializeField] private Text promptText;

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
            if (promptText == null)
            {
                Debug.LogWarning("CapturePromptUI: promptText 가 연결되지 않아 피드백을 표시할 수 없습니다.");
                return;
            }

            SlimeInstance instance = gameEvent.Payload as SlimeInstance;
            string species = instance != null ? instance.speciesId : "슬라임";
            promptText.text = $"{species} 포획 성공!";
        }
    }
}
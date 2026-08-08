using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: spec-002
    public sealed class CapturePromptUI : MonoBehaviour
    {
        [SerializeField] private Text promptText;

        // 씬의 Text 에는 uGUI 기본값 "New Text" 가 그대로 남아 있었다. 포획을
        // 한 번도 안 했으면 덮어쓸 일이 없어, 다이브하자마자 플레이어 옆에
        // "New Text" 가 떠 있었다(사용자 신고, 2026-08-08). 씬 세 개를 각각
        // 고치는 대신 여기서 지운다 — 새 바이옴 씬을 만들어도 안 새어 나온다.
        private void Awake()
        {
            if (promptText != null)
            {
                promptText.text = string.Empty;
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe(GameEventId.SlimeCaptured, OnSlimeCaptured);
            EventBus.Subscribe(GameEventId.SlimeCaptureFailed, OnSlimeCaptureFailed);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe(GameEventId.SlimeCaptured, OnSlimeCaptured);
            EventBus.Unsubscribe(GameEventId.SlimeCaptureFailed, OnSlimeCaptureFailed);
        }

        private void OnSlimeCaptured(GameEvent gameEvent)
        {
            if (promptText == null)
            {
                Debug.LogWarning("CapturePromptUI: promptText 가 연결되지 않아 피드백을 표시할 수 없습니다.");
                return;
            }

            SlimeInstance instance = gameEvent.Payload as SlimeInstance;
            string species = instance != null ? instance.speciesId : "Slime";
            promptText.text = $"{species} captured!";
        }

        private void OnSlimeCaptureFailed(GameEvent gameEvent)
        {
            if (promptText == null)
            {
                return;
            }

            string reason = gameEvent.Payload as string;
            promptText.text = string.IsNullOrEmpty(reason) ? "Capture failed" : $"Capture failed: {reason}";
        }
    }
}
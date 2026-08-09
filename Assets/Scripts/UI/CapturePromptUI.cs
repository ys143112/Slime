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
            // 씬의 Text 에는 uGUI 기본값 "New Text" 가 남아 있다 — 통째로 끈다.
            if (promptText != null)
            {
                promptText.text = string.Empty;
                promptText.gameObject.SetActive(false);
            }
        }

        // 이 칸은 플레이어를 따라다니는 월드 캔버스라 무엇을 적어도 인물을 가린다.
        // 포획 결과는 왼쪽 위 배낭 명패와 도감이 말하므로 글자를 아예 안 쓴다
        // (사용자, 2026-08-09). Update 도 없앴다 — 지울 글자가 없다.

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
            SlimeInstance instance = gameEvent.Payload as SlimeInstance;
            string species = instance != null
                ? SlimeSpeciesCatalog.DisplayName(instance.speciesId)
                : "Slime";

            Debug.Log($"capture_ok species={species}");
        }

        private void OnSlimeCaptureFailed(GameEvent gameEvent)
        {
            string reason = gameEvent.Payload as string;
            Debug.Log(string.IsNullOrEmpty(reason) ? "capture_failed" : $"capture_failed reason={reason}");
        }
    }
}
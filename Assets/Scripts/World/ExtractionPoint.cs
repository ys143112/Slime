using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-001
    public sealed class ExtractionPoint : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (GameManager.Instance == null)
            {
                Debug.LogError("ExtractionPoint: GameManager 인스턴스가 없어 탈출을 처리할 수 없습니다.");
                return;
            }

            GameManager.Instance.EndRun(RunEndCause.Extraction);
        }
    }
}
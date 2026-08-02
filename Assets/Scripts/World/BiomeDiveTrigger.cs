using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-001 (다음 시작 바이옴 선택은 spec-004 BiomeStigmaManager.HighestStigmaBiome)
    public sealed class BiomeDiveTrigger : MonoBehaviour
    {
        [SerializeField] private string defaultBiomeId = "biome_default";

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (GameManager.Instance == null)
            {
                Debug.LogError("BiomeDiveTrigger: GameManager 인스턴스가 없어 다이브를 시작할 수 없습니다.");
                return;
            }

            string biomeId = BiomeStigmaManager.Instance != null
                ? BiomeStigmaManager.Instance.HighestStigmaBiome(defaultBiomeId)
                : defaultBiomeId;

            GameManager.Instance.StartRun(biomeId);
        }
    }
}

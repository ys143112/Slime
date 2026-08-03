using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: spec-001 (다음 시작 바이옴 선택은 spec-004 BiomeStigmaManager.HighestStigmaBiome,
    //       안내판 표시는 spec-009)
    public sealed class BiomeDiveTrigger : MonoBehaviour
    {
        [SerializeField] private string defaultBiomeId = "biome_default";
        [SerializeField] private Text destinationText;

        // spec-009: 다음 런이 어느 바이옴에서 시작하는지 진입 전에 보여준다.
        // 낙인이 목적지를 바꾸는 것이 화면에서 관측되지 않으면 없는 기능이다.
        private void Update()
        {
            if (destinationText == null)
            {
                return;
            }

            string biomeId = NextBiomeId();
            BiomeCatalog catalog = GameManager.Instance != null ? GameManager.Instance.Catalog : null;
            string name = catalog != null ? catalog.DisplayNameOf(biomeId) : biomeId;
            destinationText.text = $"다음 목적지: {name}";
        }

        private string NextBiomeId()
        {
            return BiomeStigmaManager.Instance != null
                ? BiomeStigmaManager.Instance.HighestStigmaBiome(defaultBiomeId)
                : defaultBiomeId;
        }

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

            GameManager.Instance.StartRun(NextBiomeId());
        }
    }
}

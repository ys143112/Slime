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

        // spec-004: 목적지 바이옴에 낙인이 쌓여 있으면 표시. 텍스트만으론 오염
        // 여부가 눈에 안 띈다.
        [SerializeField] private GameObject stigmaIcon;

        [Tooltip("다이브 순간 한 번. 화면 전환이 시작되기 전에 울려야 원인과 결과가 이어진다.")]
        [SerializeField] private AudioClip diveSfx;

        private TextMesh _label;

        // 목장에서 이게 무엇인지 그림만으로는 안 읽혔다(팀 QA, 2026-08-08).
        // destinationText 는 어느 씬에도 안 꽂혀 있어 실질적으로 죽은 칸이라,
        // 시설들과 같은 방식(WorldLabel)으로 머리 위에 띄운다.
        private void Awake()
        {
            _label = WorldLabel.Attach(transform, "Dive", 1.1f);
        }

        // spec-009: 다음 런이 어느 바이옴에서 시작하는지 진입 전에 보여준다.
        // 낙인이 목적지를 바꾸는 것이 화면에서 관측되지 않으면 없는 기능이다.
        private void Update()
        {
            string biomeId = NextBiomeId();
            BiomeCatalog catalog = GameManager.Instance != null ? GameManager.Instance.Catalog : null;
            string name = catalog != null ? catalog.DisplayNameOf(biomeId) : biomeId;

            if (_label != null)
            {
                // 목적지만. "밟으면 들어간다" 는 안내는 뺐다 — 포털 그림 위에
                // 서면 바로 들어가지므로 한 번 겪으면 안다.
                //
                // 현상수배는 여기 같이 붙인다. 목장에 안내판을 새로 세우면 씬을
                // 고쳐야 하는데, 다이브 직전에 목표를 읽는 자리가 바로 여기다.
                _label.text = $"Dive: {name}\n{WantedBoard.PosterLine()}";
            }

            if (destinationText != null)
            {
                destinationText.text = $"Next: {name}";
            }

            if (stigmaIcon != null)
            {
                bool hasStigma = BiomeStigmaManager.Instance != null &&
                    BiomeStigmaManager.Instance.GetCorruptionTier(biomeId) > 0;
                if (stigmaIcon.activeSelf != hasStigma)
                {
                    stigmaIcon.SetActive(hasStigma);
                }
            }
        }

        [Tooltip("켜면 낙인과 무관하게 늘 defaultBiomeId 로 간다. 바이옴 하나만 쓸 때.")]
        [SerializeField] private bool forceDefaultBiome = true;

        // 낙인이 목적지를 바꾸는 것이 spec-004 의 핵심이지만, 지금은 늪지·잿벌
        // 지형이 초원만큼 다듬어지지 않아 초원 하나로 고정해 둔다(사용자 결정,
        // 2026-08-08). 플래그를 끄면 원래 동작으로 돌아간다.
        private string NextBiomeId()
        {
            if (forceDefaultBiome || BiomeStigmaManager.Instance == null)
            {
                return defaultBiomeId;
            }

            return BiomeStigmaManager.Instance.HighestStigmaBiome(defaultBiomeId);
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

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx(diveSfx);
            }

            GameManager.Instance.StartRun(NextBiomeId());
        }
    }
}

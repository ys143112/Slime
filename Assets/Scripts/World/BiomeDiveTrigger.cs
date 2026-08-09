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
            _label = WorldLabel.Attach(transform, "다이브", 1.1f);
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
                _label.text = $"다이브: {name}\n{WantedBoard.PosterLine()}";
            }

            if (destinationText != null)
            {
                destinationText.text = $"다음: {name}";
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

        [Tooltip("켜면 순환 없이 늘 defaultBiomeId 로 간다. 바이옴 하나만 쓸 때.")]
        [SerializeField] private bool forceDefaultBiome;

        /// <summary>다음 다이브가 몇 번째 바이옴인지. 목장을 떠나도 남아야 한다.</summary>
        private const string RotationKey = "dive_rotation";

        // 목적지는 목장에 돌아올 때마다 한 번만 읽는다. 매 프레임 읽어도 값은
        // 같지만, 순환 번호가 다이브 순간에 오르므로 캐시가 그 경계를 분명히 한다.
        private string _rolledBiomeId;

        private string NextBiomeId()
        {
            if (string.IsNullOrEmpty(_rolledBiomeId))
            {
                BiomeCatalog catalog = GameManager.Instance != null ? GameManager.Instance.Catalog : null;
                _rolledBiomeId = forceDefaultBiome
                    ? defaultBiomeId
                    : BiomeInRotation(catalog, PlayerPrefs.GetInt(RotationKey, 0), defaultBiomeId);
            }

            return _rolledBiomeId;
        }

        /// <summary>순환 번호에 해당하는 바이옴. 표 순서대로 초원 → 잿벌 → 늪지 → …</summary>
        /// <remarks>
        /// 추첨이었다가 순환으로 바꿨다(사용자, 2026-08-09) — 무작위는 같은 곳이
        /// 연달아 나와 "안 바뀌는 것 같다" 는 인상을 준다. 순서는
        /// <see cref="BiomeCatalog"/> 의 표 순서이므로, 바꾸고 싶으면 코드가 아니라
        /// <c>Assets/SO/BiomeCatalog.asset</c> 의 항목 순서를 바꾼다.
        ///
        /// 낙인은 더 이상 목적지를 끌지 않는다 — 순환이 그 자리를 대신한다.
        /// </remarks>
        public static string BiomeInRotation(BiomeCatalog catalog, int index, string fallbackBiomeId)
        {
            if (catalog == null || catalog.Entries.Count == 0)
            {
                return fallbackBiomeId;
            }

            // 음수 인덱스가 들어와도 표 안으로 접는다(PlayerPrefs 는 손으로 고칠 수 있다).
            int count = catalog.Entries.Count;
            int slot = ((index % count) + count) % count;
            return catalog.Entries[slot].biomeId;
        }

        /// <summary>다이브가 시작될 때 다음 차례로 넘긴다.</summary>
        private static void AdvanceRotation()
        {
            PlayerPrefs.SetInt(RotationKey, PlayerPrefs.GetInt(RotationKey, 0) + 1);
            PlayerPrefs.Save();
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

            string destination = NextBiomeId();

            // 다음 목장 방문 때 다음 바이옴이 걸리도록 여기서 넘긴다 — 다이브가
            // 실제로 시작된 순간이 유일하게 확실한 경계다(안내판만 보고 돌아가도
            // 순서가 밀리면 안 된다).
            AdvanceRotation();

            GameManager.Instance.StartRun(destination);
        }
    }
}

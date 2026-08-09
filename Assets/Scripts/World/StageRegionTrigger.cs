using UnityEngine;

namespace Game.Gameplay
{
    // 기능: 스테이지 맵 생성 (STAGE_A_DESIGN.md §3-2, §7)
    // 방 하나를 덮는 트리거. 플레이어가 들어오면 낙인·오염 티어가 읽는
    // GameManager.CurrentBiomeId 를 이 방의 구역으로 바꾼다 — "어디서
    // 죽었나" 가 씬이 아니라 지금 밟고 있는 구역 기준이 되게 하려는 것이다.
    public sealed class StageRegionTrigger : MonoBehaviour
    {
        public string BiomeId { get; set; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player") || GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.SetCurrentBiome(BiomeId);
        }
    }
}

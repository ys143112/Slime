using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-010
    // 산출량 수치는 여기 있다 — 밸런스 조정에 재컴파일이 필요하면 안 된다.
    [CreateAssetMenu(fileName = "LaborOutputTable", menuName = "SlimeRanch/Labor Output Table")]
    public sealed class LaborOutputTable : ScriptableObject
    {
        [SerializeField] private int baseOutput = 1;
        [SerializeField] private float perDefense = 0.5f;
        [SerializeField] private float perSpeed = 0.5f;

        // 교배로 섞이고 돌연변이로 뒤바뀌는 defense/speed 를 처음으로 읽는 곳이다.
        public int OutputFor(SlimeStatBlock stats)
        {
            if (stats == null)
            {
                return 0;
            }

            float raw = baseOutput + stats.defense * perDefense + stats.speed * perSpeed;
            return Mathf.Max(1, Mathf.FloorToInt(raw));
        }
    }
}

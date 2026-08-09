using System;
using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-007 (이로치는 spec-005 확장)
    [Serializable]
    public class SlimeInstance
    {
        public string speciesId;
        public SlimeStatBlock baseStats;
        public int currentHp;
        public bool weakened;
        public bool mutantFlag;
        public bool corruptedGeneFlag;
        public string capturedBiomeId;

        // 이로치(색 다름) 개체인가. 돌연변이의 하위 종류라 mutantFlag 가 켜진
        // 개체에서만 켜진다 - 이로치는 항상 돌연변이다(그 역은 아니다).
        public bool shinyFlag;

        // 현상수배로 뜬 강화 개체인가. 잡으면 수배가 하나 지워진다 — 잡은 뒤에도
        // 남는 값이라 개체가 들고 있어야 한다(WantedBoard.ReportCaptured).
        public bool bossFlag;

        // 개체마다 다를 수 있어 종이 아니라 개체가 들고 있다. 흰색이면 색 변화
        // 없음이다(SpriteRenderer.color 의 항등원이라 그냥 곱해도 무해하다).
        public Color shinyTint = Color.white;

        public SlimeInstance() { }

        public SlimeInstance(string speciesId, SlimeStatBlock baseStats)
        {
            this.speciesId = speciesId;
            this.baseStats = baseStats;
            currentHp = baseStats.maxHp;
            weakened = false;
            mutantFlag = false;
            corruptedGeneFlag = false;
            capturedBiomeId = string.Empty;
            shinyFlag = false;
            shinyTint = Color.white;
            bossFlag = false;
        }
    }
}
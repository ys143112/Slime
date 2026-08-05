using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: spec-011 (배낭 내용물 스크롤 목록 한 칸)
    public sealed class SatchelSlotView : MonoBehaviour
    {
        [SerializeField] private Text label;

        public void Bind(SlimeInstance instance)
        {
            if (label == null)
            {
                return;
            }

            string tag = instance.mutantFlag ? " [돌연변이]" : instance.corruptedGeneFlag ? " [오염]" : "";
            label.text = $"{instance.speciesId}{tag}  HP {instance.baseStats.maxHp} ATK {instance.baseStats.attack}";
        }
    }
}

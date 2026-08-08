using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-005 확장 (종 목록)
    // speciesId 하나로 그림·이로치 규칙·스탯 편향을 찾는 표. 이 표가 없으면
    // 저장된 개체(speciesId 문자열만 들고 있다)를 다시 그릴 방법이 없다.
    [CreateAssetMenu(fileName = "SlimeSpeciesCatalog", menuName = "SlimeRanch/Slime Species Catalog")]
    public sealed class SlimeSpeciesCatalog : ScriptableObject
    {
        [SerializeField] private List<SlimeSpecies> species = new List<SlimeSpecies>();

        // 씬을 거치지 않고도 찾을 수 있어야 한다 - SlimeInstance 는 순수 데이터라
        // 씬 참조를 들 수 없고, 교배(BreedingPen)·스폰(WildSlimeAgent)·목록 UI 가
        // 저마다 같은 표를 봐야 한다.
        private static SlimeSpeciesCatalog _instance;

        public IReadOnlyList<SlimeSpecies> Species => species;

        /// <summary>
        /// Resources 에서 한 번 읽어 캐시한다. 자산 이름은 SlimeSpeciesCatalog 로 고정이다.
        /// </summary>
        public static SlimeSpeciesCatalog Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<SlimeSpeciesCatalog>("SlimeSpeciesCatalog");
                    if (_instance == null)
                    {
                        Debug.LogError(
                            "SlimeSpeciesCatalog: Resources/SlimeSpeciesCatalog.asset 을 찾지 못했습니다. "
                            + "슬라임 그림과 이로치 색이 적용되지 않습니다.");
                    }
                }

                return _instance;
            }
        }

        public SlimeSpecies Find(string speciesId)
        {
            if (string.IsNullOrEmpty(speciesId))
            {
                return null;
            }

            foreach (SlimeSpecies entry in species)
            {
                if (entry != null && entry.speciesId == speciesId)
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>표를 못 찾아도 호출부가 null 검사를 반복하지 않도록 하는 편의 진입점.</summary>
        public static SlimeSpecies Lookup(string speciesId)
        {
            SlimeSpeciesCatalog catalog = Instance;
            return catalog != null ? catalog.Find(speciesId) : null;
        }

        /// <summary>화면에 쓸 이름. 표에 없으면 speciesId 를 그대로 돌려준다.</summary>
        /// <remarks>
        /// UI 일곱 군데가 저마다 <c>instance.speciesId</c> 를 그대로 찍고 있어
        /// "slime_bog" 같은 내부 식별자가 그대로 보였다. 이름을 바꾸려면 일곱
        /// 군데를 다 고쳐야 했다 — 해석을 여기 한 곳으로 모은다.
        /// </remarks>
        public static string DisplayName(string speciesId)
        {
            SlimeSpecies species = Lookup(speciesId);
            return species != null && !string.IsNullOrEmpty(species.displayName)
                ? species.displayName
                : speciesId;
        }
    }
}

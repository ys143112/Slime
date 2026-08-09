using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>어떤 종을 잡아 봤는지 남기는 도감. 게임의 목표 추적판이다.</summary>
    /// <remarks>
    /// 지금까지 "무엇을 향해 플레이하는가" 가 화면 어디에도 없었다(팀 QA,
    /// 2026-08-09). 종은 다섯인데 그중 둘은 교배로만 나오므로, "몇 종을 모았나"
    /// 가 그대로 진행도가 된다.
    ///
    /// <b>로스터가 아니라 별도로 남긴다.</b> 잡은 슬라임은 죽거나(배낭 몰수)
    /// 동행으로 나갔다 사라지지만, 한 번 본 종은 도감에서 지워지면 안 된다.
    ///
    /// 이로치는 따로 센다 — 돌연변이 안에서 다시 굴리는 25% 라, 종을 다 모은
    /// 뒤에도 남는 목표가 된다.
    /// </remarks>
    public static class SlimeBestiary
    {
        private const string SaveKey = "bestiary";

        [Serializable]
        private sealed class Entries
        {
            public List<string> caught = new List<string>();
            public List<string> shiny = new List<string>();
        }

        private static Entries _record;

        /// <summary>이 종을 한 번이라도 잡아 봤나.</summary>
        public static bool IsCaught(string speciesId) => Data.caught.Contains(speciesId);

        /// <summary>이 종의 이로치를 잡아 봤나.</summary>
        public static bool IsShinyCaught(string speciesId) => Data.shiny.Contains(speciesId);

        public static int CaughtCount => Data.caught.Count;

        public static int ShinyCount => Data.shiny.Count;

        /// <summary>도감에 실리는 전체 종 수. 카탈로그가 곧 목표 목록이다.</summary>
        public static int TotalSpecies =>
            SlimeSpeciesCatalog.Instance != null ? SlimeSpeciesCatalog.Instance.Species.Count : 0;

        /// <summary>도감이 바뀌었다 — 열려 있는 도감 창이 다시 그린다.</summary>
        public static event Action Changed;

        /// <summary>
        /// 개체 하나를 도감에 올린다. 이미 있는 종이면 아무 일도 하지 않는다.
        /// </summary>
        /// <remarks>
        /// 포획(<see cref="RunSatchel.Add"/>)과 부화·회수(<see cref="PlayerRoster.Add"/>)
        /// 두 자리에서 부른다. 배낭에서 부르는 이유: 도감은 살아 돌아오지 못해도
        /// 남아야 한다 — 죽어서 잃는 것은 슬라임이지 "봤다는 사실" 이 아니다.
        /// </remarks>
        public static void Record(SlimeInstance instance)
        {
            if (instance == null || string.IsNullOrEmpty(instance.speciesId))
            {
                return;
            }

            bool changed = false;

            if (!Data.caught.Contains(instance.speciesId))
            {
                Data.caught.Add(instance.speciesId);
                changed = true;
                Debug.Log($"bestiary_new species={instance.speciesId} total={Data.caught.Count}/{TotalSpecies}");
            }

            if (instance.shinyFlag && !Data.shiny.Contains(instance.speciesId))
            {
                Data.shiny.Add(instance.speciesId);
                changed = true;
                Debug.Log($"bestiary_new_shiny species={instance.speciesId}");
            }

            if (!changed)
            {
                return;
            }

            Save();
            Changed?.Invoke();
        }

        private static Entries Data
        {
            get
            {
                if (_record == null)
                {
                    // 저장 파일이 없으면 빈 도감으로 시작한다. Load 는 기본값을
                    // 그대로 돌려주므로 null 검사는 여기 한 번이면 된다.
                    _record = SaveSystem.Load(SaveKey, new Entries()) ?? new Entries();
                }

                return _record;
            }
        }

        private static void Save()
        {
            SaveSystem.Save(SaveKey, Data);
        }

        /// <summary>테스트·초기화용. 저장 파일까지 비운다.</summary>
        public static void Clear()
        {
            _record = new Entries();
            Save();
            Changed?.Invoke();
        }
    }
}

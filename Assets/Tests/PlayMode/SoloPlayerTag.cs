using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Tests
{
    // Hub·Biome 씬에는 Player 프리팹 인스턴스가 들어있다. 앞선 테스트가 EndRun 으로
    // Hub 를 띄우고 나면 그 플레이어가 씬에 남아, 야생 슬라임의
    // GameObject.FindGameObjectWithTag("Player") 가 테스트용 플레이어 대신 그쪽을
    // 집을 수 있다 — 어느 쪽을 집을지는 보장되지 않아 실행마다 결과가 갈렸다.
    // 테스트가 도는 동안에는 태그를 가진 오브젝트가 테스트용 하나뿐이어야 한다.
    internal sealed class SoloPlayerTag
    {
        private readonly List<GameObject> _silenced = new List<GameObject>();

        public void SilenceExisting()
        {
            foreach (GameObject tagged in GameObject.FindGameObjectsWithTag("Player"))
            {
                tagged.SetActive(false);
                _silenced.Add(tagged);
            }
        }

        public void Restore()
        {
            foreach (GameObject tagged in _silenced)
            {
                if (tagged != null)
                {
                    tagged.SetActive(true);
                }
            }

            _silenced.Clear();
        }
    }
}

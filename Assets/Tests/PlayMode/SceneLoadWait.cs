using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Gameplay.Tests
{
    // GameManager.EndRun 은 Hub 씬 로드를 예약만 하고 돌아온다. 그 로드는 프레임
    // 끝에 실행되므로, 한 프레임만 기다리고 테스트를 끝내면 다음 테스트가 도는
    // 도중에 로드가 터져 그 테스트의 오브젝트를 지운다 — 실행할 때마다 실패
    // 위치가 달라지는 원인이다. 로드가 실제로 끝날 때까지 기다린다.
    internal static class SceneLoadWait
    {
        private const string HubSceneName = "Hub";
        private const int MaxFrames = 300;

        public static IEnumerator UntilRunEndSceneLoaded()
        {
            for (int frame = 0; frame < MaxFrames; frame++)
            {
                if (SceneManager.GetActiveScene().name == HubSceneName)
                {
                    // 로드된 씬의 Awake/Start 가 한 번 돌게 두고 넘어간다.
                    yield return null;
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"EndRun 이 예약한 {HubSceneName} 씬 로드가 {MaxFrames} 프레임 안에 끝나지 않았습니다.");
        }
    }
}

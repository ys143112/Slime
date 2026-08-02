using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Gameplay
{
    public enum RunState
    {
        Hub,
        Diving,
        Dead,
        Extracted,
    }

    public enum RunEndCause
    {
        Death,
        Extraction,
    }

    // 기능: spec-001
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private string hubSceneName = "Hub";
        [SerializeField] private string biomeSceneName = "Biome";

        public RunState CurrentState { get; private set; } = RunState.Hub;
        public string CurrentBiomeId { get; private set; } = "biome_default";

        private string _activeContentScene = "";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // 씬이 Additive 로 켜질 때마다 매니저가 중복되는 것을 막는다.
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartRun(string biomeId)
        {
            CurrentBiomeId = biomeId;
            CurrentState = RunState.Diving;
            LoadContentScene(biomeSceneName);

            var gameEvent = new GameEvent(GameEventId.RunStarted, biomeId);
            EventBus.Publish(gameEvent);
            RunLogWriter.AppendLine($"RunStarted biome={biomeId}");
        }

        public void EndRun(RunEndCause cause)
        {
            CurrentState = cause == RunEndCause.Death ? RunState.Dead : RunState.Extracted;

            var gameEvent = new GameEvent(GameEventId.RunEnded, CurrentBiomeId, cause);
            EventBus.Publish(gameEvent);
            RunLogWriter.AppendLine($"RunEnded biome={CurrentBiomeId} cause={cause}");

            LoadContentScene(hubSceneName);
            CurrentState = RunState.Hub;
        }

        private void LoadContentScene(string sceneName)
        {
            if (!string.IsNullOrEmpty(_activeContentScene))
            {
                SceneManager.UnloadSceneAsync(_activeContentScene);
            }

            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
            _activeContentScene = sceneName;
        }
    }
}
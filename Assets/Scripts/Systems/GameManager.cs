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
        [SerializeField] private BiomeCatalog biomeCatalog;

        public RunState CurrentState { get; private set; } = RunState.Hub;
        public string CurrentBiomeId { get; private set; } = "biome_default";

        // spec-009: 야생 슬라임 종 추첨과 목장 안내판이 같은 표를 본다.
        public BiomeCatalog Catalog => biomeCatalog;

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

        private void Start()
        {
            // Boot 씬에는 매니저만 있다. 목장을 띄우지 않으면 런이 시작될 곳이
            // 없어 게임이 빈 화면에서 멈춘다.
            if (string.IsNullOrEmpty(_activeContentScene))
            {
                LoadContentScene(hubSceneName);
            }
        }

        public void StartRun(string biomeId)
        {
            CurrentBiomeId = biomeId;
            CurrentState = RunState.Diving;

            // spec-009: 바이옴마다 자기 씬이 있다. 표에 없는 id 는 기본 씬으로
            // 떨어뜨린다 — 낙인이 아직 없는 첫 런도 열려야 한다.
            LoadContentScene(SceneNameFor(biomeId));

            var gameEvent = new GameEvent(GameEventId.RunStarted, biomeId);
            EventBus.Publish(gameEvent);
            Debug.Log($"dive_started biome={biomeId}");
            RunLogWriter.AppendLine($"RunStarted biome={biomeId}");
        }

        public string SceneNameFor(string biomeId)
        {
            BiomeEntry entry = biomeCatalog != null ? biomeCatalog.Find(biomeId) : null;
            return entry != null && !string.IsNullOrEmpty(entry.sceneName) ? entry.sceneName : biomeSceneName;
        }

        public void EndRun(RunEndCause cause)
        {
            CurrentState = cause == RunEndCause.Death ? RunState.Dead : RunState.Extracted;

            // spec-011: 낙인(spec-004)이 붙기 전에 전리품을 정산한다. 순서가
            // 바뀌어도 결과는 같지만, 로그가 "무엇을 잃고 무엇을 얻었는지" 다음에
            // 오염이 오르는 순으로 남아야 읽힌다.
            if (cause == RunEndCause.Extraction)
            {
                RunSatchel.Commit();
            }
            else
            {
                RunSatchel.Discard();
            }

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
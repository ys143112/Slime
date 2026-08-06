using UnityEngine;

namespace Game.Gameplay
{
    // 기능: 배경음/효과음 볼륨 뼈대. 실제 BGM·SFX 클립은 아직 없어 재생 로직
    // 없이 볼륨 값만 들고 있다 - 클립이 생기면 PlayBgm/PlaySfx 에 채워 넣는다.
    public sealed class AudioManager : MonoBehaviour
    {
        private const string BgmVolumeKey = "BgmVolume";
        private const string SfxVolumeKey = "SfxVolume";

        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("배경음 — 클립을 끼우면 시작할 때 자동으로 재생된다")]
        [SerializeField] private AudioClip titleBgm;
        [SerializeField] private AudioClip hubBgm;
        [SerializeField] private AudioClip biomeBgm;

        public float BgmVolume
        {
            get => bgmSource != null ? bgmSource.volume : PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
            set
            {
                if (bgmSource != null)
                {
                    bgmSource.volume = value;
                }

                PlayerPrefs.SetFloat(BgmVolumeKey, value);
            }
        }

        public float SfxVolume
        {
            get => sfxSource != null ? sfxSource.volume : PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
            set
            {
                if (sfxSource != null)
                {
                    sfxSource.volume = value;
                }

                PlayerPrefs.SetFloat(SfxVolumeKey, value);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            BgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
            SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        }

        private void Start()
        {
            // 클립이 없으면 조용히 넘어간다 — 아직 음원이 없어도 게임은 돈다.
            PlayBgm(titleBgm);
        }

        /// <summary>
        /// 씬 성격에 맞는 배경음으로 갈아탄다. 같은 곡이면 다시 시작하지 않는다 —
        /// 씬을 오갈 때마다 곡이 처음으로 튀면 끊긴 것처럼 들린다.
        /// </summary>
        public void PlaySceneBgm(string sceneName)
        {
            AudioClip next = sceneName == "Boot" ? titleBgm
                : sceneName == "Hub" ? hubBgm
                : biomeBgm;

            if (next == null || (bgmSource != null && bgmSource.clip == next && bgmSource.isPlaying))
            {
                return;
            }

            PlayBgm(next);
        }

        public void PlayBgm(AudioClip clip)
        {
            if (bgmSource == null || clip == null)
            {
                return;
            }

            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        public void PlaySfx(AudioClip clip)
        {
            if (sfxSource == null || clip == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip);
        }
    }
}

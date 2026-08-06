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

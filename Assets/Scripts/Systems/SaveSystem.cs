using System;
using System.IO;
using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-001
    public static class SaveSystem
    {
        [Serializable]
        private sealed class Wrapper<T>
        {
            public T value;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL 은 파일 IO 가 없다. PlayerPrefs 는 IndexedDB 로 내려가므로
        // 브라우저에서도 런 사이에 남는다. 키는 파일 경로 대신 접두사로 나눈다.
        private static string PrefsKeyFor(string key) => "save_" + key;
#else
        private static string PathFor(string key)
        {
            return Path.Combine(Application.persistentDataPath, "Saves", key + ".json");
        }
#endif

        public static void Save<T>(string key, T data)
        {
            string json = JsonUtility.ToJson(new Wrapper<T> { value = data });

#if UNITY_WEBGL && !UNITY_EDITOR
            PlayerPrefs.SetString(PrefsKeyFor(key), json);

            // 브라우저 탭은 예고 없이 닫힌다 — 즉시 내려쓰지 않으면 세이브가 날아간다.
            PlayerPrefs.Save();
#else
            string path = PathFor(key);
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, json);
#endif
        }

        public static T Load<T>(string key, T defaultValue)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string json = PlayerPrefs.GetString(PrefsKeyFor(key), string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return defaultValue;
            }
#else
            string path = PathFor(key);
            if (!File.Exists(path))
            {
                return defaultValue;
            }

            string json = File.ReadAllText(path);
#endif

            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            if (wrapper == null)
            {
                // 저장 파일이 손상된 경우 기본값으로 되돌린다.
                Debug.LogWarning($"SaveSystem: '{key}' 저장 파일을 읽지 못해 기본값을 사용합니다.");
                return defaultValue;
            }

            return wrapper.value;
        }
    }
}

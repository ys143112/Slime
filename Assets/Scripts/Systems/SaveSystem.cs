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

        private static string PathFor(string key)
        {
            return Path.Combine(Application.persistentDataPath, "Saves", key + ".json");
        }

        public static void Save<T>(string key, T data)
        {
            string path = PathFor(key);
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonUtility.ToJson(new Wrapper<T> { value = data });
            File.WriteAllText(path, json);
        }

        public static T Load<T>(string key, T defaultValue)
        {
            string path = PathFor(key);
            if (!File.Exists(path))
            {
                return defaultValue;
            }

            string json = File.ReadAllText(path);
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
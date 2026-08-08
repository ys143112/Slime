using System;
using System.IO;
using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-001
    public static class RunLogWriter
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        private static readonly string LogPath = Path.Combine(Application.persistentDataPath, "Logs", "run.log");
#endif

        public static void AppendLine(string line)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // 브라우저에는 쓸 수 있는 파일시스템이 없다. 이벤트마다 불리는
            // 자리라 예외가 한 번 나면 그때부터 런이 통째로 멈춘다.
            Debug.Log($"run_log {line}");
#else
            string directory = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.AppendAllText(LogPath, $"[{DateTime.UtcNow:O}] {line}{Environment.NewLine}");
#endif
        }
    }
}

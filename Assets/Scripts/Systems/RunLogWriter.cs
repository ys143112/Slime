using System;
using System.IO;
using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-001
    public static class RunLogWriter
    {
        private static readonly string LogPath = Path.Combine(Application.persistentDataPath, "Logs", "run.log");

        public static void AppendLine(string line)
        {
            string directory = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.AppendAllText(LogPath, $"[{DateTime.UtcNow:O}] {line}{Environment.NewLine}");
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Tests
{
    // 합격 기준이 "콘솔에 이 줄이 남는다" 인 항목들을 검증하는 자리. 전투와 정산은
    // 한순간에 지나가므로 로그가 유일한 흔적이다.
    internal sealed class LogCatcher
    {
        private readonly string _prefix;

        public List<string> Lines { get; } = new List<string>();

        public LogCatcher(string prefix)
        {
            _prefix = prefix;
            Application.logMessageReceived += Collect;
        }

        public void Stop()
        {
            Application.logMessageReceived -= Collect;
        }

        private void Collect(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Log && condition.StartsWith(_prefix))
            {
                Lines.Add(condition);
            }
        }
    }
}

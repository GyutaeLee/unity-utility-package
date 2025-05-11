using System;
using System.Threading;

namespace qbot.Utility
{
    public static class CancellationTokenUtility
    {
        /// <summary>
        /// Creates a CancellationTokenSource that cancels itself after a specified timeout (in milliseconds).
        /// </summary>
        /// <param name="timeoutMilliseconds">Timeout duration in milliseconds</param>
        public static CancellationTokenSource CreateTimedCancellationTokenSource(int timeoutMilliseconds)
        {
            var cts = new CancellationTokenSource();
            _ = TimerAutoCancel(cts, timeoutMilliseconds);
            return cts;
        }

        private static async System.Threading.Tasks.Task TimerAutoCancel(CancellationTokenSource cts, int delayMs)
        {
            try
            {
                await System.Threading.Tasks.Task.Delay(delayMs, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return; // 사용자가 먼저 취소한 경우
            }

            if (cts.IsCancellationRequested == false)
            {
                cts.Cancel();
            }
        }
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DSharpPlus.Net.WebSocket
{
    // Licensed from Clyde.NET (etc; I don't know how licenses work)

    internal sealed class SocketLock : IDisposable
    {
        public ulong ApplicationId { get; }

        private SemaphoreSlim LockSemaphore { get; }
        private CancellationTokenSource TimeoutCancelSource { get; set; }
        private CancellationToken TimeoutCancel => TimeoutCancelSource.Token;
        private Task UnlockTask { get; set; }

        public SocketLock(ulong appId)
        {
            ApplicationId = appId;
            TimeoutCancelSource = null;
            LockSemaphore = new SemaphoreSlim(1);
        }

        public async Task LockAsync()
        {
            await LockSemaphore.WaitAsync().ConfigureAwait(false);

            TimeoutCancelSource = new CancellationTokenSource();
            UnlockTask = Task.Delay(TimeSpan.FromSeconds(30), TimeoutCancel);
            _ = UnlockTask.ContinueWith(InternalUnlock, TaskContinuationOptions.NotOnCanceled);
        }

        public void UnlockAfter(TimeSpan unlockDelay)
        {
            if (TimeoutCancelSource == null || LockSemaphore.CurrentCount > 0)
                return; // it's not unlockable because it's post-IDENTIFY or not locked

            try
            {
                TimeoutCancelSource.Cancel();
                TimeoutCancelSource.Dispose();
            }
            catch { }
            TimeoutCancelSource = null;

            UnlockTask = Task.Delay(unlockDelay, CancellationToken.None);
            _ = UnlockTask.ContinueWith(InternalUnlock);
        }

        public Task WaitAsync()
            => LockSemaphore.WaitAsync();

        public void Dispose()
        {
            try
            {
                TimeoutCancelSource?.Cancel();
                TimeoutCancelSource?.Dispose();
            }
            catch { }
        }

        private void InternalUnlock(Task t)
            => LockSemaphore.Release();
    }
}
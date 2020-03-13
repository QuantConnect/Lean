/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
 *
 * Changes made from original:
 *   - Removed Nullable reference type definitions for compatibility with C# 6
*/

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class SynchronizationQueue : IDisposable
    {
        private readonly BlockingCollection<Action> _actions =
            new BlockingCollection<Action>(new ConcurrentQueue<Action>());

        private readonly CancellationTokenSource _cancellationTokenSource =
            new CancellationTokenSource();

        public SynchronizationQueue()
        {
            var factory = new TaskFactory(_cancellationTokenSource.Token);
            factory.StartNew(processingTask, _cancellationTokenSource.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        public event Action<Exception> OnError;

        public void Enqueue(Action action) =>
            _actions.Add(action, _cancellationTokenSource.Token);

        [SuppressMessage(
            "Design", "CA1031:Do not catch general exception types",
            Justification = "Expected behavior - we report exceptions via OnError event.")]
        private void processingTask()
        {
            try
            {
                foreach (var action in _actions
                    .GetConsumingEnumerable(_cancellationTokenSource.Token))
                {
                    try
                    {
                        action();
                    }
                    catch (Exception exception)
                    {
                        OnError?.Invoke(exception);
                    }
                }
            }
            catch (ObjectDisposedException exception)
            {
                Trace.TraceInformation(exception.Message);
            }
            catch (OperationCanceledException exception)
            {
                Trace.TraceInformation(exception.Message);
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
            _actions.Dispose();
        }
    }
}

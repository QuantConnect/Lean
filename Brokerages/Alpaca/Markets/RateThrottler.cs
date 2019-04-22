/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class RateThrottler : IThrottler, IDisposable
    {
        private sealed class NextRetryGuard : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

            /// <summary>
            /// Used to create a random length delay when server responds with a Http status like 503, but provides no Retry-After header.
            /// </summary>
            private readonly Random _randomRetryWait = new Random();

            private DateTime _nextRetryTime = DateTime.MinValue;

            public async Task WaitToProceed()
            {
                var delay = GetDelayTillNextRetryTime();

                if (delay.TotalMilliseconds < 0)
                {
                    return;
                }

                await Task.Delay(delay);
            }

            public void SetNextRetryTimeRandom()
            {
                // TODO: If server logic fixed to provide Retry-After, this whole IF block will be dead code to remove
                SetNextRetryTime(DateTime.UtcNow.AddMilliseconds(
                    _randomRetryWait.Next(1000, 5000)));
            }

            public void SetNextRetryTime(DateTime nextRetryTime)
            {
                if (nextRetryTime < DateTime.UtcNow)
                {
                    return;
                }

                _lock.EnterWriteLock();
                try
                {
                    if (nextRetryTime > _nextRetryTime)
                    {
                        _nextRetryTime = nextRetryTime;
                    }
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            public TimeSpan GetDelayTillNextRetryTime()
            {
                _lock.EnterReadLock();
                try
                {
                    return _nextRetryTime.Subtract(DateTime.UtcNow);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            public void Dispose()
            {
                _lock?.Dispose();
            }
        }

        private readonly NextRetryGuard _nextRetryGuard = new NextRetryGuard();

        /// <summary>
        /// Times (in millisecond ticks) at which the semaphore should be exited.
        /// </summary>
        private readonly ConcurrentQueue<Int32> _exitTimes;

        /// <summary>
        /// Semaphore used to count and limit the number of occurrences per unit time.
        /// </summary>
        private readonly SemaphoreSlim _throttleSemaphore;

        /// <summary>
        /// Timer used to trigger exiting the semaphore.
        /// </summary>
        private readonly Timer _exitTimer;

        /// <summary>
        /// The length of the time unit, in milliseconds.
        /// </summary>
        private readonly Int32 _timeUnitMilliseconds;

        /// <summary>
        /// List of HTTP status codes which when received should initiate a retry of the affected request.
        /// </summary>
        private readonly ISet<Int32> _retryHttpStatuses;

        /// <summary>
        /// Creates new instance of <see cref="RateThrottler"/> object with parameters
        /// specified in <paramref name="throttleParameters"/> parameter.
        /// </summary>
        /// <param name="throttleParameters"></param>
        public RateThrottler(
            ThrottleParameters throttleParameters)
        {
            _timeUnitMilliseconds = (Int32)throttleParameters.TimeUnit.TotalMilliseconds;
            MaxRetryAttempts = throttleParameters.MaxRetryAttempts;
            _retryHttpStatuses = new HashSet<Int32>(
                throttleParameters.RetryHttpStatuses ?? Enumerable.Empty<Int32>());

            // Create the throttle semaphore, with the number of occurrences as the maximum count.
            _throttleSemaphore = new SemaphoreSlim(throttleParameters.Occurrences, throttleParameters.Occurrences);

            // Create a queue to hold the semaphore exit times.
            _exitTimes = new ConcurrentQueue<Int32>();

            // Create a timer to exit the semaphore. Use the time unit as the original
            // interval length because that's the earliest we will need to exit the semaphore.
            _exitTimer = new Timer(exitTimerCallback, null, _timeUnitMilliseconds, -1);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _throttleSemaphore.Dispose();
            _nextRetryGuard.Dispose();
            _exitTimer.Dispose();
        }

        /// <inheritdoc />
        public Int32 MaxRetryAttempts { get;}

        /// <inheritdoc />
        public async Task WaitToProceed()
        {
            await _nextRetryGuard.WaitToProceed();

            // Block until we can enter the semaphore or until the timeout expires.
            var entered = _throttleSemaphore.Wait(Timeout.Infinite);

            // If we entered the semaphore, compute the corresponding exit time 
            // and add it to the queue.
            if (entered)
            {
                var timeToExit = unchecked(Environment.TickCount + _timeUnitMilliseconds);
                _exitTimes.Enqueue(timeToExit);
            }
        }

        // Callback for the exit timer that exits the semaphore based on exit times 
        // in the queue and then sets the timer for the next exit time.
        private void exitTimerCallback(Object state)
        {
            var nextRetryDelay = _nextRetryGuard.GetDelayTillNextRetryTime().TotalMilliseconds;
            if (nextRetryDelay > 0)
            {
                _exitTimer.Change((Int32)nextRetryDelay, Timeout.Infinite);
                return;
            }

            // While there are exit times that are passed due still in the queue,
            // exit the semaphore and dequeue the exit time.
            Int32 exitTime;
            while (_exitTimes.TryPeek(out exitTime)
                   && unchecked(exitTime - Environment.TickCount) <= 0)
            {
                _throttleSemaphore.Release();
                _exitTimes.TryDequeue(out exitTime);
            }

            // Try to get the next exit time from the queue and compute
            // the time until the next check should take place. If the 
            // queue is empty, then no exit times will occur until at least
            // one time unit has passed.
            var timeUntilNextCheck = _exitTimes.TryPeek(out exitTime)
                ? unchecked(exitTime - Environment.TickCount)
                :_timeUnitMilliseconds;

            // Set the timer.
            _exitTimer.Change(timeUntilNextCheck, Timeout.Infinite);
        }

        /// <inheritdoc />
        public Boolean CheckHttpResponse(HttpResponseMessage response)
        {
            // Adhere to server reported instructions
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }

            // Accomodate server specified delays in Retry-After headers
            var retryAfterHeader = response.Headers.RetryAfter;
            if (retryAfterHeader != null)
            {
                if (retryAfterHeader.Delta.HasValue)
                {
                    _nextRetryGuard.SetNextRetryTime(DateTime.UtcNow.Add(retryAfterHeader.Delta.Value));
                    return false;
                }

                if (retryAfterHeader.Date.HasValue)
                {
                    _nextRetryGuard.SetNextRetryTime(retryAfterHeader.Date.Value.UtcDateTime);
                }
            }

            // Server unavailable, or Too many requests (429 can happen when this client competes with another client, e.g. mobile app)
            if (response.StatusCode == (HttpStatusCode)429 ||
                response.StatusCode == (HttpStatusCode)503)
            {
                _nextRetryGuard.SetNextRetryTimeRandom();
                return false;
            }

            // Accomodate retries on statuses indicated by caller
            if (_retryHttpStatuses.Contains((Int32)response.StatusCode))
            {
                return false;
            }

            // Allow framework to throw the exception
            response.EnsureSuccessStatusCode();

            return true;
        }
    }
}

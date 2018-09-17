/*
 * The official C# API client for alpaca brokerage
 * https://github.com/alpacahq/alpaca-trade-api-csharp
*/

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class RateThrottler : IThrottler, IDisposable
    {
        public Int32 MaxAttempts { get; }

        /// <summary>
        /// Times (in millisecond ticks) at which the semaphore should be exited.
        /// </summary>
        private readonly ConcurrentQueue<Int32> _exitTimes;

        /// <summary>
        /// Semaphore used to count and limit the number of occurrences per unit time.
        /// </summary>
        private readonly SemaphoreSlim _semaphore;

        /// <summary>
        /// Timer used to trigger exiting the semaphore.
        /// </summary>
        private readonly Timer _exitTimer;

        /// <summary>
        /// The length of the time unit, in milliseconds.
        /// </summary>
        private readonly Int32 _timeUnitMilliseconds;

        /// <summary>
        /// Initializes a <see cref="RateThrottler" /> with a rate of <paramref name="occurrences" />
        /// per <paramref name="timeUnit" />.
        /// </summary>
        /// <param name="occurrences">Number of occurrences allowed per unit of time.</param>
        /// <param name="maxAttempts">Number of maximal retry attampts in case of any HTTP error.</param>
        /// <param name="timeUnit">Length of the time unit.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="occurrences" />, <paramref name="maxAttempts"/> or <paramref name="timeUnit" /> is negative.
        /// </exception>
        public RateThrottler(
            Int32 occurrences,
            Int32 maxAttempts,
            TimeSpan timeUnit)
        {
            if (occurrences <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(occurrences),
                    "Number of occurrences must be a positive integer");
            }
            if (maxAttempts <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAttempts),
                    "Number of maximal retry attempts must be a positive integer");
            }
            if (timeUnit != timeUnit.Duration())
            {
                throw new ArgumentOutOfRangeException(nameof(timeUnit), "Time unit must be a positive span of time");
            }
            if (timeUnit >= TimeSpan.FromMilliseconds(UInt32.MaxValue))
            {
                throw new ArgumentOutOfRangeException(nameof(timeUnit), "Time unit must be less than 2^32 milliseconds");
            }

            _timeUnitMilliseconds = (Int32) timeUnit.TotalMilliseconds;
            MaxAttempts = maxAttempts;

            // Create the semaphore, with the number of occurrences as the maximum count.
            _semaphore = new SemaphoreSlim(occurrences, occurrences);

            // Create a queue to hold the semaphore exit times.
            _exitTimes = new ConcurrentQueue<Int32>();

            // Create a timer to exit the semaphore. Use the time unit as the original
            // interval length because that's the earliest we will need to exit the semaphore.
            _exitTimer = new Timer(exitTimerCallback, null, _timeUnitMilliseconds, -1);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _semaphore.Dispose();
            _exitTimer.Dispose();
        }

        /// <inheritdoc />
        public void WaitToProceed()
        {

            // Block until we can enter the semaphore or until the timeout expires.
            var entered = _semaphore.Wait(Timeout.Infinite);

            // If we entered the semaphore, compute the corresponding exit time 
            // and add it to the queue.
            if (entered)
            {
                var timeToExit = unchecked(Environment.TickCount + _timeUnitMilliseconds);
                _exitTimes.Enqueue(timeToExit);
            }
        }

        // Callback for the exit timer that exits the semaphore based on exit times 
        // in the queue and then sets the timer for the nextexit time.
        private void exitTimerCallback(Object state)
        {
            // While there are exit times that are passed due still in the queue,
            // exit the semaphore and dequeue the exit time.
            Int32 exitTime;
            while (_exitTimes.TryPeek(out exitTime)
                   && unchecked(exitTime - Environment.TickCount) <= 0)
            {
                _semaphore.Release();
                _exitTimes.TryDequeue(out exitTime);
            }

            // Try to get the next exit time from the queue and compute
            // the time until the next check should take place. If the 
            // queue is empty, then no exit times will occur until at least
            // one time unit has passed.
            Int32 timeUntilNextCheck;
            if (_exitTimes.TryPeek(out exitTime))
            {
                timeUntilNextCheck = unchecked(exitTime - Environment.TickCount);
            }
            else
            {
                timeUntilNextCheck = _timeUnitMilliseconds;
            }

            // Set the timer.
            _exitTimer.Change(timeUntilNextCheck, -1);
        }
    }
}
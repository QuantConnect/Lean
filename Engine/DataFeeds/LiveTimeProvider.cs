
using System;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Live time provide which supports an initial warmup period using the given time provider <see cref="SubscriptionFrontierTimeProvider"/>
    /// </summary>
    public class LiveTimeProvider : ITimeProvider
    {
        private DateTime _previous;
        private readonly DateTime _liveStart;
        private readonly ITimeProvider _realTime;
        private ITimeProvider _warmupTimeProvider;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="realTime">Real time provider</param>
        public LiveTimeProvider(ITimeProvider realTime)
        {
            _realTime = realTime;
            _liveStart = _realTime.GetUtcNow();
        }

        /// <summary>
        /// Fully initializes this instance providing the initial warmup time provider to use
        /// </summary>
        /// <param name="warmupTimeProvider">The warmup provider to use</param>
        public void Initialize(ITimeProvider warmupTimeProvider)
        {
            _warmupTimeProvider = warmupTimeProvider;
        }

        /// <summary>
        /// Gets the current time in UTC
        /// </summary>
        /// <returns>The current time in UTC</returns>
        public DateTime GetUtcNow()
        {
            if(ReferenceEquals(_realTime, _warmupTimeProvider))
            {
                // warmup ended
                return _realTime.GetUtcNow();
            }

            if (_warmupTimeProvider == null)
            {
                // don't let any live data point through until we are fully initialized
                return Time.BeginningOfTime;
            }

            var newTime = _warmupTimeProvider.GetUtcNow();
            if (_previous == newTime || newTime >= _liveStart)
            {
                // subscription time provider swap ended, start using live
                _warmupTimeProvider = _realTime;
                return GetUtcNow();
            }
            _previous = newTime;
            return newTime;
        }
    }
}

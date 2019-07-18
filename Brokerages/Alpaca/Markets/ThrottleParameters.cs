﻿/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Helper class for storing parameters required for initializing rate throttler in <see cref="RestClient"/> class.
    /// </summary>
    internal sealed class ThrottleParameters
    {
        private const Int32 DEFAULT_OCCURRENCES = 200;

        private const Int32 DEFAULT_MAX_RETRY_ATTEMPT = 5;

        private readonly TimeSpan _defaultTimeUnit = TimeSpan.FromMinutes(1);

        private readonly Lazy<IThrottler> _rateThrottler;

        private Int32 _occurrences;

        private TimeSpan _timeUnit;

        private Int32 _maxRetryAttempts;

        private HashSet<Int32> _retryHttpStatuses;

        /// <summary>
        /// Creates new instance of <see cref="ThrottleParameters"/> object.
        /// </summary>
        /// <param name="occurrences"></param>
        /// <param name="timeUnit"></param>
        /// <param name="maxRetryAttempts"></param>
        /// <param name="retryHttpStatuses"></param>
        public ThrottleParameters(
            Int32? occurrences = null,
            TimeSpan? timeUnit = null,
            Int32? maxRetryAttempts = null,
            IEnumerable<Int32> retryHttpStatuses = null)
        {
            Occurrences = occurrences ?? DEFAULT_OCCURRENCES;
            TimeUnit = timeUnit ?? _defaultTimeUnit;
            MaxRetryAttempts = maxRetryAttempts ?? DEFAULT_MAX_RETRY_ATTEMPT;
            RetryHttpStatuses = retryHttpStatuses;

            _rateThrottler = new Lazy<IThrottler>(() => new RateThrottler(this));
        }

        /// <summary>
        /// Gets or sets number of occurrences per unit time.
        /// </summary>
        public Int32 Occurrences
        {
            get { return _occurrences; }
            set
            {
                checkIfNotTooLateToConfigure();

                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(Occurrences),
                        "Number of occurrences must be a positive integer");
                }

                _occurrences = value;
            }
        }

        /// <summary>
        /// Gets or sets throttling time interval.
        /// </summary>
        public TimeSpan TimeUnit
        {
            get { return _timeUnit; }
            set
            {
                checkIfNotTooLateToConfigure();

                if (value != value.Duration())
                {
                    throw new ArgumentOutOfRangeException(nameof(TimeUnit),
                        "Time unit must be a positive span of time");
                }
                if (value >= TimeSpan.FromMilliseconds(uint.MaxValue))
                {
                    throw new ArgumentOutOfRangeException(nameof(TimeUnit),
                        "Time unit must be less than 2^32 milliseconds");
                }
                if (value.TotalMilliseconds < 1)
                {
                    throw new ArgumentException(nameof(TimeUnit));
                }

                _timeUnit = value;
            }
        }

        /// <summary>
        /// Gets or sets maximal number of retry attempts for single request.
        /// </summary>
        public Int32 MaxRetryAttempts
        {
            get { return _maxRetryAttempts; }
            set
            {
                checkIfNotTooLateToConfigure();

                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(MaxRetryAttempts),
                        "Number of maximal retry attempts must be a positive integer");
                }

                _maxRetryAttempts = value;
            }
        }

        /// <summary>
        /// Gets or sets list of Http status codes which when received should initiate a retry of the affected request
        /// </summary>
        public IEnumerable<Int32> RetryHttpStatuses
        {
            get { return _retryHttpStatuses; }
            set
            {
                checkIfNotTooLateToConfigure();
                _retryHttpStatuses = new HashSet<Int32>(
                    value ?? Enumerable.Empty<Int32>());
            }
        }

        internal IThrottler GetThrottler()
        {
            return _rateThrottler.Value;
        }

        /// <summary>
        /// Gets throttle parameters initialized with default values or from configuration file.
        /// </summary>
        public static ThrottleParameters Default { get; } = new ThrottleParameters();

        private void checkIfNotTooLateToConfigure(
            [System.Runtime.CompilerServices.CallerMemberName] String propertyName = null)
        {
            if (_rateThrottler != null &&
                _rateThrottler.IsValueCreated)
            {
                throw new InvalidOperationException(
                    $"Unable to change {propertyName} value - throttler already created.");
            }
        }
    }
}

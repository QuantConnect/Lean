/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

using System;
using System.Collections;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// This enumerator will update the <see cref="SubscriptionDataConfig.PriceScaleFactor"/> when required
    /// and adjust the raw <see cref="BaseData"/> prices based on the provided <see cref="SubscriptionDataConfig"/>.
    /// Assumes the prices of the provided <see cref="IEnumerator"/> are in raw mode.
    /// </summary>
    public class PriceScaleFactorEnumerator : IEnumerator<BaseData>
    {
        private readonly IEnumerator<BaseData> _rawDataEnumerator;
        private readonly SubscriptionDataConfig _config;
        private readonly Lazy<FactorFile> _factorFile;
        private DateTime _lastTradableDate;

        /// <summary>
        /// Explicit interface implementation for <see cref="Current"/>
        /// </summary>
        object IEnumerator.Current => Current;

        /// <summary>
        /// Last read <see cref="BaseData"/> object from this type and source
        /// </summary>
        public BaseData Current
        {
            get;
            private set;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="PriceScaleFactorEnumerator"/>.
        /// </summary>
        /// <param name="rawDataEnumerator">The underlying raw data enumerator</param>
        /// <param name="config">The <see cref="SubscriptionDataConfig"/> to enumerate for.
        /// Will determine the <see cref="DataNormalizationMode"/> to use.</param>
        /// <param name="factorFile">The <see cref="FactorFile"/> instance to use</param>
        public PriceScaleFactorEnumerator(
            IEnumerator<BaseData> rawDataEnumerator,
            SubscriptionDataConfig config,
            Lazy<FactorFile> factorFile)
        {
            _lastTradableDate = DateTime.MinValue;
            _config = config;
            _rawDataEnumerator = rawDataEnumerator;
            _factorFile = factorFile;
        }

        /// <summary>
        /// Dispose of the underlying enumerator.
        /// </summary>
        public void Dispose()
        {
            _rawDataEnumerator.Dispose();
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// True if the enumerator was successfully advanced to the next element;
        /// False if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            var underlyingReturnValue = _rawDataEnumerator.MoveNext();
            Current = _rawDataEnumerator.Current;

            if (underlyingReturnValue
                && Current != null
                && _factorFile != null
                && _config.DataNormalizationMode != DataNormalizationMode.Raw)
            {
                if (Current.Time.Date > _lastTradableDate)
                {
                    _lastTradableDate = Current.Time.Date;
                    UpdateScaleFactor(_lastTradableDate);
                }

                Current = Current.Normalize(_config);
            }

            return underlyingReturnValue;
        }

        /// <summary>
        /// Reset the IEnumeration
        /// </summary>
        /// <remarks>Not used</remarks>
        public void Reset()
        {
            throw new NotImplementedException("Reset method not implemented. Assumes loop will only be used once.");
        }

        private void UpdateScaleFactor(DateTime date)
        {
            switch (_config.DataNormalizationMode)
            {
                case DataNormalizationMode.Raw:
                    return;

                case DataNormalizationMode.TotalReturn:
                case DataNormalizationMode.SplitAdjusted:
                    _config.PriceScaleFactor = _factorFile.Value.GetSplitFactor(date);
                    break;

                case DataNormalizationMode.Adjusted:
                    _config.PriceScaleFactor = _factorFile.Value.GetPriceScaleFactor(date);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

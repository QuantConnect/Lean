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
using QuantConnect.Interfaces;

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
        private readonly IFactorFileProvider _factorFileProvider;
        private DateTime _nextTradableDate;
        private IFactorProvider _factorFile;
        private bool _liveMode;
        private DateTime? _endDate;

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
        /// <param name="factorFileProvider">The <see cref="IFactorFileProvider"/> instance to use</param>
        /// <param name="liveMode">True, is this is a live mode data stream</param>
        /// <param name="endDate">The enumerator end date</param>
        public PriceScaleFactorEnumerator(
            IEnumerator<BaseData> rawDataEnumerator,
            SubscriptionDataConfig config,
            IFactorFileProvider factorFileProvider,
            bool liveMode = false,
            DateTime? endDate = null)
        {
            _config = config;
            _liveMode = liveMode;
            _nextTradableDate = DateTime.MinValue;
            _rawDataEnumerator = rawDataEnumerator;
            _factorFileProvider = factorFileProvider;
            _endDate = endDate;
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
                && _factorFileProvider != null
                && _config.DataNormalizationMode != DataNormalizationMode.Raw)
            {
                var priceScaleFrontier = Current.GetUpdatePriceScaleFrontier();
                if (priceScaleFrontier >= _nextTradableDate)
                {
                    _factorFile = _factorFileProvider.Get(_config.Symbol);
                    _config.PriceScaleFactor = _factorFile.GetPriceScale(priceScaleFrontier.Date, _config.DataNormalizationMode, _config.ContractDepthOffset, _config.DataMappingMode, _endDate);

                    // update factor files every day
                    _nextTradableDate = priceScaleFrontier.Date.AddDays(1);
                    if (_liveMode)
                    {
                        // in live trading we add a offset to make sure new factor files are available
                        _nextTradableDate = _nextTradableDate.Add(Time.LiveAuxiliaryDataOffset);
                    }
                }

                Current = Current.Normalize(_config.PriceScaleFactor, _config.DataNormalizationMode, _config.SumOfDividends);
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
    }
}

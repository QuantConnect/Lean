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
*/

using System;
using System.IO;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Coarse base fundamental data provider
    /// </summary>
    public class CoarseFundamentalDataProvider : IFundamentalDataProvider
    {
        private DateTime _date;
        private readonly Dictionary<SecurityIdentifier, CoarseFundamental> _coarseFundamental = new();

        private readonly CoarseFundamental _factory = new();
        private IDataProvider _dataProvider;

        /// <summary>
        /// Initializes the service
        /// </summary>
        /// <param name="dataProvider">The data provider instance to use</param>
        /// <param name="liveMode">True if running in live mode</param>
        public void Initialize(IDataProvider dataProvider, bool liveMode)
        {
            _dataProvider = dataProvider;
        }

        /// <summary>
        /// Will fetch the requested fundamental information for the requested time and symbol
        /// </summary>
        /// <typeparam name="T">The expected data type</typeparam>
        /// <param name="time">The time to request this data for</param>
        /// <param name="securityIdentifier">The security identifier</param>
        /// <param name="name">The name of the fundamental property</param>
        /// <returns>The fundamental information</returns>
        public T Get<T>(DateTime time, SecurityIdentifier securityIdentifier, string name)
        {
            lock(_coarseFundamental)
            {
                if (time == _date)
                {
                    return GetProperty<T>(securityIdentifier, name);
                }
                _date = time;

                var config = new SubscriptionDataConfig(typeof(CoarseFundamental), new Symbol(securityIdentifier, securityIdentifier.Symbol), Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, false, false, false);
                var source = _factory.GetSource(config, time, false);
                var fileStream = _dataProvider.Fetch(source.Source);

                if (fileStream == null)
                {
                    return default;
                }
                _coarseFundamental.Clear();
                using (var reader = new StreamReader(fileStream))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var coarse = _factory.Reader(config, line, time, false) as CoarseFundamental;
                        if (coarse != null)
                        {
                            _coarseFundamental[coarse.Symbol.ID] = coarse;
                        }
                    }
                }

                return GetProperty<T>(securityIdentifier, name);
            }
        }

        private dynamic GetProperty<T>(SecurityIdentifier securityIdentifier, string property)
        {
            if (!_coarseFundamental.TryGetValue(securityIdentifier, out var coarse))
            {
                return default(T);
            }

            switch (property)
            {
                case nameof(CoarseFundamental.Price):
                    return coarse.Price;
                case nameof(CoarseFundamental.Value):
                    return coarse.Value;
                case nameof(CoarseFundamental.Market):
                    return coarse.Market;
                case nameof(CoarseFundamental.Volume):
                    return coarse.Volume;
                case nameof(CoarseFundamental.PriceFactor):
                    return coarse.PriceFactor;
                case nameof(CoarseFundamental.SplitFactor):
                    return coarse.SplitFactor;
                case nameof(CoarseFundamental.DollarVolume):
                    return coarse.DollarVolume;
                case nameof(CoarseFundamental.HasFundamentalData):
                    return false;
            }

            return default(T);
        }
    }
}

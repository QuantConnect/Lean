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
using System.Collections.Generic;
using QuantConnect.Data.Fundamental;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Coarse base fundamental data provider
    /// </summary>
    public class CoarseFundamentalDataProvider : BaseFundamentalDataProvider
    {
        private DateTime _date;
        private readonly Dictionary<SecurityIdentifier, CoarseFundamental> _coarseFundamental = new();

        /// <summary>
        /// Will fetch the requested fundamental information for the requested time and symbol
        /// </summary>
        /// <typeparam name="T">The expected data type</typeparam>
        /// <param name="time">The time to request this data for</param>
        /// <param name="securityIdentifier">The security identifier</param>
        /// <param name="enumName">The name of the fundamental property</param>
        /// <returns>The fundamental information</returns>
        public override T Get<T>(DateTime time, SecurityIdentifier securityIdentifier, FundamentalProperty enumName)
        {
            var name = Enum.GetName(enumName);
            lock (_coarseFundamental)
            {
                if (time == _date)
                {
                    return GetProperty<T>(securityIdentifier, name);
                }
                _date = time;

                var path = Path.Combine(Globals.DataFolder, "equity", "usa", "fundamental", "coarse", $"{time:yyyyMMdd}.csv");
                var fileStream = DataProvider.Fetch(path);
                if (fileStream == null)
                {
                    return GetDefault<T>();
                }

                _coarseFundamental.Clear();
                using (var reader = new StreamReader(fileStream))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var coarse = Read(line, time);
                        if (coarse != null)
                        {
                            _coarseFundamental[coarse.Symbol.ID] = coarse;
                        }
                    }
                }

                return GetProperty<T>(securityIdentifier, name);
            }
        }

        public static CoarseFundamentalSource Read(string line, DateTime date)
        {
            try
            {
                var csv = line.Split(',');
                var coarse = new CoarseFundamentalSource
                {
                    Symbol = new Symbol(SecurityIdentifier.Parse(csv[0]), csv[1]),
                    Time = date,
                    Value = csv[2].ToDecimal(),
                    VolumeSetter = csv[3].ToInt64(),
                    DollarVolumeSetter = (double)csv[4].ToDecimal()
                };

                if (csv.Length > 5)
                {
                    coarse.HasFundamentalDataSetter = csv[5].ConvertInvariant<bool>();
                }

                if (csv.Length > 7)
                {
                    coarse.PriceFactorSetter = csv[6].ToDecimal();
                    coarse.SplitFactorSetter = csv[7].ToDecimal();
                }

                return coarse;
            }
            catch (Exception)
            {
                return null;
            }

        }

        private dynamic GetProperty<T>(SecurityIdentifier securityIdentifier, string property)
        {
            if (!_coarseFundamental.TryGetValue(securityIdentifier, out var coarse))
            {
                return GetDefault<T>();
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

            return GetDefault<T>();
        }

        /// <summary>
        /// Coarse fundamental with setters
        /// </summary>
        public class CoarseFundamentalSource : CoarseFundamental
        {
            public long VolumeSetter;
            public double DollarVolumeSetter;
            public decimal PriceFactorSetter = 1;
            public decimal SplitFactorSetter = 1;
            public bool HasFundamentalDataSetter;

            /// <summary>
            /// Gets the day's dollar volume for this symbol
            /// </summary>
            public override double DollarVolume => DollarVolumeSetter;

            /// <summary>
            /// Gets the day's total volume
            /// </summary>
            public override long Volume => VolumeSetter;

            /// <summary>
            /// Returns whether the symbol has fundamental data for the given date
            /// </summary>
            public override bool HasFundamentalData => HasFundamentalDataSetter;

            /// <summary>
            /// Gets the price factor for the given date
            /// </summary>
            public override decimal PriceFactor => PriceFactorSetter;

            /// <summary>
            /// Gets the split factor for the given date
            /// </summary>
            public override decimal SplitFactor => SplitFactorSetter;
        }
    }
}

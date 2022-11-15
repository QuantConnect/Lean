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
using System.Linq;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Class implements Fed's US primary credit rate as risk free rate, implementing <see cref="IQLRiskFreeRateEstimator"/>.
    /// </summary>
    /// <remarks>
    /// Board of Governors of the Federal Reserve System (US), Primary Credit Rate - Historical Dates of Changes and Rates for Federal Reserve District 8: St. Louis [PCREDIT8]
    /// retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/PCREDIT8
    /// </remarks>
    public class FedRateQLRiskFreeRateEstimator : IQLRiskFreeRateEstimator
    {
        private InterestRateProvider[] _riskFreeRateProvider;
        private int _riskFreeRateIndex;
        private static readonly object DataFolderSymbolLock = new object();

        private IDataProvider _dataProvider =
            Composer.Instance.GetExportedValueByTypeName<IDataProvider>(Config.Get("data-provider",
                "DefaultDataProvider"));

        /// <summary>
        /// Constructor initializes class
        /// </summary>
        public FedRateQLRiskFreeRateEstimator()
        {
        }

        /// <summary>
        /// Returns current flat estimate of the risk free rate
        /// </summary>
        /// <param name="security">The option security object</param>
        /// <param name="slice">The current data slice. This can be used to access other information
        /// available to the algorithm</param>
        /// <param name="contract">The option contract to evaluate</param>
        /// <returns>The estimate</returns>
        public double Estimate(Security security, Slice slice, OptionContract contract)
        {
            if (slice == null)
                return 0d;

            if (_riskFreeRateProvider == null)
            {
                _riskFreeRateProvider = LoadInterestRateProvider();
                _riskFreeRateIndex = 0;
            }

            var date = slice.Time.Date;

            while (_riskFreeRateIndex + 1 < _riskFreeRateProvider.Length &&
                _riskFreeRateProvider[_riskFreeRateIndex + 1].Date <= date)
            {
                _riskFreeRateIndex++;
            }

            return _riskFreeRateProvider[_riskFreeRateIndex].InterestRate;
        }

        /// <summary>
        /// Gets the sorted list of historical US primary credit rate
        /// data found in /option/usa/interest-rate-csv
        /// </summary>
        /// <returns>Sorted list of historical primary credit rate</returns>
        protected InterestRateProvider[] LoadInterestRateProvider(string subDirectory = "option/usa", string fileName = "interest-rate.csv")
        {
            var directory = Path.Combine(Globals.DataFolder,
                                        subDirectory,
                                        fileName);
            return FromCsvFile(directory);
        }

        /// <summary>
        /// Reads Fed primary credit rate file and returns a sorted list of historical margin changes
        /// </summary>
        /// <param name="file">The csv file to be read</param>
        /// <returns>Sorted list of historical margin changes</returns>
        private InterestRateProvider[] FromCsvFile(string file)
        {
            lock (DataFolderSymbolLock)
            {
                // skip the first header line, also skip #'s as these are comment lines
                var interestRateProvider = _dataProvider.ReadLines(file)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Skip(1)
                    .Select(InterestRateProvider.Create)
                    .OrderBy(x => x.Date)
                    .ToArray();

                if(interestRateProvider.Length == 0)
                {
                    Log.Trace($"Unable to locate FED primary credit rate file. Defaulting to 1%. File: {file}");

                    return new[] {
                        new InterestRateProvider
                        {
                            Date = DateTime.MinValue,
                            InterestRate = 0.01d
                        }
                    };
                }
                return interestRateProvider;
            }
        }
    }
}

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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;

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
        private Dictionary<DateTime, decimal> _riskFreeRateProvider;

        /// <summary>
        /// Constructor initializes class
        /// </summary>
        public FedRateQLRiskFreeRateEstimator()
        {
            LoadInterestRateProvider();
        }

        /// <summary>
        /// Constructor initializes class
        /// </summary>
        public Dictionary<DateTime, decimal> GetRiskFreeRateCollection()
        {
            return _riskFreeRateProvider;
        }

        /// <summary>
        /// Returns current flat estimate of the risk free rate
        /// </summary>
        /// <param name="security">The option security object</param>
        /// <param name="slice">The current data slice. This can be used to access other information
        /// available to the algorithm</param>
        /// <param name="contract">The option contract to evaluate</param>
        /// <returns>The estimate</returns>
        public decimal Estimate(Security security, Slice slice, OptionContract contract)
        {
            if (slice == null)
                return 0.01m;
            
            var date = slice.Time.Date;

            while (!_riskFreeRateProvider.ContainsKey(date))
            {
                date = date.AddDays(-1);
            }
            return _riskFreeRateProvider[date];
        }

        /// <summary>
        /// Generate the daily historical US primary credit rate
        /// data found in /option/usa/interest-rate-csv
        /// </summary>
        protected void LoadInterestRateProvider(DateTime? endDate = null, string subDirectory = "option/usa", string fileName = "interest-rate.csv")
        {
            endDate = (DateTime)(endDate ?? (DateTime?)DateTime.Today);
            var directory = Path.Combine(Globals.DataFolder,
                                        subDirectory,
                                        fileName);

            _riskFreeRateProvider = InterestRateProvider.FromCsvFile(directory);
            var firstDate = _riskFreeRateProvider.Keys.OrderBy(x => x).First();

            // Sparse the discrete datapoints into continuous credit rate data for every day
            var currentRate = _riskFreeRateProvider[firstDate];
            for (DateTime date = firstDate.AddDays(1); date <= endDate; date = date.AddDays(1))
            {
                // Skip Saturday and Sunday (non-trading day)
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    continue;
                }

                if (_riskFreeRateProvider.ContainsKey(date))
                {
                    currentRate = _riskFreeRateProvider[date];
                }
                else
                {
                    _riskFreeRateProvider[date] = currentRate;
                }
            }
        }
    }
}

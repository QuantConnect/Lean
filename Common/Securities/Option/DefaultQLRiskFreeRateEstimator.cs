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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Util;
using QLNet;

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Class implements default flat risk free curve, implementing <see cref="IQLRiskFreeRateEstimator"/>.
    /// </summary>
    public class DefaultQLRiskFreeRateEstimator : IQLRiskFreeRateEstimator
    {
        private readonly decimal _riskFreeRate;
        /// <summary>
        /// Constructor initializes class with risk free rate constant
        /// </summary>
        /// <param name="riskFreeRate"></param>
        public DefaultQLRiskFreeRateEstimator(decimal riskFreeRate = 0.01m)
        {
            _riskFreeRate = riskFreeRate;
        }

        /// <summary>
        /// Returns current flat estimate of the risk free rate
        /// </summary>
        /// <param name="security">The option security object</param>
        /// <param name="slice">The current data slice. This can be used to access other information
        /// available to the algorithm</param>
        /// <param name="contract">The option contract to evaluate</param>
        /// <returns>The estimate</returns>
        public Handle<YieldTermStructure> Estimate(Security security, Slice slice, OptionContract contract)
        {
            var dayCounter = new Actual365Fixed();
            var settlementDate = contract.Time.Date.AddDays(Option.DefaultSettlementDays);

            return new Handle<YieldTermStructure>(
                        new FlatForward(settlementDate, (double)_riskFreeRate, dayCounter));
        }
    }
}

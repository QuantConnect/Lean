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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to test if expired options contracts chains are making their
    /// way into the timeslices being delivered to OnData()
    /// </summary>
    public class OptionsExpiredContractRegression : QCAlgorithm
    {
        /// <summary>
        /// Initializes the algorithm state.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2015, 12, 23);
            SetEndDate(2016, 1, 20);
            SetCash(1000000);

            // Subscribe to GOOG Options
            var option = AddOption("GOOG");
            option.SetFilter(x => x.CallsOnly().Strikes(0, 1).Expiration(0, 30));
        }

        public override void OnData(Slice data)
        {
            foreach (var chain in data.OptionChains)
            {
                foreach (var contract in chain.Value.OrderBy(x => x.Expiry))
                {
                    if (contract.Expiry < Time)
                    {
                        throw new Exception($"Received expired contract {contract} expired: {contract.Expiry} current time: {Time}");
                    }
                }
            }
        }
    }
}
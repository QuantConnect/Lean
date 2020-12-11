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

using QuantConnect.Algorithm;
using QuantConnect.Orders;
using QuantConnect.Securities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace QuantConnect.Report
{
    /// <summary>
    /// Fake algorithm that initializes portfolio and algorithm securities. Never ran.
    /// </summary>
    public class PortfolioLooperAlgorithm : QCAlgorithm
    {
        private decimal _startingCash;
        private List<Order> _orders;

        public PortfolioLooperAlgorithm(decimal startingCash, IEnumerable<Order> orders) : base()
        {
            _startingCash = startingCash;
            _orders = orders.ToList();
        }

        public void FromOrders(IEnumerable<Order> orders)
        {
            foreach (var symbol in orders.Select(x => x.Symbol).Distinct())
            {
                Resolution resolution;
                switch (symbol.SecurityType)
                {
                    case SecurityType.Option:
                    case SecurityType.Future:
                        resolution = Resolution.Minute;
                        break;
                    default:
                        resolution = Resolution.Daily;
                        break;
                }

                var configs = SubscriptionManager.SubscriptionDataConfigService.Add(symbol, resolution, false, false);
                var security = Securities.CreateSecurity(symbol, configs, 0m);
                if (symbol.SecurityType == SecurityType.Crypto)
                {
                    security.BuyingPowerModel = new SecurityMarginModel();
                }

                // Set leverage to 10000 to account for unknown leverage values in user algorithms
                security.SetLeverage(10000m);

                var method = typeof(QCAlgorithm).GetMethod("AddToUserDefinedUniverse", BindingFlags.NonPublic | BindingFlags.Instance);
                method.Invoke(this, new object[] { security, configs });
            }
        }

        public override void Initialize()
        {
            SetCash(_startingCash);

            if (_orders.Count != 0)
            {
                SetStartDate(_orders.First().Time);
                SetEndDate(_orders.Last().Time);
            }

            SetBenchmark(b => 0);
        }
    }
}

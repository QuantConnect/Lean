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

using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstrate the usage of the BrokerageModel property to help improve backtesting
    /// accuracy through simulation of a specific brokerage's rules around restrictions
    /// on submitting orders as well as fee structure.
    /// </summary>
    /// <meta name="tag" content="trading and orders" />
    /// <meta name="tag" content="brokerage models" />
    public class BrokerageModelAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialize the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must be initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecurityType.Equity, "SPY", Resolution.Second);

            // there's two ways to set your brokerage model. The easiest would be to call
            // SetBrokerageModel( BrokerageName ); // BrokerageName is an enum
            //SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage);
            //SetBrokerageModel(BrokerageName.Default);

            // the other way is to call SetBrokerageModel( IBrokerageModel ) with your
            // own custom model. I've defined a simple extension to the default brokerage
            // model to take into account a requirement to maintain 500 cash in the account
            // at all times

            SetBrokerageModel(new MinimumAccountBalanceBrokerageModel(this, 500.00m));
        }

        private decimal _last = 1.0m;

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {
            if (!Portfolio.Invested)
            {
                //fails first several times, we'll keep decrementing until it succeeds
                SetHoldings("SPY", _last);
                if (Portfolio["SPY"].Quantity == 0)
                {
                    // each time we fail to purchase we'll decrease our set holdings percentage
                    Debug(Time + " - Failed to purchase stock");
                    _last *= 0.95m;
                }
                else
                {
                    Debug(Time + " - Purchased Stock @ SetHoldings( " + _last + " )");
                }
            }
        }

        /// <summary>
        /// Custom brokerage model that requires clients to maintain a minimum cash balance
        /// </summary>
        class MinimumAccountBalanceBrokerageModel : DefaultBrokerageModel
        {
            private readonly QCAlgorithm _algorithm;
            private readonly decimal _minimumAccountBalance;

            public MinimumAccountBalanceBrokerageModel(QCAlgorithm algorithm, decimal minimumAccountBalance)
            {
                _algorithm = algorithm;
                _minimumAccountBalance = minimumAccountBalance;
            }

            /// <summary>
            /// Prevent orders which would bring the account below a minimum cash balance
            /// </summary>
            public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
            {
                message = null;

                // we want to model brokerage requirement of _minimumAccountBalance cash value in account

                var orderCost = order.GetValue(security);
                var cash = _algorithm.Portfolio.Cash;
                var cashAfterOrder = cash - orderCost;
                if (cashAfterOrder < _minimumAccountBalance)
                {
                    // return a message describing why we're not allowing this order
                    message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "InsufficientRemainingCapital",
                        $"Account must maintain a minimum of ${_minimumAccountBalance.ToStringInvariant()} USD at all times. " +
                        $"Order ID: {order.Id.ToStringInvariant()}"
                    );
                    return false;
                }
                return true;
            }
        }
    }
}
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
using System.Linq;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Base class for equity option strategy regression algorithms which holds some basic shared setup logic
    /// </summary>
    public abstract class OptionEquityBaseStrategyRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        protected decimal _paidFees;
        protected Symbol _optionSymbol;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(200000);

            var equity = AddEquity("GOOG", leverage: 4);
            var option = AddOption(equity.Symbol);
            _optionSymbol = option.Symbol;

            // set our strike/expiry filter for this option chain
            option.SetFilter(u => u.Strikes(-2, +2)
                                   // Expiration method accepts TimeSpan objects or integer for days.
                                   // The following statements yield the same filtering criteria
                                   .Expiration(0, 180));
        }

        protected void AssertOptionStrategyIsPresent(string name, int? quantity = null)
        {
            if (Portfolio.Positions.Groups.Where(group => group.BuyingPowerModel is OptionStrategyPositionGroupBuyingPowerModel)
                .Count(group => ((OptionStrategyPositionGroupBuyingPowerModel)@group.BuyingPowerModel).ToString() == name
                    && (!quantity.HasValue || Math.Abs(group.Quantity) == quantity)) != 1)
            {
                throw new Exception($"Option strategy: '{name}' was not found!");
            }
        }

        protected void AssertDefaultGroup(Symbol symbol, decimal quantity)
        {
            if (Portfolio.Positions.Groups.Where(group => group.BuyingPowerModel is SecurityPositionGroupBuyingPowerModel)
                .Count(group => group.Positions.Any(position => position.Symbol == symbol && position.Quantity == quantity)) != 1)
            {
                throw new Exception($"Default groupd for symbol '{symbol}' and quantity '{quantity}' was not found!");
            }
        }

        protected decimal GetPriceSpreadDifference(params Symbol[] symbols)
        {
            var spreadPaid = 0m;
            foreach (var symbol in symbols)
            {
                var security = Securities[symbol];
                var actualQuantity = security.Holdings.AbsoluteQuantity;
                var spread = 0m;
                if (security.Holdings.IsLong)
                {
                    if (security.AskPrice != 0)
                    {
                        spread = security.Price - security.AskPrice;
                    }
                }
                else if(security.BidPrice != 0)
                {
                    spread = security.BidPrice - security.Price;
                }
                spreadPaid += spread * actualQuantity * security.SymbolProperties.ContractMultiplier;
            }

            return spreadPaid;
        }

        /// <summary>
        /// Order fill event handler. On an order fill update the resulting information is passed to this method.
        /// </summary>
        /// <param name="orderEvent">Order event details containing details of the events</param>
        /// <remarks>This method can be called asynchronously and so should only be used by seasoned C# experts. Ensure you use proper locks on thread-unsafe objects</remarks>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                _paidFees += orderEvent.OrderFee.Value.Amount;
                if (orderEvent.Symbol.SecurityType.IsOption())
                {
                    var security = Securities[orderEvent.Symbol];
                    var premiumPaid = orderEvent.Quantity * orderEvent.FillPrice * security.SymbolProperties.ContractMultiplier;
                    Log($"{orderEvent}. Premium paid: {premiumPaid}");
                    return;
                }
            }
            Log($"{orderEvent}");
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 0;

        /// </summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public abstract Dictionary<string, string> ExpectedStatistics { get; }
    }
}

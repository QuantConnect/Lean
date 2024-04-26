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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template algorithm that implements a fill model with combo orders
    /// </summary>
    public class ComboOrdersFillModelAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Security _spy;
        private Security _ibm;
        private Dictionary<OrderType, int> _orderTypes;

        public override void Initialize()
        {
            SetStartDate(2019, 1, 1);
            SetEndDate(2019, 1, 20);

            _orderTypes = new Dictionary<OrderType, int>();
            _spy = AddEquity("SPY", Resolution.Hour);
            _ibm = AddEquity("IBM", Resolution.Hour);

            _spy.SetFillModel(new CustomPartialFillModel());
            _ibm.SetFillModel(new CustomPartialFillModel());
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                var legs = new List<Leg>() { Leg.Create(_spy.Symbol, 1), Leg.Create(_ibm.Symbol, -1)};
                ComboMarketOrder(legs, 100);
                ComboLimitOrder(legs, 100, Math.Round(_spy.BidPrice));

                legs = new List<Leg>() { Leg.Create(_spy.Symbol, 1, Math.Round(_spy.BidPrice) + 1), Leg.Create(_ibm.Symbol, -1, Math.Round(_ibm.BidPrice) + 1) };
                ComboLegLimitOrder(legs, 100);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if(orderEvent.Status == OrderStatus.Filled)
            {
                var orderType = Transactions.GetOrderById(orderEvent.OrderId).Type;
                if (orderType == OrderType.ComboMarket && orderEvent.AbsoluteFillQuantity != 50)
                {
                    throw new Exception($"The absolute quantity filled for all combo market orders should be 50, but for order {orderEvent.OrderId} was {orderEvent.AbsoluteFillQuantity}");
                }

                if (orderType == OrderType.ComboLimit && orderEvent.AbsoluteFillQuantity != 20)
                {
                    throw new Exception($"The absolute quantity filled for all combo limit orders should be 20, but for order {orderEvent.OrderId} was {orderEvent.AbsoluteFillQuantity}");
                }

                if (orderType == OrderType.ComboLegLimit && orderEvent.AbsoluteFillQuantity != 10)
                {
                    throw new Exception($"The absolute quantity filled for all combo leg limit orders should be 20, but for order {orderEvent.OrderId} was {orderEvent.AbsoluteFillQuantity}");
                }

                _orderTypes[orderType] = 1;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_orderTypes.Keys.Count != 3)
            {
                throw new Exception($"Just 3 different types of order were submitted in this algorithm, but the amount of order types was {_orderTypes.Count}");
            }

            if (!_orderTypes.Keys.Contains(OrderType.ComboMarket))
            {
                throw new Exception($"One Combo Market Order should have been submitted but it was not");
            }

            if (!_orderTypes.Keys.Contains(OrderType.ComboLimit))
            {
                throw new Exception($"One Combo Limit Order should have been submitted but it was not");
            }

            if (!_orderTypes.Keys.Contains(OrderType.ComboLegLimit))
            {
                throw new Exception($"One Combo Leg Limit Order should have been submitted but it was not");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 281;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "6"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "162.471%"},
            {"Drawdown", "1.800%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "104781.43"},
            {"Net Profit", "4.781%"},
            {"Sharpe Ratio", "8.272"},
            {"Sortino Ratio", "6.986"},
            {"Probabilistic Sharpe Ratio", "87.028%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.049"},
            {"Beta", "0.646"},
            {"Annual Standard Deviation", "0.119"},
            {"Annual Variance", "0.014"},
            {"Information Ratio", "-8.927"},
            {"Tracking Error", "0.069"},
            {"Treynor Ratio", "1.519"},
            {"Total Fees", "$6.00"},
            {"Estimated Strategy Capacity", "$250000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "9.81%"},
            {"OrderListHash", "397d4e81b8c7fa9258e18c4bcf4154e1"}
        };
    }

    public class CustomPartialFillModel : FillModel
    {
        private readonly Dictionary<int, decimal> _absoluteRemainingByOrderId;

        public CustomPartialFillModel()
        {
            _absoluteRemainingByOrderId = new Dictionary<int, decimal>();
        }

        private List<OrderEvent> FillOrdersPartially(FillModelParameters parameters, List<OrderEvent> fills, int quantity)
        {
            var partialFills = new List<OrderEvent>();
            if (fills.Count == 0)
            {
                return partialFills;
            }

            foreach (var kvp in parameters.SecuritiesForOrders.OrderBy(kvp => kvp.Key.Id))
            {
                var order = kvp.Key;
                var fill = fills.Find(x => x.OrderId == order.Id);

                decimal absoluteRemaining;
                if (!_absoluteRemainingByOrderId.TryGetValue(order.Id, out absoluteRemaining))
                {
                    absoluteRemaining = order.AbsoluteQuantity;
                }

                // Set the fill amount
                fill.FillQuantity = Math.Sign(order.Quantity) * quantity;
                if (Math.Min(Math.Abs(fill.FillQuantity), absoluteRemaining) == absoluteRemaining)
                {
                    fill.FillQuantity = Math.Sign(order.Quantity) * absoluteRemaining;
                    fill.Status = OrderStatus.Filled;
                    _absoluteRemainingByOrderId.Remove(order.Id);
                }
                else
                {
                    fill.Status = OrderStatus.PartiallyFilled;
                    _absoluteRemainingByOrderId[order.Id] = absoluteRemaining - Math.Abs(fill.FillQuantity);
                    var price = fill.FillPrice;
                    //_algorithm.Debug($"{_algorithm.Time} - Partial Fill - Remaining {_absoluteRemainingByOrderId[order.Id]} Price - {price}");
                }

                partialFills.Add(fill);
            }

            return partialFills;
        }

        public override List<OrderEvent> ComboMarketFill(Order order, FillModelParameters parameters)
        {
            var fills = base.ComboMarketFill(order, parameters);
            var partialFills = FillOrdersPartially(parameters, fills, 50);
            return partialFills;
        }

        public override List<OrderEvent> ComboLimitFill(Order order, FillModelParameters parameters)
        {
            var fills = base.ComboLimitFill(order, parameters);
            var partialFills = FillOrdersPartially(parameters, fills, 20);
            return partialFills;
        }

        public override List<OrderEvent> ComboLegLimitFill(Order order, FillModelParameters parameters)
        {
            var fills = base.ComboLegLimitFill(order, parameters);
            var partialFills = FillOrdersPartially(parameters, fills, 10);
            return partialFills;
        }
    }
}

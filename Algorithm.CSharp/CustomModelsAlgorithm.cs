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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of using custom fee, slippage, fill, and buying power models for modelling transactions in backtesting.
    /// QuantConnect allows you to model all orders as deeply and accurately as you need.
    /// </summary>
    /// <meta name="tag" content="trading and orders" />
    /// <meta name="tag" content="transaction fees and slippage" />
    /// <meta name="tag" content="custom buying power models" />
    /// <meta name="tag" content="custom transaction models" />
    /// <meta name="tag" content="custom slippage models" />
    /// <meta name="tag" content="custom fee models" />
    public class CustomModelsAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Security _security;
        private Symbol _spy;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 01);
            SetEndDate(2013, 10, 31);
            _security = AddEquity("SPY", Resolution.Hour);
            _spy = _security.Symbol;

            // set our models
            _security.SetFeeModel(new CustomFeeModel(this));
            _security.SetFillModel(new CustomFillModel(this));
            _security.SetSlippageModel(new CustomSlippageModel(this));
            _security.SetBuyingPowerModel(new CustomBuyingPowerModel(this));
        }

        public override void OnData(Slice data)
        {
            var openOrders = Transactions.GetOpenOrders(_spy);
            if (openOrders.Count != 0) return;

            if (Time.Day > 10 && _security.Holdings.Quantity <= 0)
            {
                var quantity = CalculateOrderQuantity(_spy, .5m);
                Log($"MarketOrder: {quantity}");
                MarketOrder(_spy, quantity, asynchronous: true); // async needed for partial fill market orders
            }
            else if (Time.Day > 20 && _security.Holdings.Quantity >= 0)
            {
                var quantity = CalculateOrderQuantity(_spy, -.5m);
                Log($"MarketOrder: {quantity}");
                MarketOrder(_spy, quantity, asynchronous: true); // async needed for partial fill market orders
            }
        }

        public class CustomFillModel : ImmediateFillModel
        {
            private readonly QCAlgorithm _algorithm;
            private readonly Random _random = new Random(387510346); // seed it for reproducibility
            private readonly Dictionary<long, decimal> _absoluteRemainingByOrderId = new Dictionary<long, decimal>();

            public CustomFillModel(QCAlgorithm algorithm)
            {
                _algorithm = algorithm;
            }

            public override OrderEvent MarketFill(Security asset, MarketOrder order)
            {
                // this model randomly fills market orders

                decimal absoluteRemaining;
                if (!_absoluteRemainingByOrderId.TryGetValue(order.Id, out absoluteRemaining))
                {
                    absoluteRemaining = order.AbsoluteQuantity;
                    _absoluteRemainingByOrderId.Add(order.Id, order.AbsoluteQuantity);
                }

                var fill = base.MarketFill(asset, order);
                var absoluteFillQuantity = (int) (Math.Min(absoluteRemaining, _random.Next(0, 2*(int)order.AbsoluteQuantity)));
                fill.FillQuantity = Math.Sign(order.Quantity) * absoluteFillQuantity;

                if (absoluteRemaining == absoluteFillQuantity)
                {
                    fill.Status = OrderStatus.Filled;
                    _absoluteRemainingByOrderId.Remove(order.Id);
                }
                else
                {
                    absoluteRemaining = absoluteRemaining - absoluteFillQuantity;
                    _absoluteRemainingByOrderId[order.Id] = absoluteRemaining;
                    fill.Status = OrderStatus.PartiallyFilled;
                }

                _algorithm.Log($"CustomFillModel: {fill}");

                return fill;
            }
        }

        public class CustomFeeModel : FeeModel
        {
            private readonly QCAlgorithm _algorithm;

            public CustomFeeModel(QCAlgorithm algorithm)
            {
                _algorithm = algorithm;
            }

            public override OrderFee GetOrderFee(OrderFeeParameters parameters)
            {
                // custom fee math
                var fee = Math.Max(
                    1m,
                    parameters.Security.Price*parameters.Order.AbsoluteQuantity*0.00001m);

                _algorithm.Log($"CustomFeeModel: {fee}");
                return new OrderFee(new CashAmount(fee, "USD"));
            }
        }

        public class CustomSlippageModel : ISlippageModel
        {
            private readonly QCAlgorithm _algorithm;

            public CustomSlippageModel(QCAlgorithm algorithm)
            {
                _algorithm = algorithm;
            }

            public decimal GetSlippageApproximation(Security asset, Order order)
            {
                // custom slippage math
                var slippage = asset.Price*0.0001m*(decimal) Math.Log10(2*(double) order.AbsoluteQuantity);

                _algorithm.Log($"CustomSlippageModel: {slippage}");
                return slippage;
            }
        }

        public class CustomBuyingPowerModel : BuyingPowerModel
        {
            private readonly QCAlgorithm _algorithm;

            public CustomBuyingPowerModel(QCAlgorithm algorithm)
            {
                _algorithm = algorithm;
            }

            public override HasSufficientBuyingPowerForOrderResult HasSufficientBuyingPowerForOrder(
                HasSufficientBuyingPowerForOrderParameters parameters)
            {
                // custom behavior: this model will assume that there is always enough buying power
                var hasSufficientBuyingPowerForOrderResult = new HasSufficientBuyingPowerForOrderResult(true);
                _algorithm.Log($"CustomBuyingPowerModel: {hasSufficientBuyingPowerForOrderResult.IsSufficient}");

                return hasSufficientBuyingPowerForOrderResult;
            }
        }

        /// <summary>
        /// The simple fill model shows how to implement a simpler version of 
        /// the most popular order fills: Market, Stop Market and Limit
        /// </summary>
        public class SimpleCustomFillModel : FillModel
        {
            private static OrderEvent CreateOrderEvent(Security asset, Order order)
            {
                var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
                return new OrderEvent(order, utcTime, OrderFee.Zero);
            }

            private static OrderEvent SetOrderEventToFilled(OrderEvent fill, decimal fillPrice, decimal fillQuantity)
            {
                fill.Status = OrderStatus.Filled;
                fill.FillQuantity = fillQuantity;
                fill.FillPrice = fillPrice;
                return fill;
            }

            private static TradeBar GetTradeBar(Security asset, OrderDirection orderDirection)
            {
                var tradeBar = asset.Cache.GetData<TradeBar>();
                if (tradeBar != null) return tradeBar;
                
                // Tick-resolution data doesn't have TradeBar, use the asset price
                var price = asset.Price;
                return new TradeBar(asset.LocalTime, asset.Symbol, price, price, price, price, 0);
            }

            public override OrderEvent MarketFill(Security asset, MarketOrder order)
            {
                var fill = CreateOrderEvent(asset, order);
                if (order.Status == OrderStatus.Canceled) return fill;

                return SetOrderEventToFilled(fill,
                    order.Direction == OrderDirection.Buy
                        ? asset.Cache.AskPrice
                        : asset.Cache.BidPrice,
                    order.Quantity);
            }

            public override OrderEvent StopMarketFill(Security asset, StopMarketOrder order)
            {
                var fill = CreateOrderEvent(asset, order);
                if (order.Status == OrderStatus.Canceled) return fill;

                var stopPrice = order.StopPrice;
                var tradeBar = GetTradeBar(asset, order.Direction);
                
                return order.Direction switch
                {
                    OrderDirection.Buy => tradeBar.Low < stopPrice
                        ? SetOrderEventToFilled(fill, stopPrice, order.Quantity)
                        : fill,
                    OrderDirection.Sell => tradeBar.High > stopPrice
                        ? SetOrderEventToFilled(fill, stopPrice, order.Quantity)
                        : fill,
                    _ => fill
                };
            }

            public override OrderEvent LimitFill(Security asset, LimitOrder order)
            {
                var fill = CreateOrderEvent(asset, order);
                if (order.Status == OrderStatus.Canceled) return fill;

                var limitPrice = order.LimitPrice;
                var tradeBar = GetTradeBar(asset, order.Direction);

                return order.Direction switch
                {
                    OrderDirection.Buy => tradeBar.High > limitPrice
                        ? SetOrderEventToFilled(fill, limitPrice, order.Quantity)
                        : fill,
                    OrderDirection.Sell => tradeBar.Low < limitPrice
                        ? SetOrderEventToFilled(fill, limitPrice, order.Quantity)
                        : fill,
                    _ => fill
                };
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
        public long DataPoints => 330;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "63"},
            {"Average Win", "0.11%"},
            {"Average Loss", "-0.06%"},
            {"Compounding Annual Return", "-7.236%"},
            {"Drawdown", "2.400%"},
            {"Expectancy", "-0.187"},
            {"Start Equity", "100000"},
            {"End Equity", "99370.95"},
            {"Net Profit", "-0.629%"},
            {"Sharpe Ratio", "-1.47"},
            {"Sortino Ratio", "-2.086"},
            {"Probabilistic Sharpe Ratio", "21.874%"},
            {"Loss Rate", "70%"},
            {"Win Rate", "30%"},
            {"Profit-Loss Ratio", "1.73"},
            {"Alpha", "-0.102"},
            {"Beta", "0.122"},
            {"Annual Standard Deviation", "0.04"},
            {"Annual Variance", "0.002"},
            {"Information Ratio", "-4.126"},
            {"Tracking Error", "0.102"},
            {"Treynor Ratio", "-0.479"},
            {"Total Fees", "$62.25"},
            {"Estimated Strategy Capacity", "$52000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "197.95%"},
            {"OrderListHash", "709bbf9af9ec6b43a10617dc192a6a5b"}
        };
    }
}

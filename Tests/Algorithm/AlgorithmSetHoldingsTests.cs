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
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmSetHoldingsTests
    {
        // Test class to enable calling protected methods
        public class TestSecurityMarginModel : SecurityMarginModel
        {
            public TestSecurityMarginModel(decimal leverage) : base(leverage) { }

            public new decimal GetInitialMarginRequiredForOrder(
                InitialMarginRequiredForOrderParameters parameters)
            {
                return base.GetInitialMarginRequiredForOrder(parameters);
            }

            public new decimal GetMarginRemaining(SecurityPortfolioManager portfolio, Security security, OrderDirection direction)
            {
                return base.GetMarginRemaining(portfolio, security, direction);
            }
        }

        public enum Position { Zero = 0, Long = 1, Short = -1 }
        public enum FeeType { None, Small, Large, InteractiveBrokers }
        public enum PriceMovement { Static, RisingSmall, FallingSmall, RisingLarge, FallingLarge }

        private readonly Dictionary<FeeType, IFeeModel> _feeModels = new Dictionary<FeeType, IFeeModel>
        {
            { FeeType.None, new ConstantFeeModel(0) },
            { FeeType.Small, new ConstantFeeModel(1) },
            { FeeType.Large, new ConstantFeeModel(100) },
            { FeeType.InteractiveBrokers, new InteractiveBrokersFeeModel() }
        };

        private readonly Symbol _symbol = Symbols.SPY;
        private const decimal Cash = 100000m;
        private const decimal VeryLowPrice = 155m;
        private const decimal LowPrice = 159m;
        private const decimal BasePrice = 160m;
        private const decimal HighPrice = 161m;
        private const decimal VeryHighPrice = 165m;

        public class Permuter<T>
        {
            private static void Permute(T[] row, int index, IReadOnlyList<List<T>> data, ICollection<T[]> result)
            {
                foreach (var dataRow in data[index])
                {
                    row[index] = dataRow;
                    if (index >= data.Count - 1)
                    {
                        var rowCopy = new T[row.Length];
                        row.CopyTo(rowCopy, 0);
                        result.Add(rowCopy);
                    }
                    else
                    {
                        Permute(row, index + 1, data, result);
                    }
                }
            }

            public static void Permute(List<List<T>> data, List<T[]> result)
            {
                if (data.Count == 0)
                    return;

                Permute(new T[data.Count], 0, data, result);
            }
        }

        public TestCaseData[] TestParameters
        {
            get
            {
                var initialPositions = Enum.GetValues(typeof(Position)).Cast<Position>().ToList();
                var finalPositions = Enum.GetValues(typeof(Position)).Cast<Position>().ToList();
                var feeTypes = Enum.GetValues(typeof(FeeType)).Cast<FeeType>().ToList();
                var priceMovements = Enum.GetValues(typeof(PriceMovement)).Cast<PriceMovement>().ToList();
                var leverages = new List<int> { 1, 100 };

                var data = new List<List<object>>
                {
                    initialPositions.Cast<object>().ToList(),
                    finalPositions.Cast<object>().ToList(),
                    feeTypes.Cast<object>().ToList(),
                    priceMovements.Cast<object>().ToList(),
                    leverages.Cast<object>().ToList()
                };
                var permutations = new List<object[]>();
                Permuter<object>.Permute(data, permutations);

                var ret = permutations
                    .Where(row => (Position)row[0] != (Position)row[1])     // initialPosition != finalPosition
                    .Select(row => new TestCaseData(row).SetName(string.Join("_", row)))
                    .ToArray();

                return ret;
            }
        }

        [Test, TestCaseSource("TestParameters")]
        public void Run(Position initialPosition, Position finalPosition, FeeType feeType, PriceMovement priceMovement, int leverage)
        {
            //Console.WriteLine("----------");
            //Console.WriteLine("PARAMETERS");
            //Console.WriteLine("Initial position: " + initialPosition);
            //Console.WriteLine("Final position: " + finalPosition);
            //Console.WriteLine("Fee type: " + feeType);
            //Console.WriteLine("Price movement: " + priceMovement);
            //Console.WriteLine("Leverage: " + leverage);
            //Console.WriteLine("----------");
            //Console.WriteLine();

            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            var security = algorithm.AddSecurity(_symbol.ID.SecurityType, _symbol.ID.Symbol);
            security.FeeModel = _feeModels[feeType];
            security.SetLeverage(leverage);

            var buyingPowerModel = new TestSecurityMarginModel(leverage);
            security.BuyingPowerModel = buyingPowerModel;

            algorithm.SetCash(Cash);

            Update(security, BasePrice);

            decimal targetPercentage;
            OrderDirection orderDirection;
            MarketOrder order;
            OrderFee orderFee;
            OrderEvent fill;
            decimal orderQuantity;
            decimal freeMargin;
            decimal requiredMargin;
            if (initialPosition != Position.Zero)
            {
                targetPercentage = (decimal)initialPosition;
                orderDirection = initialPosition == Position.Long ? OrderDirection.Buy : OrderDirection.Sell;
                orderQuantity = algorithm.CalculateOrderQuantity(_symbol, targetPercentage);
                order = new MarketOrder(_symbol, orderQuantity, DateTime.UtcNow);
                freeMargin = buyingPowerModel.GetMarginRemaining(algorithm.Portfolio, security, orderDirection);
                requiredMargin = buyingPowerModel.GetInitialMarginRequiredForOrder(
                    new InitialMarginRequiredForOrderParameters(
                        new IdentityCurrencyConverter(algorithm.Portfolio.CashBook.AccountCurrency), security, order));

                //Console.WriteLine("Current price: " + security.Price);
                //Console.WriteLine("Target percentage: " + targetPercentage);
                //Console.WriteLine("Order direction: " + orderDirection);
                //Console.WriteLine("Order quantity: " + orderQuantity);
                //Console.WriteLine("Free margin: " + freeMargin);
                //Console.WriteLine("Required margin: " + requiredMargin);
                //Console.WriteLine();

                Assert.That(Math.Abs(requiredMargin) <= freeMargin);

                orderFee = security.FeeModel.GetOrderFee(
                    new OrderFeeParameters(security, order));
                fill = new OrderEvent(order, DateTime.UtcNow, orderFee) { FillPrice = security.Price, FillQuantity = orderQuantity };
                algorithm.Portfolio.ProcessFill(fill);

                //Console.WriteLine("Portfolio.Cash: " + algorithm.Portfolio.Cash);
                //Console.WriteLine("Portfolio.TotalPortfolioValue: " + algorithm.Portfolio.TotalPortfolioValue);
                //Console.WriteLine();

                if (priceMovement == PriceMovement.RisingSmall)
                {
                    Update(security, HighPrice);
                }
                else if (priceMovement == PriceMovement.FallingSmall)
                {
                    Update(security, LowPrice);
                }
                else if (priceMovement == PriceMovement.RisingLarge)
                {
                    Update(security, VeryHighPrice);
                }
                else if (priceMovement == PriceMovement.FallingLarge)
                {
                    Update(security, VeryLowPrice);
                }
            }

            targetPercentage = (decimal)finalPosition;
            orderDirection = finalPosition == Position.Long || (finalPosition == Position.Zero && initialPosition == Position.Short) ? OrderDirection.Buy : OrderDirection.Sell;
            orderQuantity = algorithm.CalculateOrderQuantity(_symbol, targetPercentage);
            order = new MarketOrder(_symbol, orderQuantity, DateTime.UtcNow);
            freeMargin = buyingPowerModel.GetMarginRemaining(algorithm.Portfolio, security, orderDirection);
            requiredMargin = buyingPowerModel.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(
                    new IdentityCurrencyConverter(algorithm.Portfolio.CashBook.AccountCurrency), security, order));

            //Console.WriteLine("Current price: " + security.Price);
            //Console.WriteLine("Target percentage: " + targetPercentage);
            //Console.WriteLine("Order direction: " + orderDirection);
            //Console.WriteLine("Order quantity: " + orderQuantity);
            //Console.WriteLine("Free margin: " + freeMargin);
            //Console.WriteLine("Required margin: " + requiredMargin);
            //Console.WriteLine();

            Assert.That(Math.Abs(requiredMargin) <= freeMargin);

            orderFee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, order));
            fill = new OrderEvent(order, DateTime.UtcNow, orderFee) { FillPrice = security.Price, FillQuantity = orderQuantity };
            algorithm.Portfolio.ProcessFill(fill);

            //Console.WriteLine("Portfolio.Cash: " + algorithm.Portfolio.Cash);
            //Console.WriteLine("Portfolio.TotalPortfolioValue: " + algorithm.Portfolio.TotalPortfolioValue);
            //Console.WriteLine();
        }

        private static void Update(Security security, decimal price)
        {
            security.SetMarketPrice(new TradeBar
            {
                Time = DateTime.Now, Symbol = security.Symbol, Open = price, High = price, Low = price, Close = price
            });
        }
    }
}

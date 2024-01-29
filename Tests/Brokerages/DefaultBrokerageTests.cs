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
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages
{
    /// <summary>
    /// Additional tests for the base <see cref="Brokerage"/> class
    /// </summary>
    public class DefaultBrokerageTests
    {
        [TestCase(OrderDirection.Buy, 0, ExpectedResult = OrderPosition.BuyToOpen)]
        [TestCase(OrderDirection.Buy, 100, ExpectedResult = OrderPosition.BuyToOpen)]
        [TestCase(OrderDirection.Buy, -100, ExpectedResult = OrderPosition.BuyToClose)]
        [TestCase(OrderDirection.Sell, 0, ExpectedResult = OrderPosition.SellToOpen)]
        [TestCase(OrderDirection.Sell, 100, ExpectedResult = OrderPosition.SellToClose)]
        [TestCase(OrderDirection.Sell, -100, ExpectedResult = OrderPosition.SellToOpen)]
        public OrderPosition GetsOrderPosition(OrderDirection direction, decimal holdingsQuantity)
        {
            return TestableBrokerage.GetOrderPositionPublic(direction, holdingsQuantity);
        }

        private class TestableBrokerage : Brokerage
        {
            public TestableBrokerage(string name) : base(name)
            {
            }

            public override bool IsConnected => throw new NotImplementedException();

            public override bool CancelOrder(Order order)
            {
                throw new NotImplementedException();
            }

            public override void Connect()
            {
                throw new NotImplementedException();
            }

            public override void Disconnect()
            {
                throw new NotImplementedException();
            }

            public override List<Holding> GetAccountHoldings()
            {
                throw new NotImplementedException();
            }

            public override List<CashAmount> GetCashBalance()
            {
                throw new NotImplementedException();
            }

            public override List<Order> GetOpenOrders()
            {
                throw new NotImplementedException();
            }

            public override bool PlaceOrder(Order order)
            {
                throw new NotImplementedException();
            }

            public override bool UpdateOrder(Order order)
            {
                throw new NotImplementedException();
            }

            public static OrderPosition GetOrderPositionPublic(OrderDirection direction, decimal holdingsQuantity)
            {
                return GetOrderPosition(direction, holdingsQuantity);
            }
        }

    }
}

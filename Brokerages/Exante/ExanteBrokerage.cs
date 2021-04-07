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

using Newtonsoft.Json;
using QuantConnect.Orders;
using QuantConnect.Securities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using com.sun.security.ntlm;
using Exante.Net;
using Exante.Net.Enums;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders.Fees;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Exante
{
    public class ExanteBrokerage : Brokerage, IDataQueueHandler
    {
        private bool _isConnected;
        private readonly ExanteClient _client;
        private string _accountId;

        public ExanteBrokerage(
            ExanteClient client,
            string accountId
            )
            : base("Exante Brokerage")
        {
            _client = client;
            _accountId = accountId;
        }

        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            throw new NotImplementedException();
        }

        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            throw new NotImplementedException();
        }

        public void SetJob(LiveNodePacket job)
        {
            throw new NotImplementedException();
        }

        public override bool IsConnected => _isConnected;

        public override List<Order> GetOpenOrders()
        {
            throw new NotImplementedException();
        }

        public override List<Holding> GetAccountHoldings()
        {
            throw new NotImplementedException();
        }

        public override List<CashAmount> GetCashBalance()
        {
            const string reportCurrency = "USD";
            var accountSummary =
                _client
                    .GetAccountSummaryAsync(_accountId, reportCurrency)
                    .SynchronouslyAwaitTaskResult();
            var cashAmounts =
                from currencyData in accountSummary.Data.Currencies
                select new CashAmount(currencyData.Value, currencyData.Currency);
            return cashAmounts.ToList();
        }

        public override bool PlaceOrder(Order order)
        {
            var account = _client.GetAccountsAsync().Result.Data.GetEnumerator().Current;
            var orderSide = default(ExanteOrderSide);
            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    orderSide = ExanteOrderSide.Buy;
                    break;
                case OrderDirection.Sell:
                    orderSide = ExanteOrderSide.Sell;
                    break;
            }

            var orderPlacementTask = _client.PlaceOrderAsync(
                account.AccountId,
                order.Symbol.ID.Symbol,
                ExanteOrderType.Market,
                orderSide,
                order.Quantity,
                ExanteOrderDuration.AtTheClose
            );
            var orderPlacement = orderPlacementTask.Result.Data.ToList()[0];

            var isPlaced = orderPlacement.OrderState.Status != ExanteOrderStatus.Cancelled;
            return isPlaced;
        }

        public override bool UpdateOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override bool CancelOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override void Connect()
        {
            _isConnected = true;
        }

        public override void Disconnect()
        {
            _isConnected = false;
        }
    }
}

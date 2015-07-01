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
using QuantConnect.Data.Market;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.Examples
{
    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash
    /// </summary>
    public class OrderOperationsAlgorithm : QCAlgorithm
    {
        private string _symbol = "SPY";
        private Order submittedMarketOrder;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecurityType.Equity, _symbol, Resolution.Second);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {
            var security = Securities[_symbol];

            if (!Portfolio.Invested)
            {
                submittedMarketOrder = null;

                SetHoldings(_symbol, 1);

                if (submittedMarketOrder != null)
                {
                    if (Transactions.CachedOrderCount == 1)
                    {

                        Transactions.GetCompletedOrderAsync(1).ContinueWith(t =>
                        {
                            var bigOrderId = ProcessAndLog(QuantConnect.Orders.MarketOrder.CreateSubmitRequest(security.Type, security.Symbol, -3000, security.Time, tag: "Big Order", price: security.Price));

                            var bigOrder = Transactions.GetOrderById(bigOrderId);

                            // Must fail due to invalid order status
                            ProcessAndLog(((QuantConnect.Orders.MarketOrder)bigOrder).CreateUpdateRequest(-100));
                        });

                    }

                    Transactions.GetCompletedOrderAsync(submittedMarketOrder.Id).ContinueWith(entry =>
                    {
                        var takeProfitLimitOrderId = ProcessAndLog(QuantConnect.Orders.LimitOrder.CreateSubmitRequest(security.Type, security.Symbol, -security.Holdings.Quantity, security.Time, security.Price + 10m, tag: "Take Profit"));

                        Transactions.GetOrderAsync(o => o.Id == takeProfitLimitOrderId).ContinueWith(takeProfitLimitOrderTask =>
                        {
                            var takeProfitLimitOrder = takeProfitLimitOrderTask.Result;

                            ProcessAndLog(((QuantConnect.Orders.LimitOrder)takeProfitLimitOrder).CreateUpdateRequest(limitPrice: Securities[_symbol].Price + 1m));
                            var stopLossStopMarketOrderId = ProcessAndLog(QuantConnect.Orders.StopMarketOrder.CreateSubmitRequest(security.Type, security.Symbol, -security.Holdings.Quantity, security.Time, security.Price - 1m, tag: "Stop Loss"));

                            // an imitation of OCO
                            Transactions.GetCompletedOrderAsync(takeProfitLimitOrderId).ContinueWith(t => { if (t.Result.Status == OrderStatus.Filled) Log(CancelOrder(stopLossStopMarketOrderId)); });
                            Transactions.GetCompletedOrderAsync(stopLossStopMarketOrderId).ContinueWith(t => { if (t.Result.Status == OrderStatus.Filled) Log(CancelOrder(takeProfitLimitOrderId)); });

                            Transactions.GetOrderAsync(o => o.Id == stopLossStopMarketOrderId).ContinueWith(slOrderTask =>
                            {
                                var stopLossStopmarketOrder = slOrderTask.Result;

                                UpdateOrder((QuantConnect.Orders.StopMarketOrder)stopLossStopmarketOrder, stopPrice: Securities[_symbol].Price - 0.9m);

                            });

                        });

                    });
                }

            }
        }

        private void Log(OrderResponse response)
        {
            Console.WriteLine(response);
        }

        private int ProcessAndLog(OrderRequest request)
        {
            Console.WriteLine(request);

            var response = Transactions.ProcessOrderRequest(request).Result;

            Console.WriteLine(response);

            return response.OrderId;
        }

        public override void OnOrderEvent(OrderEvent fill)
        {
            var order = Transactions.GetOrderById(fill.OrderId);

            if (order != null && order.Type.IsMarket() && order.Status == OrderStatus.Submitted)
                submittedMarketOrder = order;

            Console.WriteLine(Time + " - " + order.Type + " - " + fill.Status + ":: " + fill);
        }
    }
}
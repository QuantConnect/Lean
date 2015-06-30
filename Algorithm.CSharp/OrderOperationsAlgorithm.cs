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
            if (!Portfolio.Invested)
            {
                SetHoldings(_symbol, 1);

                // Must fail due to insufficient bying power
                var bigOrderId = ProcessAndLog(QuantConnect.Orders.MarketOrder.SubmitRequest(_symbol, - 3000, "Big Order", Securities[_symbol].Type, Securities[_symbol].Price));

                var bigOrder = Transactions.GetOrderById(bigOrderId);

                // Must fail due to invalid order status
                ProcessAndLog(((QuantConnect.Orders.MarketOrder)bigOrder).UpdateRequest(-100));

                var takeProfitLimitOrderId = ProcessAndLog(QuantConnect.Orders.LimitOrder.SubmitRequest(_symbol, -Securities[_symbol].Holdings.Quantity, Securities[_symbol].Price + 10m, "Take Profit", Securities[_symbol].Type));
                var takeProfitLimitOrder = Transactions.GetOrderById(takeProfitLimitOrderId);
                ProcessAndLog(((QuantConnect.Orders.LimitOrder)takeProfitLimitOrder).UpdateRequest(limitPrice: Securities[_symbol].Price + 0.1m));

                Transactions.WaitForOrder(takeProfitLimitOrderId);
            }


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
            Console.WriteLine(Time + " - " + order.Type + " - " + fill.Status + ":: " + fill);
        }
    }
}
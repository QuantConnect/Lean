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
using QuantConnect.Data.Market;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm showcases two margin related event handlers.
    /// OnMarginCallWarning: Fired when a portfolio's remaining margin dips below 5% of the total portfolio value
    /// OnMarginCall: Fired immediately before margin call orders are execued, this gives the algorithm a change to regain margin on its own through liquidation
    /// </summary>
    /// <meta name="tag" content="securities and portfolio" />
    /// <meta name="tag" content="margin models" />
    public class MarginCallEventsAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 01);  //Set Start Date
            SetEndDate(2013, 12, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecurityType.Equity, "SPY", Resolution.Second);

            // cranking up the leverage increases the odds of a margin call when the security falls in value
            Securities["SPY"].SetLeverage(100);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {
            if (!Portfolio.Invested)
            {
                Liquidate();
                SetHoldings("SPY", 100);
            }
        }

        /// <summary>
        /// Margin call event handler. This method is called right before the margin call orders are placed in the market.
        /// </summary>
        /// <param name="requests">The orders to be executed to bring this algorithm within margin limits</param>
        public override void OnMarginCall(List<SubmitOrderRequest> requests)
        {
            // this code gets called BEFORE the orders are placed, so we can try to liquidate some of our positions
            // before we get the margin call orders executed. We could also modify these orders by changing their
            // quantities
            foreach (var order in requests.ToList())
            {
                // liquidate an extra 10% each time we get a margin call to give us more padding
                var newQuantity = (int)(Math.Sign(order.Quantity) * order.Quantity * 1.1m);
                requests.Remove(order);
                requests.Add(new SubmitOrderRequest(order.OrderType, order.SecurityType, order.Symbol, newQuantity, order.StopPrice, order.LimitPrice, Time, "OnMarginCall"));
            }
        }

        /// <summary>
        /// Margin call warning event handler. This method is called when Portfolio.MarginRemaining is under 5% of your Portfolio.TotalPortfolioValue
        /// </summary>
        public override void OnMarginCallWarning()
        {
            // this code gets called when the margin remaining drops below 5% of our total portfolio value, it gives the algorithm
            // a chance to prevent a margin call from occurring

            // prevent margin calls by responding to the warning and increasing margin remaining
            var spyHoldings = Securities["SPY"].Holdings.Quantity;
            var shares = (int)(-spyHoldings * .005m);
            Error($"{Time.ToStringInvariant()} - OnMarginCallWarning(): Liquidating {shares.ToStringInvariant()} shares of SPY to avoid margin call.");
            MarketOrder("SPY", shares);
        }
    }
}
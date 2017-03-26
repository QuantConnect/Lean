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
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using Order = QuantConnect.Orders.Order;

namespace QuantConnect.Brokerages.Oanda
{
    /// <summary>
    /// Oanda REST API v20 implementation
    /// </summary>
    public class OandaRestApiV20 : OandaRestApiBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OandaRestApiV20"/> class.
        /// </summary>
        /// <param name="symbolMapper">The symbol mapper.</param>
        /// <param name="orderProvider">The order provider.</param>
        /// <param name="securityProvider">The holdings provider.</param>
        /// <param name="environment">The Oanda environment (Trade or Practice)</param>
        /// <param name="accessToken">The Oanda access token (can be the user's personal access token or the access token obtained with OAuth by QC on behalf of the user)</param>
        /// <param name="accountId">The account identifier.</param>
        public OandaRestApiV20(OandaSymbolMapper symbolMapper, IOrderProvider orderProvider, ISecurityProvider securityProvider, Environment environment, string accessToken, string accountId)
            : base(symbolMapper, orderProvider, securityProvider, environment, accessToken, accountId)
        {
        }

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public override void Disconnect()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the list of available tradable instruments/products from Oanda
        /// </summary>
        public override List<string> GetInstrumentList()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets all open orders on the account. 
        /// NOTE: The order objects returned do not have QC order IDs.
        /// </summary>
        /// <returns>The open orders returned from Oanda</returns>
        public override List<Order> GetOpenOrders()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<Cash> GetCashBalance()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves the current rate for each of a list of instruments
        /// </summary>
        /// <param name="instruments">the list of instruments to check</param>
        /// <returns>Dictionary containing the current quotes for each instrument</returns>
        public override Dictionary<string, Tick> GetRates(List<string> instruments)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Starts streaming transactions for the active account
        /// </summary>
        public override void StartTransactionStream()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Stops streaming transactions for the active account
        /// </summary>
        public override void StopTransactionStream()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Starts streaming prices for a list of instruments
        /// </summary>
        public override void StartPricingStream(List<string> instruments)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Stops streaming prices for all instruments
        /// </summary>
        public override void StopPricingStream()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Downloads a list of TradeBars at the requested resolution
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="startTimeUtc">The starting time (UTC)</param>
        /// <param name="endTimeUtc">The ending time (UTC)</param>
        /// <param name="resolution">The requested resolution</param>
        /// <param name="requestedTimeZone">The requested timezone for the data</param>
        /// <returns>The list of bars</returns>
        public override IEnumerable<TradeBar> DownloadTradeBars(Symbol symbol, DateTime startTimeUtc, DateTime endTimeUtc, Resolution resolution, DateTimeZone requestedTimeZone)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Downloads a list of QuoteBars at the requested resolution
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="startTimeUtc">The starting time (UTC)</param>
        /// <param name="endTimeUtc">The ending time (UTC)</param>
        /// <param name="resolution">The requested resolution</param>
        /// <param name="requestedTimeZone">The requested timezone for the data</param>
        /// <returns>The list of bars</returns>
        public override IEnumerable<QuoteBar> DownloadQuoteBars(Symbol symbol, DateTime startTimeUtc, DateTime endTimeUtc, Resolution resolution, DateTimeZone requestedTimeZone)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the next ticks from the live trading data queue
        /// </summary>
        /// <returns>IEnumerable list of ticks since the last update.</returns>
        public override IEnumerable<BaseData> GetNextTicks()
        {
            throw new NotImplementedException();
        }
    }
}

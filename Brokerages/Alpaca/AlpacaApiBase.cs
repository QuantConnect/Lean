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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using QuantConnect.Brokerages.Alpaca.Markets;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Alpaca
{
	/// <summary>
	/// Alpaca API base class
	/// </summary>
	public class AlpacaApiBase : Brokerage, IDataQueueHandler
	{
		private static readonly TimeSpan SubscribeDelay = TimeSpan.FromMilliseconds(250);
		private DateTime _lastSubscribeRequestUtcTime = DateTime.MinValue;
		
		private bool _subscriptionsPending;

		private bool _isConnected;
		private Thread _connectionMonitorThread;
		private volatile bool _connectionLost;
		private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

		private Markets.RestClient restClient;
		/// <summary>
		/// This lock is used to sync 'PlaceOrder' and callback 'OnTransactionDataReceived'
		/// </summary>
		protected readonly object Locker = new object();
		/// <summary>
		/// This container is used to keep pending to be filled market orders, so when the callback comes in we send the filled event
		/// </summary>
		protected readonly ConcurrentDictionary<int, Orders.OrderStatus> PendingFilledMarketOrders = new ConcurrentDictionary<int, Orders.OrderStatus>();

		/// <summary>
		/// The UTC time of the last received heartbeat message
		/// </summary>
		protected DateTime LastHeartbeatUtcTime;

		/// <summary>
		/// A lock object used to synchronize access to LastHeartbeatUtcTime
		/// </summary>
		protected readonly object LockerConnectionMonitor = new object();

		/// <summary>
		/// The list of ticks received
		/// </summary>
		protected readonly List<Tick> Ticks = new List<Tick>();

		/// <summary>
		/// The list of currently subscribed symbols
		/// </summary>
		protected HashSet<Symbol> SubscribedSymbols = new HashSet<Symbol>();

		/// <summary>
		/// A lock object used to synchronize access to subscribed symbols
		/// </summary>
		protected readonly object LockerSubscriptions = new object();

		/// <summary>
		/// The order provider
		/// </summary>
		protected IOrderProvider OrderProvider;

		/// <summary>
		/// The security provider
		/// </summary>
		protected ISecurityProvider SecurityProvider;

		/// <summary>
		/// The Alpaca api key
		/// </summary>
		protected string AccountKeyId;

		/// <summary>
		/// The Alpaca api secret
		/// </summary>
		protected string SecretKey;

		/// <summary>
		/// The Alpaca base url
		/// </summary>
		protected string BaseUrl;


		private TransactionStreamSession _eventsSession;
		private PricingStreamSession _ratesSession;
		private readonly Dictionary<Symbol, DateTimeZone> _symbolExchangeTimeZones = new Dictionary<Symbol, DateTimeZone>();


		/// <summary>
		/// Initializes a new instance of the <see cref="AlpacaApiBase"/> class.
		/// </summary>
		/// <param name="orderProvider">The order provider.</param>
		/// <param name="securityProvider">The holdings provider.</param>
		/// <param name="keyId">The Alpaca api key</param>
		/// <param name="secretKey">The api secret</param>
		/// <param name="baseUrl">The Alpaca base url</param>
		public AlpacaApiBase(IOrderProvider orderProvider, ISecurityProvider securityProvider, string keyId, string secretKey, string baseUrl)
			: base("Alpaca Brokerage")
		{
			OrderProvider = orderProvider;
			SecurityProvider = securityProvider;
            AccountKeyId = keyId;
			SecretKey = secretKey;
			BaseUrl = baseUrl;

			restClient = new Markets.RestClient(AccountKeyId, SecretKey, baseUrl);
		}

		/// <summary>
		/// Returns true if we're currently connected to the broker
		/// </summary>
		public override bool IsConnected
		{
			get { return _isConnected && !_connectionLost; }
		}

		/// <summary>
		/// Connects the client to the broker's remote servers
		/// </summary>
		public override void Connect()
		{
			// Register to the event session to receive events.
			StartTransactionStream();

			_isConnected = true;

			// create new thread to manage disconnections and reconnections
			_cancellationTokenSource = new CancellationTokenSource();
			_connectionMonitorThread = new Thread(() =>
			{
				var nextReconnectionAttemptUtcTime = DateTime.UtcNow;
				double nextReconnectionAttemptSeconds = 1;

				lock (LockerConnectionMonitor)
				{
					LastHeartbeatUtcTime = DateTime.UtcNow;
				}

				try
				{
					while (!_cancellationTokenSource.IsCancellationRequested)
					{
						TimeSpan elapsed;
						lock (LockerConnectionMonitor)
						{
                            elapsed = TimeSpan.FromSeconds(10); // DateTime.UtcNow - LastHeartbeatUtcTime;
						}

						if (!_connectionLost && elapsed > TimeSpan.FromSeconds(20))
						{
							_connectionLost = true;
							nextReconnectionAttemptUtcTime = DateTime.UtcNow.AddSeconds(nextReconnectionAttemptSeconds);

							OnMessage(BrokerageMessageEvent.Disconnected("Connection with Alpaca server lost. " +
																		 "This could be because of internet connectivity issues. "));
						}
						else if (_connectionLost)
						{
							try
							{
								if (elapsed <= TimeSpan.FromSeconds(20))
								{
									_connectionLost = false;
									nextReconnectionAttemptSeconds = 1;

									OnMessage(BrokerageMessageEvent.Reconnected("Connection with Alpaca server restored."));
								}
								else
								{
									if (DateTime.UtcNow > nextReconnectionAttemptUtcTime)
									{
										try
										{
											// check if we have a connection
											GetInstrumentList();

											// restore events session
											StopTransactionStream();
											StartTransactionStream();

											// restore rates session
											List<Symbol> symbolsToSubscribe;
											lock (LockerSubscriptions)
											{
												symbolsToSubscribe = SubscribedSymbols.ToList();
											}
											SubscribeSymbols(symbolsToSubscribe);
										}
										catch (Exception)
										{
											// double the interval between attempts (capped to 1 minute)
											nextReconnectionAttemptSeconds = Math.Min(nextReconnectionAttemptSeconds * 2, 60);
											nextReconnectionAttemptUtcTime = DateTime.UtcNow.AddSeconds(nextReconnectionAttemptSeconds);
										}
									}
								}
							}
							catch (Exception exception)
							{
								Log.Error(exception);
							}
						}

						Thread.Sleep(1000);
					}
				}
				catch (Exception exception)
				{
					Log.Error(exception);
				}
			})
			{ IsBackground = true };
			_connectionMonitorThread.Start();
			while (!_connectionMonitorThread.IsAlive)
			{
				Thread.Sleep(1);
			}
		}

		/// <summary>
		/// Disconnects the client from the broker's remote servers
		/// </summary>
		public override void Disconnect()
		{
			StopTransactionStream();
			StopPricingStream();

			// request and wait for thread to stop
			_cancellationTokenSource.Cancel();
			if (_connectionMonitorThread != null)
			{
				_connectionMonitorThread.Join();
			}
			
			_isConnected = false;
		}

		/// <summary>
		/// Gets the list of available tradable instruments/products from Alpaca
		/// </summary>
		public List<string> GetInstrumentList()
		{
			var response = restClient.ListAssetsAsync().Result;

			return response.Select(x => x.Symbol).ToList();
		}

		/// <summary>
		/// Retrieves the current rate for each of a list of instruments
		/// </summary>
		/// <param name="instruments">the list of instruments to check</param>
		/// <returns>Dictionary containing the current quotes for each instrument</returns>
		public Dictionary<string, Tick> GetRates(List<string> instruments)
		{
			var response = restClient.ListQuotesAsync(instruments).Result;
			return response
				.ToDictionary(
					x => x.Symbol,
					x => new Tick { BidPrice = x.BidPrice, AskPrice = x.AskPrice }
				);
		}

		/// <summary>
		/// Gets all open orders on the account.
		/// NOTE: The order objects returned do not have QC order IDs.
		/// </summary>
		/// <returns>The open orders returned from Alpaca</returns>
		public override List<Order> GetOpenOrders()
		{
			var orders = restClient.ListOrdersAsync().Result;

			var qcOrders = new List<Order>();
			foreach (var order in orders)
			{
				qcOrders.Add(ConvertOrder(order));
			}
			return qcOrders;
		}

		/// <summary>
		/// Gets all holdings for the account
		/// </summary>
		/// <returns>The current holdings from the account</returns>
		public override List<Holding> GetAccountHoldings()
		{
			var holdings = restClient.ListPositionsAsync().Result;

			var qcHoldings = new List<Holding>();
			foreach (var holds in holdings)
			{
				qcHoldings.Add(ConvertHolding(holds));
			}

			return qcHoldings;
		}

		/// <summary>
		/// Gets the current cash balance for each currency held in the brokerage account
		/// </summary>
		/// <returns>The current cash balance for each currency available for trading</returns>
		public override List<Cash> GetCashBalance()
		{
			var balance = restClient.GetAccountAsync().Result;

			return new List<Cash>
			{
				new Cash("USD",
					balance.TradableCash,
					1m)
			};
		}

		/// <summary>
		/// Places a new order and assigns a new broker ID to the order
		/// </summary>
		/// <param name="order">The order to be placed</param>
		/// <returns>True if the request for a new order has been placed, false otherwise</returns>
		public override bool PlaceOrder(Order order)
		{
			const int orderFee = 0;
			var marketOrderFillQuantity = 0;
			var marketOrderFillPrice = 0m;
			var marketOrderRemainingQuantity = 0;
			var marketOrderStatus = Orders.OrderStatus.Filled;
			order.PriceCurrency = "USD";

			lock (Locker)
			{
                IOrder apOrder = null;
                try
                {
                    apOrder = GenerateAndPlaceOrder(order);
                    order.BrokerId.Add(apOrder.OrderId.ToString());
                }
                catch (Exception e)
                { 
                    Log.Trace(e.Message);
                    if (e.InnerException != null) Log.Trace(e.InnerException.Message);
                    return false;
                }

				// Market orders are special, due to the callback not being triggered always,
				// if the order was Filled/PartiallyFilled, find fill quantity and price and inform the user
				if (order.Type == Orders.OrderType.Market)
				{
                    marketOrderFillQuantity = Convert.ToInt32(apOrder.FilledQuantity);

                    marketOrderFillPrice = 0;
                    if (marketOrderFillQuantity != 0) marketOrderFillPrice = apOrder.AverageFillPrice.Value;
                                        
					marketOrderRemainingQuantity = Convert.ToInt32(order.AbsoluteQuantity - Math.Abs(marketOrderFillQuantity));
					if (marketOrderRemainingQuantity > 0)
					{
						marketOrderStatus = Orders.OrderStatus.PartiallyFilled;
						// The order was not fully filled lets save it so the callback can inform the user
						PendingFilledMarketOrders[order.Id] = marketOrderStatus;
					}
				}
			}
			OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee) { Status = Orders.OrderStatus.Submitted });

			// If 'marketOrderRemainingQuantity < order.AbsoluteQuantity' is false it means the order was not even PartiallyFilled, wait for callback
			if (order.Type == Orders.OrderType.Market && marketOrderRemainingQuantity < order.AbsoluteQuantity)
			{
				OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Alpaca Fill Event")
				{
					Status = marketOrderStatus,
					FillPrice = marketOrderFillPrice,
					FillQuantity = marketOrderFillQuantity
				});
			}

			return true;
		}

		private Markets.IOrder GenerateAndPlaceOrder(Order order)
		{
			var instrument = order.Symbol;

			var quantity = (long)order.Quantity;
			var side = order.Quantity > 0 ? OrderSide.Buy : OrderSide.Sell;
            if (order.Quantity < 0) quantity = -quantity;
			Markets.OrderType type;
			decimal? limitPrice = null;
			decimal? stopPrice = null;

			switch (order.Type)
			{
				case Orders.OrderType.Market:
					type = Markets.OrderType.Market;
					break;

				case Orders.OrderType.Limit:
					type = Markets.OrderType.Limit;
					limitPrice = ((LimitOrder)order).LimitPrice;
					break;

				case Orders.OrderType.StopMarket:
					type = Markets.OrderType.Stop;
					stopPrice = ((StopMarketOrder)order).StopPrice;
					break;

				case Orders.OrderType.StopLimit:
					type = Markets.OrderType.StopLimit;
					stopPrice = ((StopLimitOrder)order).StopPrice;
					limitPrice = ((StopLimitOrder)order).LimitPrice;
					break;
				default:
					throw new NotSupportedException("The order type " + order.Type + " is not supported.");
			}
			var apOrder = restClient.PostOrderAsync(order.Symbol.Value, quantity, side, type, Markets.TimeInForce.Gtc,
				limitPrice, stopPrice).Result;

			return apOrder;
		}

		/// <summary>
		/// Updates the order with the same id
		/// </summary>
		/// <param name="order">The new order information</param>
		/// <returns>True if the request was made for the order to be updated, false otherwise</returns>
		public override bool UpdateOrder(Order order)
		{
			return false;
		}

		/// <summary>
		/// Cancels the order with the specified ID
		/// </summary>
		/// <param name="order">The order to cancel</param>
		/// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
		public override bool CancelOrder(Order order)
		{
			Log.Trace("AlpacaBrokerage.CancelOrder(): " + order);

			if (!order.BrokerId.Any())
			{
				Log.Trace("AlpacaBrokerage.CancelOrder(): Unable to cancel order without BrokerId.");
				return false;
			}

			foreach (var orderId in order.BrokerId)
			{
				var res = restClient.DeleteOrderAsync(new Guid(orderId)).Result;
				OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Alpaca Cancel Order Event") { Status = Orders.OrderStatus.CancelPending });
			}

			return true;
		}

		/// <summary>
		/// Starts streaming transactions for the active account
		/// </summary>
		public void StartTransactionStream()
		{
			_eventsSession = new TransactionStreamSession(this);
			_eventsSession.TradeReceived += new Action<Markets.ITradeUpdate>(OnTransactionDataReceived);
            _eventsSession.StartSession();
		}

		/// <summary>
		/// Stops streaming transactions for the active account
		/// </summary>
		public void StopTransactionStream()
		{
			if (_eventsSession != null)
			{
            	_eventsSession.StopSession();
			}
		}

		/// <summary>
		/// Event handler for streaming events
		/// </summary>
		/// <param name="trade">The event object</param>
		private void OnTransactionDataReceived(Markets.ITradeUpdate trade)
		{
            Log.Trace("OnTransactionData: {0} {1} {2}", trade.Event, trade.Order.OrderId, trade.Order.OrderStatus);
			Order order;
            string tradeEvent = trade.Event.ToUpper();
			lock (Locker)
			{
				order = OrderProvider.GetOrderByBrokerageId(trade.Order.OrderId.ToString());
			}
			if (order != null)
			{
				Orders.OrderStatus status;
                // Market orders are special: if the order was not in 'PartiallyFilledMarketOrders', means
                // we already sent the fill event with OrderStatus.Filled, else it means we already informed the user
                // of a partiall fill, or didn't inform the user, so we need to do it now
                if (tradeEvent == "FILL")
                {
                    if (order.Type != Orders.OrderType.Market || PendingFilledMarketOrders.TryRemove(order.Id, out status))
                    {
                        order.PriceCurrency = SecurityProvider.GetSecurity(order.Symbol).SymbolProperties.QuoteCurrency;
                        
                        status = Orders.OrderStatus.Filled;
                        if (trade.Order.FilledQuantity < trade.Order.Quantity) status = Orders.OrderStatus.PartiallyFilled;
                        OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Alpaca Fill Event")
                        {
                            Status = status,
                            FillPrice = trade.Price.Value,
                            FillQuantity = Convert.ToInt32(trade.Order.Quantity)
                        });
                    }
                }
                else if (tradeEvent == "CANCELED")
                {
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Alpaca Cancel Order Event") { Status = Orders.OrderStatus.Canceled });
                }
                else if (tradeEvent == "ORDER_CANCEL_REJECTED")
                {

                }
			}
			else
			{
				Log.Error($"AlpacaBrokerage.OnTransactionDataReceived(): order id not found: {trade.Order.OrderId.ToString()}");
			}
		}


		/// <summary>
		/// Starts streaming prices for a list of instruments
		/// </summary>
		public void StartPricingStream(List<string> instruments)
		{
            Log.Trace("Start Pricing Stream");
			_ratesSession = new PricingStreamSession(this, instruments);
			_ratesSession.QuoteReceived += new Action<Markets.IStreamQuote>(OnPricingDataReceived);
			_ratesSession.StartSession();
		}

		/// <summary>
		/// Stops streaming prices for all instruments
		/// </summary>
		public void StopPricingStream()
		{
			if (_ratesSession != null)
			{
                Log.Trace("End Pricing Stream");
				_ratesSession.StopSession();
			}
		}

		/// <summary>
		/// Event handler for streaming ticks
		/// </summary>
		/// <param name="quote">The data object containing the received tick</param>
		private void OnPricingDataReceived(Markets.IStreamQuote quote)
		{
            LastHeartbeatUtcTime = DateTime.UtcNow;
			var symbol = Symbol.Create(quote.Symbol, SecurityType.Equity, Market.USA);
			var time = quote.Time;

			// live ticks timestamps must be in exchange time zone
			DateTimeZone exchangeTimeZone;
			if (!_symbolExchangeTimeZones.TryGetValue(key: symbol, value: out exchangeTimeZone))
			{
				exchangeTimeZone = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.USA, symbol, SecurityType.Equity).TimeZone;
				_symbolExchangeTimeZones.Add(symbol, exchangeTimeZone);
			}
			time = time.ConvertFromUtc(exchangeTimeZone);

			var bidPrice = quote.BidPrice;
			var askPrice = quote.AskPrice;
			var tick = new Tick(time, symbol, bidPrice, askPrice);
			lock (Ticks)
			{
				Ticks.Add(tick);
			}
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
		public IEnumerable<TradeBar> DownloadTradeBars(Symbol symbol, DateTime startTimeUtc, DateTime endTimeUtc, Resolution resolution, DateTimeZone requestedTimeZone)
		{
			var startUtc = startTimeUtc.ToString("yyyy-MM-dd");
			var endUtc = endTimeUtc.ToString("yyyy-MM-dd");

            // This is due to the polygon API logic.
            // If start and end date is equal, then the result is null
            var endTimeUtcForAPI = endTimeUtc.Add(TimeSpan.FromDays(1));

			var period = resolution.ToTimeSpan();

			// No seconds resolution
			if (period.Seconds < 60) 
			{
				yield return null;
			}

			List<Markets.IBar> bars = new List<Markets.IBar>();

            DateTime startTime = startTimeUtc;
            DateTime startTimeWithTZ = startTimeUtc.ConvertFromUtc(requestedTimeZone).RoundDown(period);
            DateTime endTimeWithTZ = endTimeUtc.ConvertFromUtc(requestedTimeZone).RoundDown(period);
            
            TradeBar currentBar = new TradeBar();
            while (true)
			{
                List<Markets.IBar> newBars = new List<Markets.IBar>();
                try
                {
                    newBars = (period.Days < 1) ? restClient.ListMinuteAggregatesAsync(symbol.Value, startTime, endTimeUtcForAPI).Result.Items.ToList() : restClient.ListDayAggregatesAsync(symbol.Value, startTime, endTimeUtc).Result.Items.ToList();
                }
                catch (Exception e)
                {
                    if (e.InnerException != null && e.InnerException.Message.Contains("ticks")) break;
                    throw;
                }

                if (newBars.Count == 0)
                {
                    if (currentBar.Symbol != Symbol.Empty) yield return currentBar;
                    break;
                }
                if (startTime == newBars.Last().Time)
                {
                    yield return currentBar;
                    break;
                }

                startTime = newBars.Last().Time;

                var result = newBars
                        .GroupBy(x => x.Time.RoundDown(period))
                        .Select(x => new TradeBar(
                            x.Key.ConvertFromUtc(requestedTimeZone),
                            symbol,
                            x.First().Open,
                            x.Max(t => t.High),
                            x.Min(t => t.Low),
                            x.Last().Close,
                            0,
                            period
                            ))
                         .ToList();
                if (currentBar.Symbol == Symbol.Empty) currentBar = result[0];
                if (currentBar.Time == result[0].Time)
                {
                    // Update the last QuoteBar
                    var newBar = result[0];
                    currentBar.High = currentBar.High > newBar.High ? currentBar.High : newBar.High;
                    currentBar.Low = currentBar.Low < newBar.Low ? currentBar.Low : newBar.Low;
                    currentBar.Close = newBar.Close;
                    result[0] = currentBar;
                }
                else
                {
                    result.Insert(0, currentBar);
                }
                if (result.Count == 1 && result[0].Time == currentBar.Time) continue;
                bool isEnd = false;
                for (int i = 0; i < result.Count-1; i++)
                {
                    if (result[i].Time < startTimeWithTZ) continue;
                    if (result[i].Time > endTimeWithTZ)
                    {
                        isEnd = true;
                        break;
                    }
                    yield return result[i];
                }
                currentBar = result[result.Count - 1];

                if (isEnd) break;
                if (currentBar.Time == endTimeWithTZ)
                {
                    yield return currentBar;
                    break;
                }
			}

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
		public IEnumerable<QuoteBar> DownloadQuoteBars(Symbol symbol, DateTime startTimeUtc, DateTime endTimeUtc, Resolution resolution, DateTimeZone requestedTimeZone)
		{
			var startUtc = startTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");
			var endUtc = endTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");

			var period = resolution.ToTimeSpan();

			DateTime startTime = startTimeUtc;
            DateTime startTimeWithTZ = startTimeUtc.ConvertFromUtc(requestedTimeZone).RoundDown(period);
            DateTime endTimeWithTZ = endTimeUtc.ConvertFromUtc(requestedTimeZone).RoundDown(period);
            long offsets = 0;
            
            QuoteBar currentBar = new QuoteBar();
			while (true)
			{
                
                List<IHistoricalQuote> asList = new List<IHistoricalQuote>();
                try
                {
                    var newBars = restClient.ListHistoricalQuotesAsync(symbol.Value, startTime, offsets).Result;
                    asList = newBars.Items.ToList();

                    if (asList.Count == 0)
                    {
                        startTime = new DateTime(startTime.Year, startTime.Month, startTime.Day);
                        startTime = startTime.AddDays(1);
                        if (startTime > endTimeUtc) break;
                        offsets = 0;
                        continue;
                    }
                    
                    // The first item in the HistoricalQuote is always 0 on BidPrice, so ignore it.
                    asList.RemoveAt(0);
                    if (asList.Count == 0) break;

                    offsets = asList.Last().TimeOffset;
                    if (DateTimeHelper.FromUnixTimeMilliseconds(offsets) < startTimeUtc) continue;
                }
                catch (Exception e)
                {
                    throw e;
                }
                var result = asList
                        .GroupBy(x => DateTimeHelper.FromUnixTimeMilliseconds(x.TimeOffset).RoundDown(period))
                        .Select(x => new QuoteBar(
                            x.Key.ConvertFromUtc(requestedTimeZone),
                            symbol,
                            new Bar(
                                x.First().BidPrice,
                                x.Max(t => t.BidPrice),
                                x.Min(t => t.BidPrice),
                                x.Last().BidPrice
                            ),
                            x.Last().BidSize,
                            new Bar(
                                x.First().AskPrice,
                                x.Max(t => t.AskPrice),
                                x.Min(t => t.AskPrice),
                                x.Last().AskPrice
                            ),
                            x.Last().AskPrice,
                            period
                            ))
                         .ToList();
                if (currentBar.Symbol == Symbol.Empty) currentBar = result[0];
                if (currentBar.Time == result[0].Time)
                {
                    // Update the last QuoteBar
                    var newBar = result[0];
                    currentBar.Bid.High = currentBar.Bid.High > newBar.Bid.High ? currentBar.Bid.High : newBar.Bid.High;
                    currentBar.Bid.Low = currentBar.Bid.Low < newBar.Bid.Low ? currentBar.Bid.Low : newBar.Bid.Low;
                    currentBar.Bid.Close = newBar.Bid.Close;

                    currentBar.Ask.High = currentBar.Ask.High > newBar.Ask.High ? currentBar.Ask.High : newBar.Ask.High;
                    currentBar.Ask.Low = currentBar.Ask.Low < newBar.Ask.Low ? currentBar.Ask.Low : newBar.Ask.Low;
                    currentBar.Ask.Close = newBar.Ask.Close;
                    result[0] = currentBar;
                }
                else
                {
                    result.Insert(0, currentBar);
                }
                if (result.Count == 1 && result[0].Time == currentBar.Time) continue;
                bool isEnd = false;
                for (int i = 0; i < result.Count-1; i++)
                {
                    if (startTimeWithTZ > result[i].Time) continue;
                    if (endTimeWithTZ < result[i].Time)
                    {
                        isEnd = true;
                        break;
                    }
                    yield return result[i];
                }
                currentBar = result[result.Count - 1];

                if (isEnd) break;
                if (currentBar.Time == endTimeWithTZ)
                {
                    yield return currentBar;
                    break;
                }
			}
            
		}

        /// <summary>
		/// Downloads a list of Ticks for the requested period
		/// </summary>
		/// <param name="symbol">The symbol</param>
		/// <param name="startTimeUtc">The starting time (UTC)</param>
		/// <param name="endTimeUtc">The ending time (UTC)</param>
		/// <param name="requestedTimeZone">The requested timezone for the data</param>
		/// <returns>The list of ticks</returns>
		public IEnumerable<Tick> DownloadTicks(Symbol symbol, DateTime startTimeUtc, DateTime endTimeUtc, DateTimeZone requestedTimeZone)
        {
            var startUtc = startTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var endUtc = endTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");

            DateTime startTime = startTimeUtc;
            DateTime startTimeWithTZ = startTimeUtc.ConvertFromUtc(requestedTimeZone);
            DateTime endTimeWithTZ = endTimeUtc.ConvertFromUtc(requestedTimeZone);
            long offsets = 0;
            
            Tick currentTick = new Tick();
            while (true)
            {
                List<IHistoricalQuote> asList = new List<IHistoricalQuote>();
                try
                {
                    var newBars = restClient.ListHistoricalQuotesAsync(symbol.Value, startTime, offsets).Result;
                    asList = newBars.Items.ToList();

                    // The first item in the HistoricalQuote is always 0 on BidPrice, so ignore it.
                    asList.RemoveAt(0);

                    offsets = asList.Last().TimeOffset;
                    if (DateTimeHelper.FromUnixTimeMilliseconds(offsets) < startTimeUtc) continue;
                }
                catch (Exception e)
                {
                    if (e.InnerException != null && e.InnerException.Message.Contains("ticks"))
                    {
                        startTime = new DateTime(startTime.Year, startTime.Month, startTime.Day);
                        startTime = startTime.AddDays(1);
                        if (startTime > endTimeUtc) break;
                        offsets = 0;
                    }
                    else
                        throw;
                }
                bool isEnd = false;
                for (int i = 0; i < asList.Count; i++)
                {
                    var currentTime = DateTimeHelper.FromUnixTimeMilliseconds(asList[i].TimeOffset).ConvertFromUtc(requestedTimeZone);
                    if (startTimeWithTZ > currentTime) continue;
                    if (endTimeWithTZ < currentTime)
                    {
                        isEnd = true;
                        break;
                    }
                    currentTick.Time = currentTime;
                    currentTick.Symbol = symbol;
                    currentTick.BidPrice = asList[i].BidPrice;
                    currentTick.AskPrice = asList[i].AskPrice;
                    yield return currentTick;
                }
                asList.Clear();
                if (isEnd) break;
            }

        }

        /// <summary>
        /// Get the next ticks from the live trading data queue
        /// </summary>
        /// <returns>IEnumerable list of ticks since the last update.</returns>
        public IEnumerable<BaseData> GetNextTicks()
		{
			lock (Ticks)
			{
				var copy = Ticks.ToArray();
				Ticks.Clear();
				return copy;
			}
		}

		/// <summary>
		/// Adds the specified symbols to the subscription
		/// </summary>
		/// <param name="job">Job we're subscribing for:</param>
		/// <param name="symbols">The symbols to be added keyed by SecurityType</param>
		public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
		{
			lock (LockerSubscriptions)
			{
				var symbolsToSubscribe = (from symbol in symbols
										  where !SubscribedSymbols.Contains(symbol) && CanSubscribe(symbol)
										  select symbol).ToList();
				if (symbolsToSubscribe.Count == 0)
					return;

				Log.Trace("AlpacaBrokerage.Subscribe(): {0}", string.Join(",", symbolsToSubscribe.Select(x => x.Value)));

				// Alpaca does not allow more than a few rate streaming sessions,
				// so we only use a single session for all currently subscribed symbols
				symbolsToSubscribe = symbolsToSubscribe.Union(SubscribedSymbols.ToList()).ToList();

				SubscribedSymbols = symbolsToSubscribe.ToHashSet();

				ProcessSubscriptionRequest();
			}
		}

		/// <summary>
		/// Removes the specified symbols from the subscription
		/// </summary>
		/// <param name="job">Job we're processing.</param>
		/// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
		public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
		{
			lock (LockerSubscriptions)
			{
				var symbolsToUnsubscribe = (from symbol in symbols
											where SubscribedSymbols.Contains(symbol)
											select symbol).ToList();
				if (symbolsToUnsubscribe.Count == 0)
					return;

				Log.Trace("AlpacaBrokerage.Unsubscribe(): {0}", string.Join(",", symbolsToUnsubscribe.Select(x => x.Value)));

				// Alpaca does not allow more than a few rate streaming sessions,
				// so we only use a single session for all currently subscribed symbols
				var symbolsToSubscribe = SubscribedSymbols.ToList().Where(x => !symbolsToUnsubscribe.Contains(x)).ToList();

				SubscribedSymbols = symbolsToSubscribe.ToHashSet();

				ProcessSubscriptionRequest();
			}
		}

		/// <summary>
		/// Groups multiple subscribe/unsubscribe calls to avoid closing and reopening the streaming session on each call
		/// </summary>
		private void ProcessSubscriptionRequest()
		{
			if (_subscriptionsPending) return;

			_lastSubscribeRequestUtcTime = DateTime.UtcNow;
			_subscriptionsPending = true;

			Task.Run(() =>
			{
				while (true)
				{
					DateTime requestTime;
					List<Symbol> symbolsToSubscribe;
					lock (LockerSubscriptions)
					{
						requestTime = _lastSubscribeRequestUtcTime.Add(SubscribeDelay);
						symbolsToSubscribe = SubscribedSymbols.ToList();
					}

					if (DateTime.UtcNow > requestTime)
					{
						// restart streaming session
						SubscribeSymbols(symbolsToSubscribe);

						lock (LockerSubscriptions)
						{
							_lastSubscribeRequestUtcTime = DateTime.UtcNow;
							if (SubscribedSymbols.Count == symbolsToSubscribe.Count)
							{
								// no more subscriptions pending, task finished
								_subscriptionsPending = false;
                                break;
							}
						}
					}

					Thread.Sleep(200);
				}
			});
		}

		/// <summary>
		/// Returns true if this brokerage supports the specified symbol
		/// </summary>
		private static bool CanSubscribe(Symbol symbol)
		{
			// ignore unsupported security types
			if (symbol.ID.SecurityType != SecurityType.Equity)
				return false;

			return true;
		}

		/// <summary>
		/// Subscribes to the requested symbols (using a single streaming session)
		/// </summary>
		/// <param name="symbolsToSubscribe">The list of symbols to subscribe</param>
		protected void SubscribeSymbols(List<Symbol> symbolsToSubscribe)
		{
			var instruments = symbolsToSubscribe
				.Select(symbol => symbol.Value)
				.ToList();

			StopPricingStream();

			if (instruments.Count > 0)
			{
				StartPricingStream(instruments);
			}
		}


		/// <summary>
		/// Converts an Alpaca order into a LEAN order.
		/// </summary>
		private Order ConvertOrder(IOrder order)
		{
			var type = order.OrderType;

			Order qcOrder;
			switch (type)
			{
				case Markets.OrderType.Stop:
					qcOrder = new StopMarketOrder
					{
						StopPrice = order.StopPrice.Value
					};
					break;

				case Markets.OrderType.Limit:
					qcOrder = new LimitOrder
					{
						LimitPrice = order.LimitPrice.Value
					};
					break;

				case Markets.OrderType.StopLimit:
					qcOrder = new StopLimitOrder
					{
						Price = order.StopPrice.Value,
						LimitPrice = order.LimitPrice.Value
					};
					break;

				case Markets.OrderType.Market:
					qcOrder = new MarketOrder();
					break;

				default:
					throw new NotSupportedException(
						"An existing " + type + " working order was found and is currently unsupported. Please manually cancel the order before restarting the algorithm.");
			}

			var instrument = order.Symbol;
			var id = order.OrderId.ToString();
			
			qcOrder.Symbol = Symbol.Create(instrument, SecurityType.Equity, Market.USA);
            qcOrder.Time = order.SubmittedAt.Value;
			qcOrder.Quantity = order.Quantity;
			qcOrder.Status = Orders.OrderStatus.None;
			qcOrder.BrokerId.Add(id);

			var orderByBrokerageId = OrderProvider.GetOrderByBrokerageId(id);
			if (orderByBrokerageId != null)
			{
				qcOrder.Id = orderByBrokerageId.Id;
			}

			if (order.ExpiredAt != null)
			{
				qcOrder.Properties.TimeInForce = Orders.TimeInForce.GoodTilDate(order.ExpiredAt.Value);
			}

			return qcOrder;
		}

		/// <summary>
		/// Converts an Alpaca position into a LEAN holding.
		/// </summary>
		private Holding ConvertHolding(Markets.IPosition position)
		{
			var securityType = SecurityType.Equity;
			var symbol = Symbol.Create(position.Symbol, securityType, Market.USA);

			return new Holding
			{
				Symbol = symbol,
				Type = securityType,
				AveragePrice = position.AverageEntryPrice,
				ConversionRate = 1.0m,
				CurrencySymbol = "$",
				Quantity = position.Quantity
			};
		}
		
		/// <summary>
		/// Returns the websocket client for alpaca
		/// </summary>
		internal SockClient GetSockClient()
		{
			var sockClient = new SockClient(AccountKeyId, SecretKey, BaseUrl);
			return sockClient;
		}

		/// <summary>
		/// Returns the polygon client for alpaca
		/// </summary>
		internal NatsClient GetNatsClient()
		{
            var isStaging = BaseUrl.Contains("staging") ? true : false;

            var natsClient = new NatsClient(AccountKeyId, isStaging);
			return natsClient;
		}
	}
}

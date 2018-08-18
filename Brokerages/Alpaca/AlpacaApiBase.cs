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
		/// The symbol mapper
		/// </summary>
		//protected OandaSymbolMapper SymbolMapper;

		/// <summary>
		/// The order provider
		/// </summary>
		protected IOrderProvider OrderProvider;

		/// <summary>
		/// The security provider
		/// </summary>
		protected ISecurityProvider SecurityProvider;

		/// <summary>
		/// The Alpaca enviroment
		/// </summary>
		protected Environment Environment;

		/// <summary>
		/// The Alpaca access token
		/// </summary>
		protected string AccessToken;

		/// <summary>
		/// The Alpaca account ID
		/// </summary>
		protected string AccountId;

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
		/// <param name="environment">The Alpaca environment (Trade or Practice)</param>
		/// <param name="accessToken">The Alpaca access token (can be the user's personal access token or the access token obtained with OAuth by QC on behalf of the user)</param>
		/// <param name="accountId">The account identifier.</param>
		/// <param name="baseUrl">The Alpaca base url</param>
		public AlpacaApiBase(IOrderProvider orderProvider, ISecurityProvider securityProvider, Environment environment, string accessToken, string accountId, string baseUrl)
			: base("Alpaca Brokerage")
		{
			OrderProvider = orderProvider;
			SecurityProvider = securityProvider;
			Environment = environment;
			AccessToken = accessToken;
			AccountId = accountId;
			BaseUrl = baseUrl;

			restClient = new Markets.RestClient(accountId, accessToken, baseUrl);
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
            Log.Trace(response.ToList().ToString());
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
			order.PriceCurrency = "$";

			lock (Locker)
			{
				var apOrder = GenerateAndPlaceOrder(order);
				order.BrokerId.Add(apOrder.OrderId.ToString());

				// Market orders are special, due to the callback not being triggered always,
				// if the order was Filled/PartiallyFilled, find fill quantity and price and inform the user
				if (order.Type == Orders.OrderType.Market)
				{
					marketOrderFillPrice = apOrder.AverageFillPrice.Value;

					marketOrderFillQuantity = Convert.ToInt32(apOrder.FilledQuantity);
					

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
				OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Alpaca Cancel Order Event") { Status = Orders.OrderStatus.Canceled });
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
			
			Order order;
			lock (Locker)
			{
				order = OrderProvider.GetOrderByBrokerageId(trade.Order.OrderId.ToString());
			}
			if (order != null && trade.Order.OrderStatus == Markets.OrderStatus.Filled)
			{
				Orders.OrderStatus status;
				// Market orders are special: if the order was not in 'PartiallyFilledMarketOrders', means
				// we already sent the fill event with OrderStatus.Filled, else it means we already informed the user
				// of a partiall fill, or didn't inform the user, so we need to do it now
				if (order.Type != Orders.OrderType.Market || PendingFilledMarketOrders.TryRemove(order.Id, out status))
				{
					order.PriceCurrency = SecurityProvider.GetSecurity(order.Symbol).SymbolProperties.QuoteCurrency;

					const int orderFee = 0;
					OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Alpaca Fill Event")
					{
						Status = Orders.OrderStatus.Filled,
						FillPrice = trade.Price.Value,
						FillQuantity = Convert.ToInt32(trade.Quantity)
					});
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
			var symbol = quote.Symbol;
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
			var startUtc = startTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");
			var endUtc = endTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");

			var period = resolution.ToTimeSpan();

			// No seconds resolution
			if (period.Seconds < 60) 
			{
				yield return null;
			}

			List<Markets.IBar> bars = new List<Markets.IBar>();
			DateTime startTime = startTimeUtc;

			while (true)
			{
				var newBars = (period.Days < 1)? restClient.ListMinuteAggregatesAsync(symbol.Value, startTime, endTimeUtc).Result : restClient.ListDayAggregatesAsync(symbol.Value, startTime, endTimeUtc).Result;
				if (startTime == newBars.Items.Last().Time)
					break;
				List<Markets.IBar> asList = newBars.Items.ToList<Markets.IBar>();
				bars.AddRange(asList);
				startTime = asList.Last().Time;
			}

			var convertedCandles = to_larger_timeframe(bars, period);

			foreach (var candle in convertedCandles)
			{
				var time = candle.Time;
				if (time > endTimeUtc)
					break;

				yield return new TradeBar(
					time.ConvertFromUtc(requestedTimeZone),
					symbol,
					candle.Open,
					candle.High,
					candle.Low,
					candle.Close,
					0,
					period);
			}
		}

		// Convert 1m candles to the required timeframe
		private List<Markets.IBar> to_larger_timeframe(List<Markets.IBar> bars_to_convert, TimeSpan time)
		{
			var bars_converted = new List<Markets.IBar>();
			long current_tick_interval = -1;
			DateTime boundary_adjusted_time = default(DateTime);
			decimal current_bar_open = default(decimal);
			decimal current_bar_high = default(decimal);
			decimal current_bar_low = default(decimal);
			decimal current_bar_close = default(decimal);

			if (bars_to_convert.Count == 0)
				return bars_converted;

			foreach (var bar in bars_to_convert)
			{
				var this_tick_interval = bar.Time.Ticks / time.Ticks;
				if (this_tick_interval != current_tick_interval)
				{
					if (current_tick_interval != -1)
					{
						JsonBar barTemp = new JsonBar
						{
							Time = boundary_adjusted_time,
							Open = current_bar_open,
							High = current_bar_high,
							Low = current_bar_low,
							Close = current_bar_close
						};
						bars_converted.Add(barTemp);
					}
					current_tick_interval = this_tick_interval;
					boundary_adjusted_time = new DateTime(current_tick_interval * time.Ticks);
					current_bar_open = bar.Open;
					current_bar_high = bar.High;
					current_bar_low = bar.Low;
					current_bar_close = bar.Close;
				}
				else
				{
					current_bar_high = bar.High > current_bar_high ? bar.High : current_bar_high;
					current_bar_low = bar.Low < current_bar_low ? bar.Low : current_bar_low;
					current_bar_close = bar.Close;
				}
			}
			// Add the final bar
			JsonBar nbar = new JsonBar
			{
				Time = boundary_adjusted_time,
				Open = current_bar_open,
				High = current_bar_high,
				Low = current_bar_low,
				Close = current_bar_close
			};
			bars_converted.Add(nbar);
			return bars_converted;
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

			// No seconds resolution
			if (period.Seconds < 60)
			{
				yield return null;
			}

			List<Markets.IHistoricalQuote> bars = new List<Markets.IHistoricalQuote>();
			DateTime startTime = startTimeUtc;

			while (true)
			{
				var newBars = restClient.ListHistoricalQuotesAsync(symbol.Value, startTime).Result;
				var asList = newBars.Items.ToList();
				if (startTime == DateTimeHelper.FromUnixTimeMilliseconds(asList.Last().TimeOffset))
					break;
				bars.AddRange(asList);
				startTime = DateTimeHelper.FromUnixTimeMilliseconds(asList.Last().TimeOffset);
			}

			var bidaskList = to_larger_timeframe(bars, period);

			foreach (var bidask in bidaskList)
			{
				var bidBar = bidask.First();
				var askBar = bidask.Last();
				var time = bidBar.Time;
				if (time > endTimeUtc)
					break;

				yield return new QuoteBar(
					time.ConvertFromUtc(requestedTimeZone),
					symbol,
					new Bar(
						bidBar.Open,
						bidBar.High,
						bidBar.Low,
						bidBar.Close
					),
					0,
					new Bar(
						askBar.Open,
						askBar.High,
						askBar.Low,
						askBar.Close
					),
					0,
					period);
			}
		}

		private List<List<Markets.IBar>> to_larger_timeframe(List<Markets.IHistoricalQuote> bars, TimeSpan time)
		{
			var bars_converted = new List<List<Markets.IBar>>();
			long current_tick_interval = -1;
			DateTime boundary_adjusted_time = default(DateTime);
			decimal current_bar_open = default(decimal);
			decimal current_bar_high = default(decimal);
			decimal current_bar_low = default(decimal);
			decimal current_bar_close = default(decimal);
			decimal current_bar_open_ask = default(decimal);
			decimal current_bar_high_ask = default(decimal);
			decimal current_bar_low_ask = default(decimal);
			decimal current_bar_close_ask = default(decimal);

			if (bars.Count == 0)
				return null;

			foreach (var bar in bars)
			{
				var this_tick_interval = DateTimeHelper.FromUnixTimeMilliseconds(bar.TimeOffset).Ticks / time.Ticks;
				if (this_tick_interval != current_tick_interval)
				{
					if (current_tick_interval != -1)
					{
						JsonBar barTemp = new JsonBar
						{
							Time = boundary_adjusted_time,
							Open = current_bar_open,
							High = current_bar_high,
							Low = current_bar_low,
							Close = current_bar_close
						};
						JsonBar barTemp1 = new JsonBar
						{
							Time = boundary_adjusted_time,
							Open = current_bar_open_ask,
							High = current_bar_high_ask,
							Low = current_bar_low_ask,
							Close = current_bar_close_ask
						};
						bars_converted.Add(new List<Markets.IBar>() { barTemp, barTemp1 });
					}
					current_tick_interval = this_tick_interval;
					boundary_adjusted_time = new DateTime(current_tick_interval * time.Ticks);
					current_bar_open = current_bar_high = current_bar_low = current_bar_close = bar.BidPrice;
					current_bar_open_ask = current_bar_high_ask = current_bar_low_ask = current_bar_close_ask = bar.AskPrice;
				}
				else
				{
					current_bar_high = bar.BidPrice > current_bar_high ? bar.BidPrice : current_bar_high;
					current_bar_low = bar.BidPrice < current_bar_low ? bar.BidPrice : current_bar_low;
					current_bar_close = bar.BidPrice;
					current_bar_high_ask = bar.AskPrice > current_bar_high_ask ? bar.AskPrice : current_bar_high_ask;
					current_bar_low_ask = bar.AskPrice < current_bar_low_ask ? bar.AskPrice : current_bar_low_ask;
					current_bar_close_ask = bar.AskPrice;
				}
			}
			// Add the final bar
			JsonBar nbar1 = new JsonBar
			{
				Time = boundary_adjusted_time,
				Open = current_bar_open,
				High = current_bar_high,
				Low = current_bar_low,
				Close = current_bar_close
			};
			JsonBar nbar2 = new JsonBar
			{
				Time = boundary_adjusted_time,
				Open = current_bar_open_ask,
				High = current_bar_high_ask,
				Low = current_bar_low_ask,
				Close = current_bar_close_ask
			};
			bars_converted.Add(new List<Markets.IBar>() { nbar1, nbar2});
			return bars_converted;
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
                                Console.WriteLine("subscription ended");
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
			var sockClient = new SockClient(AccountId, AccessToken, BaseUrl);
			return sockClient;
		}

		/// <summary>
		/// Returns the polygon client for alpaca
		/// </summary>
		internal NatsClient GetNatsClient()
		{
            var accId = BaseUrl.Contains("staging") ? AccountId + "-staging" : AccountId;

            var natsClient = new NatsClient(accId);
			return natsClient;
		}
	}
}

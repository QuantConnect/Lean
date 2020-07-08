using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Custom.Fred;
using QuantConnect.Data.Custom.Tiingo;
using QuantConnect.Data.Custom.TradingEconomics;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories;
using QuantConnect.Packets;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    public class LiveDataAggregator
    {
        private readonly LiveNodePacket _job;
        private readonly BaseDataExchange _exchange;
        private readonly BaseDataExchange _customExchange;
        private readonly ILiveDataProvider _liveDataProvider;
        private readonly IDataChannelProvider _channelProvider;
        private readonly IDataProvider _dataProvider;
        private readonly ITimeProvider _timeProvider;
        private readonly ITimeProvider _frontierTimeProvider;

        public Subscription Subscription { get; set; }
        protected Func<Subscription, TimeZoneOffsetProvider, SecurityExchangeHours, BaseData, bool> UpdateSubscriptionRealTimePrice;

        public LiveDataAggregator(
            LiveNodePacket job,
            BaseDataExchange exchange,
            BaseDataExchange customExchange,
            ILiveDataProvider liveDataProvider,
            IDataChannelProvider channelProvider,
            IDataProvider dataProvider,
            ITimeProvider timeProvider,
            ITimeProvider frontierTimeProvider,
            Func<Subscription, TimeZoneOffsetProvider, SecurityExchangeHours, BaseData, bool> updateSubscriptionRealTimePrice)
        {
            _job = job;
            _exchange = exchange;
            _customExchange = customExchange;
            _liveDataProvider = liveDataProvider;
            _channelProvider = channelProvider;
            _dataProvider = dataProvider;
            _timeProvider = timeProvider;
            _frontierTimeProvider = frontierTimeProvider;

            UpdateSubscriptionRealTimePrice = updateSubscriptionRealTimePrice;
        }

        public IEnumerator<BaseData> CreateEnumerator(
            SubscriptionRequest request,
            TimeZoneOffsetProvider timeZoneOffsetProvider)
        {
            // Bail out of this method early if we don't have a wrapper instance.
            // We only setup the aggregation for wrapped DataQueueHandlers.
            if (_liveDataProvider.GetType() != typeof(DataQueueHandlerLiveDataProvider))
            {
                var enqueueableEnumerator = new EnqueueableEnumerator<BaseData>();
                _exchange.AddDataHandler(request.Configuration.Symbol, data => NoAggregationDataHandler(enqueueableEnumerator, request, timeZoneOffsetProvider, data));
                return enqueueableEnumerator;
            }

            if (!_channelProvider.ShouldStreamSubscription(_job, request.Configuration))
            {
                if (!Quandl.IsAuthCodeSet)
                {
                    // we're not using the SubscriptionDataReader, so be sure to set the auth token here
                    Quandl.SetAuthCode(Config.Get("quandl-auth-token"));
                }

                if (!Tiingo.IsAuthCodeSet)
                {
                    // we're not using the SubscriptionDataReader, so be sure to set the auth token here
                    Tiingo.SetAuthCode(Config.Get("tiingo-auth-token"));
                }

                if (!USEnergyAPI.IsAuthCodeSet)
                {
                    // we're not using the SubscriptionDataReader, so be sure to set the auth token here
                    USEnergyAPI.SetAuthCode(Config.Get("us-energy-information-auth-token"));
                }

                if (!FredApi.IsAuthCodeSet)
                {
                    // we're not using the SubscriptionDataReader, so be sure to set the auth token here
                    FredApi.SetAuthCode(Config.Get("fred-auth-token"));
                }

                if (!TradingEconomicsCalendar.IsAuthCodeSet)
                {
                    // we're not using the SubscriptionDataReader, so be sure to set the auth token here
                    TradingEconomicsCalendar.SetAuthCode(Config.Get("trading-economics-auth-token"));
                }

                var factory = new LiveCustomDataSubscriptionEnumeratorFactory(_timeProvider);
                var enumeratorStack = factory.CreateEnumerator(request, _dataProvider);

                _customExchange.AddEnumerator(request.Configuration.Symbol, enumeratorStack);

                var enqueable = new EnqueueableEnumerator<BaseData>();
                _customExchange.SetDataHandler(request.Configuration.Symbol, data =>
                {
                    enqueable.Enqueue(data);

                    Subscription.OnNewDataAvailable();

                    UpdateSubscriptionRealTimePrice(
                        Subscription,
                        timeZoneOffsetProvider,
                        request.Security.Exchange.Hours,
                        data);
                });

                return enqueable;
            }

            // this enumerator allows the exchange to pump ticks into the 'back' of the enumerator,
            // and the time sync loop can pull aggregated trade bars off the front
            switch (request.Configuration.Type.Name)
            {
                case nameof(QuoteBar):
                    var quoteBarAggregator = new QuoteBarBuilderEnumerator(
                        request.Configuration.Increment,
                        request.Security.Exchange.TimeZone,
                        _timeProvider,
                        true,
                        (sender, args) => Subscription.OnNewDataAvailable());

                    _exchange.AddDataHandler(request.Configuration.Symbol, data =>
                        {
                            var tick = data as Tick;

                            if (tick?.TickType == TickType.Quote && !tick.Suspicious)
                            {
                                quoteBarAggregator.ProcessData(tick);

                                UpdateSubscriptionRealTimePrice(
                                    Subscription,
                                    timeZoneOffsetProvider,
                                    request.Security.Exchange.Hours,
                                    data);
                            }
                        });
                    return quoteBarAggregator;

                case nameof(TradeBar):
                    var tradeBarAggregator = new TradeBarBuilderEnumerator(
                        request.Configuration.Increment,
                        request.Security.Exchange.TimeZone,
                        _timeProvider,
                        true,
                        (sender, args) => Subscription.OnNewDataAvailable());

                    var auxDataEnumerator = new LiveAuxiliaryDataEnumerator(
                        request.Security.Exchange.TimeZone,
                        _timeProvider);

                    _exchange.AddDataHandler(
                        request.Configuration.Symbol,
                        data =>
                        {
                            if (data.DataType == MarketDataType.Auxiliary)
                            {
                                auxDataEnumerator.Enqueue(data);

                                Subscription.OnNewDataAvailable();
                            }
                            else
                            {
                                var tick = data as Tick;
                                if (tick?.TickType == TickType.Trade && !tick.Suspicious)
                                {
                                    tradeBarAggregator.ProcessData(tick);

                                    UpdateSubscriptionRealTimePrice(
                                        Subscription,
                                        timeZoneOffsetProvider,
                                        request.Security.Exchange.Hours,
                                        data);
                                }
                            }
                        });

                    return request.Configuration.SecurityType == SecurityType.Equity
                        ? (IEnumerator<BaseData>)new LiveEquityDataSynchronizingEnumerator(_frontierTimeProvider, request.Security.Exchange.TimeZone, auxDataEnumerator, tradeBarAggregator)
                        : tradeBarAggregator;

                case nameof(OpenInterest):
                    var oiAggregator = new OpenInterestEnumerator(
                        request.Configuration.Increment,
                        request.Security.Exchange.TimeZone,
                        _timeProvider,
                        true,
                        (sender, args) => Subscription.OnNewDataAvailable());

                    _exchange.AddDataHandler(request.Configuration.Symbol, data =>
                        {
                            var tick = data as Tick;

                            if (tick?.TickType == TickType.OpenInterest && !tick.Suspicious)
                            {
                                oiAggregator.ProcessData(tick);
                            }
                        });
                    return oiAggregator;

                case nameof(Tick):
                default:
                    // tick or streaming custom data subscriptions can pass right through
                    var enqueueableEnumerator = new EnqueueableEnumerator<BaseData>();
                    _exchange.AddDataHandler(request.Configuration.Symbol, data => NoAggregationDataHandler(
                        enqueueableEnumerator,
                        request,
                        timeZoneOffsetProvider,
                        data));

                    return enqueueableEnumerator;
            }
        }

        private void NoAggregationDataHandler(EnqueueableEnumerator<BaseData> enumerator, SubscriptionRequest request, TimeZoneOffsetProvider timeZoneOffsetProvider, BaseData data)
        {
            var tick = data as Tick;
            if (tick == null)
            {
                enumerator.Enqueue(data);
                Subscription.OnNewDataAvailable();
                return;
            }

            if (tick.TickType != request.Configuration.TickType)
            {
                return;
            }

            enumerator.Enqueue(data);
            Subscription.OnNewDataAvailable();

            if (tick.TickType != TickType.OpenInterest)
            {
                UpdateSubscriptionRealTimePrice(
                    Subscription,
                    timeZoneOffsetProvider,
                    request.Security.Exchange.Hours,
                    data);
            }
        }
    }
}

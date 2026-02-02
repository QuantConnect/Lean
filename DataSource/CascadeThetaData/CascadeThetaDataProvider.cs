/*
 * Cascade Labs - Modified ThetaData Provider
 * Based on QuantConnect.Lean.DataSource.ThetaData
 *
 * Key modifications:
 * - Uses CascadeThetaDataRestClient with Bearer token auth
 * - Removes QuantConnect subscription validation
 * - Adds local data caching
 */

using NodaTime;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Lean.DataSource.CascadeThetaData;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Rest;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Common;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Interfaces;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.SubscriptionPlans;

using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Lean.DataSource.CascadeThetaData
{
    /// <summary>
    /// Cascade Labs ThetaData provider with Bearer auth and caching
    /// </summary>
    public class CascadeThetaDataProvider : SynchronizingHistoryProvider, IDataQueueHandler
    {
        private static readonly DateTimeZone TimeZoneThetaData = TimeZones.NewYork;

        private IDataAggregator? _dataAggregator;
        private ThetaDataSymbolMapper? _symbolMapper;
        private CascadeThetaDataRestClient? _restApiClient;
        private ISubscriptionPlan? _userSubscriptionPlan;
        private bool _initialized;

        // Warning flags
        private volatile bool _invalidSecurityTypeWarningFired;
        private volatile bool _invalidResolutionWarningFired;
        private volatile bool _invalidStartDateWarningFired;

        public bool IsConnected => true; // REST-only, always "connected"

        public CascadeThetaDataProvider() : this(Config.Get("thetadata-subscription-plan", "Pro"))
        {
        }

        public CascadeThetaDataProvider(string pricePlan)
        {
            if (!string.IsNullOrWhiteSpace(pricePlan))
            {
                Initialize(pricePlan);
            }
        }

        private void Initialize(string pricePlan)
        {
            if (_initialized) return;

            Log.Trace($"CascadeThetaDataProvider: Initializing with plan '{pricePlan}'");

            _dataAggregator = Composer.Instance.GetPart<IDataAggregator>();
            if (_dataAggregator == null)
            {
                _dataAggregator = Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(
                    Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager"),
                    forceTypeNameOnExisting: false);
            }

            _userSubscriptionPlan = GetUserSubscriptionPlan(pricePlan);
            _restApiClient = new CascadeThetaDataRestClient(_userSubscriptionPlan.RateGate!);
            _symbolMapper = new ThetaDataSymbolMapper();

            _initialized = true;

            Log.Trace($"CascadeThetaDataProvider: Initialized successfully");

            // NOTE: We intentionally skip ValidateSubscription() from the original
            // since we're using our own deployment and don't need QC validation
        }

        private ISubscriptionPlan GetUserSubscriptionPlan(string pricePlan)
        {
            if (string.IsNullOrEmpty(pricePlan))
            {
                pricePlan = "Pro"; // Default to Pro for cascadelabs
            }

            // Map to subscription plan (simplified, always use Pro features)
            return new ProSubscriptionPlan();
        }

        public void SetJob(LiveNodePacket job)
        {
            if (_initialized) return;

            if (job.BrokerageData.TryGetValue("thetadata-subscription-plan", out var pricePlan))
            {
                Initialize(pricePlan);
            }
            else
            {
                Initialize("Pro");
            }
        }

        public override void Initialize(HistoryProviderInitializeParameters parameters)
        {
            // No additional initialization needed
        }

        public override IEnumerable<Slice>? GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
        {
            var subscriptions = new List<Subscription>();

            foreach (var request in requests)
            {
                var history = GetHistory(request);
                if (history == null) continue;

                var subscription = CreateSubscription(request, history);
                if (!subscription.MoveNext()) continue;

                subscriptions.Add(subscription);
            }

            if (subscriptions.Count == 0) return null;

            return CreateSliceEnumerableFromSubscriptions(subscriptions, sliceTimeZone);
        }

        public IEnumerable<BaseData>? GetHistory(HistoryRequest historyRequest)
        {
            if (_restApiClient == null || _symbolMapper == null)
            {
                Log.Error("CascadeThetaDataProvider: Not initialized");
                return null;
            }

            // Validate request
            if (!CanSubscribe(historyRequest.Symbol))
            {
                if (!_invalidSecurityTypeWarningFired)
                {
                    _invalidSecurityTypeWarningFired = true;
                    Log.Trace($"CascadeThetaDataProvider: Unsupported security type '{historyRequest.Symbol.SecurityType}'");
                }
                return null;
            }

            if (!_userSubscriptionPlan!.AccessibleResolutions.Contains(historyRequest.Resolution))
            {
                if (!_invalidResolutionWarningFired)
                {
                    _invalidResolutionWarningFired = true;
                    Log.Trace($"CascadeThetaDataProvider: Resolution {historyRequest.Resolution} not supported by subscription");
                }
                return null;
            }

            var startDateTimeUtc = historyRequest.StartTimeUtc;
            if (_userSubscriptionPlan.FirstAccessDate.Date > historyRequest.StartTimeUtc.Date)
            {
                if (!_invalidStartDateWarningFired)
                {
                    _invalidStartDateWarningFired = true;
                    Log.Trace($"CascadeThetaDataProvider: Adjusting start date to subscription limit");
                }
                startDateTimeUtc = _userSubscriptionPlan.FirstAccessDate.Date;

                if (startDateTimeUtc >= historyRequest.EndTimeUtc)
                {
                    return null;
                }
            }

            // Build query parameters
            var queryParameters = new Dictionary<string, string>();
            queryParameters = GetSymbolQueryParameters(queryParameters, historyRequest.Symbol);

            var startDateTimeLocal = startDateTimeUtc.ConvertFromUtc(TimeZoneThetaData);
            var endDateTimeLocal = historyRequest.EndTimeUtc.ConvertFromUtc(TimeZoneThetaData);

            queryParameters[RequestParameters.StartDate] = startDateTimeLocal.ConvertToThetaDataDateFormat();
            queryParameters[RequestParameters.EndDate] = endDateTimeLocal.ConvertToThetaDataDateFormat();
            queryParameters["start_time"] = "0";

            var endpoint = GetResourceUrl(historyRequest.Symbol.SecurityType, historyRequest.TickType, historyRequest.Resolution);
            var symbolExchangeTimeZone = historyRequest.Symbol.GetSymbolExchangeTimeZone();

            // Fetch data
            IEnumerable<BaseData>? history = null;

            switch (historyRequest.Resolution)
            {
                case Resolution.Tick:
                    history = GetTickHistoryData(endpoint, queryParameters, historyRequest.Symbol,
                        historyRequest.TickType, startDateTimeUtc, historyRequest.EndTimeUtc, symbolExchangeTimeZone);
                    break;

                case Resolution.Second:
                case Resolution.Minute:
                case Resolution.Hour:
                    history = GetIntradayHistoryData(endpoint, queryParameters, historyRequest.Symbol,
                        historyRequest.Resolution, historyRequest.TickType, symbolExchangeTimeZone);
                    break;

                case Resolution.Daily:
                    history = GetDailyHistoryData(endpoint, queryParameters, historyRequest.Symbol,
                        historyRequest.Resolution, historyRequest.TickType, symbolExchangeTimeZone);
                    break;
            }

            if (history == null) return null;

            // Filter and return (matches upstream ThetaDataHistoryProvider behavior)
            return FilterHistory(history, historyRequest, startDateTimeLocal, endDateTimeLocal);
        }

        private bool CanSubscribe(Symbol symbol)
        {
            return
                symbol.Value.IndexOfInvariant("universe", true) == -1 &&
                !symbol.IsCanonical() &&
                _symbolMapper!.SupportedSecurityType.Contains(symbol.SecurityType);
        }

        private Dictionary<string, string> GetSymbolQueryParameters(Dictionary<string, string> queryParameters, Symbol symbol)
        {
            var ticker = _symbolMapper!.GetBrokerageSymbol(symbol);

            switch (symbol.SecurityType)
            {
                case SecurityType.Index:
                case SecurityType.Equity:
                    queryParameters["root"] = ticker;
                    break;
                case SecurityType.Option:
                case SecurityType.IndexOption:
                    var tickerOption = ticker.Split(',');
                    queryParameters["root"] = tickerOption[0];
                    queryParameters["exp"] = tickerOption[1];
                    queryParameters["strike"] = tickerOption[2];
                    queryParameters["right"] = tickerOption[3];
                    break;
                default:
                    throw new NotImplementedException($"Security type '{symbol.SecurityType}' not implemented");
            }

            return queryParameters;
        }

        private string GetResourceUrl(SecurityType securityType, TickType tickType, Resolution resolution)
        {
            return tickType switch
            {
                TickType.Trade => GetTradeResourceUrl(securityType, resolution),
                TickType.Quote => GetQuoteResourceUrl(securityType, resolution),
                TickType.OpenInterest when securityType == SecurityType.Option => "hist/option/open_interest",
                _ => throw new ArgumentException($"Invalid tick type: {tickType}")
            };
        }

        private string GetTradeResourceUrl(SecurityType securityType, Resolution resolution)
        {
            return resolution switch
            {
                Resolution.Tick => securityType switch
                {
                    SecurityType.Index => "hist/index/price",
                    SecurityType.Equity => "hist/stock/trade",
                    SecurityType.IndexOption or SecurityType.Option => "hist/option/trade",
                    _ => throw new NotImplementedException()
                },
                Resolution.Second or Resolution.Minute or Resolution.Hour => securityType switch
                {
                    SecurityType.Index => "hist/index/price",
                    SecurityType.Equity => "hist/stock/ohlc",
                    SecurityType.IndexOption or SecurityType.Option => "hist/option/ohlc",
                    _ => throw new NotImplementedException()
                },
                Resolution.Daily => securityType switch
                {
                    SecurityType.Index => "hist/index/eod",
                    SecurityType.Equity => "hist/stock/eod",
                    SecurityType.IndexOption or SecurityType.Option => "hist/option/eod",
                    _ => throw new NotImplementedException()
                },
                _ => throw new NotImplementedException()
            };
        }

        private string GetQuoteResourceUrl(SecurityType securityType, Resolution resolution)
        {
            return resolution switch
            {
                Resolution.Tick => securityType switch
                {
                    SecurityType.Equity => "hist/stock/quote",
                    SecurityType.IndexOption or SecurityType.Option => "hist/option/quote",
                    _ => throw new NotImplementedException()
                },
                Resolution.Second or Resolution.Minute or Resolution.Hour => securityType switch
                {
                    SecurityType.Index => "hist/index/price",
                    SecurityType.Equity => "hist/stock/quote",
                    SecurityType.IndexOption or SecurityType.Option => "hist/option/quote",
                    _ => throw new NotImplementedException()
                },
                Resolution.Daily => securityType switch
                {
                    SecurityType.Equity => "hist/stock/eod",
                    SecurityType.IndexOption or SecurityType.Option => "hist/option/eod",
                    _ => throw new NotImplementedException()
                },
                _ => throw new NotImplementedException()
            };
        }

        private IEnumerable<BaseData>? GetTickHistoryData(
            string endpoint,
            Dictionary<string, string> queryParameters,
            Symbol symbol,
            TickType tickType,
            DateTime startDateTimeUtc,
            DateTime endDateTimeUtc,
            DateTimeZone symbolExchangeTimeZone)
        {
            // Implementation follows original ThetaDataProvider pattern
            // but uses CascadeThetaDataRestClient
            switch (tickType)
            {
                case TickType.Trade:
                    return GetHistoricalTickTradeData(endpoint, queryParameters, symbol, startDateTimeUtc, endDateTimeUtc, symbolExchangeTimeZone);
                case TickType.Quote:
                    queryParameters[RequestParameters.IntervalInMilliseconds] = "0";
                    return GetHistoricalQuoteData(endpoint, queryParameters, symbol, symbolExchangeTimeZone, Resolution.Tick);
                default:
                    throw new ArgumentException($"Invalid tick type: {tickType}");
            }
        }

        private IEnumerable<BaseData>? GetIntradayHistoryData(
            string endpoint,
            Dictionary<string, string> queryParameters,
            Symbol symbol,
            Resolution resolution,
            TickType tickType,
            DateTimeZone symbolExchangeTimeZone)
        {
            queryParameters[RequestParameters.IntervalInMilliseconds] = GetIntervalsInMilliseconds(resolution);
            var period = resolution.ToTimeSpan();

            switch (tickType)
            {
                case TickType.Trade:
                    return GetHistoricalOHLCData(endpoint, queryParameters, symbol, period, symbolExchangeTimeZone);
                case TickType.Quote:
                    return GetHistoricalQuoteData(endpoint, queryParameters, symbol, symbolExchangeTimeZone, resolution);
                default:
                    throw new ArgumentException($"Invalid tick type: {tickType}");
            }
        }

        private IEnumerable<BaseData>? GetDailyHistoryData(
            string endpoint,
            Dictionary<string, string> queryParameters,
            Symbol symbol,
            Resolution resolution,
            TickType tickType,
            DateTimeZone symbolExchangeTimeZone)
        {
            var period = resolution.ToTimeSpan();

            switch (tickType)
            {
                case TickType.Trade:
                    return GetHistoryEndOfDay(endpoint, queryParameters, symbol, period, symbolExchangeTimeZone, true);
                case TickType.Quote:
                    return GetHistoryEndOfDay(endpoint, queryParameters, symbol, period, symbolExchangeTimeZone, false);
                default:
                    throw new ArgumentException($"Invalid tick type: {tickType}");
            }
        }

        private IEnumerable<Tick> GetHistoricalTickTradeData(
            string endpoint,
            Dictionary<string, string> queryParameters,
            Symbol symbol,
            DateTime startDateTimeUtc,
            DateTime endDateTimeUtc,
            DateTimeZone symbolExchangeTimeZone)
        {
            var startDateTimeET = startDateTimeUtc.ConvertFromUtc(TimeZoneThetaData);
            var endDateTimeET = endDateTimeUtc.ConvertFromUtc(TimeZoneThetaData);
            var modifiedParams = new Dictionary<string, string>(queryParameters);

            foreach (var dateRange in ThetaDataExtensions.GenerateDateRangesWithInterval(startDateTimeET, endDateTimeET))
            {
                modifiedParams[RequestParameters.StartDate] = dateRange.startDate.ConvertToThetaDataDateFormat();
                modifiedParams[RequestParameters.EndDate] = dateRange.endDate.ConvertToThetaDataDateFormat();

                foreach (var trades in _restApiClient!.ExecuteRequest<BaseResponse<TradeResponse>>(endpoint, modifiedParams))
                {
                    if (trades.Response == null) continue;

                    foreach (var trade in trades.Response)
                    {
                        yield return new Tick(
                            ConvertToSymbolTimeZone(trade.DateTimeMilliseconds, symbolExchangeTimeZone),
                            symbol,
                            trade.Condition.ToStringInvariant(),
                            trade.Exchange.TryGetExchangeOrDefault(),
                            trade.Size,
                            trade.Price);
                    }
                }
            }
        }

        private IEnumerable<TradeBar> GetHistoricalOHLCData(
            string endpoint,
            Dictionary<string, string> queryParameters,
            Symbol symbol,
            TimeSpan period,
            DateTimeZone symbolExchangeTimeZone)
        {
            foreach (var trades in _restApiClient!.ExecuteRequest<BaseResponse<OpenHighLowCloseResponse>>(endpoint, queryParameters))
            {
                if (trades.Response == null) continue;

                foreach (var trade in trades.Response)
                {
                    if (trade.Open == 0 || trade.High == 0 || trade.Low == 0 || trade.Close == 0)
                        continue;

                    yield return new TradeBar(
                        ConvertToSymbolTimeZone(trade.DateTimeMilliseconds, symbolExchangeTimeZone),
                        symbol,
                        trade.Open,
                        trade.High,
                        trade.Low,
                        trade.Close,
                        trade.Volume,
                        period);
                }
            }
        }

        private IEnumerable<BaseData> GetHistoricalQuoteData(
            string endpoint,
            Dictionary<string, string> queryParameters,
            Symbol symbol,
            DateTimeZone symbolExchangeTimeZone,
            Resolution resolution)
        {
            var period = resolution.ToTimeSpan();

            foreach (var quotes in _restApiClient!.ExecuteRequest<BaseResponse<QuoteResponse>>(endpoint, queryParameters))
            {
                if (quotes.Response == null) continue;

                foreach (var quote in quotes.Response)
                {
                    if (quote.AskPrice == 0 || quote.BidPrice == 0) continue;

                    if (resolution == Resolution.Tick)
                    {
                        yield return new Tick(
                            ConvertToSymbolTimeZone(quote.DateTimeMilliseconds, symbolExchangeTimeZone),
                            symbol,
                            quote.AskCondition,
                            quote.AskExchange.TryGetExchangeOrDefault(),
                            quote.BidSize,
                            quote.BidPrice,
                            quote.AskSize,
                            quote.AskPrice);
                    }
                    else
                    {
                        var bar = new QuoteBar(
                            ConvertToSymbolTimeZone(quote.DateTimeMilliseconds, symbolExchangeTimeZone),
                            symbol,
                            null, decimal.Zero,
                            null, decimal.Zero,
                            period);
                        bar.UpdateQuote(quote.BidPrice, quote.BidSize, quote.AskPrice, quote.AskSize);
                        yield return bar;
                    }
                }
            }
        }

        private IEnumerable<BaseData> GetHistoryEndOfDay(
            string endpoint,
            Dictionary<string, string> queryParameters,
            Symbol symbol,
            TimeSpan period,
            DateTimeZone symbolExchangeTimeZone,
            bool isTrade)
        {
            foreach (var endOfDays in _restApiClient!.ExecuteRequest<BaseResponse<EndOfDayReportResponse>>(endpoint, queryParameters))
            {
                if (endOfDays.Response == null) continue;

                foreach (var eod in endOfDays.Response)
                {
                    if (isTrade)
                    {
                        if (eod.Open == 0 || eod.High == 0 || eod.Low == 0 || eod.Close == 0) continue;

                        yield return new TradeBar(
                            ConvertToSymbolTimeZone(eod.LastTradeDateTimeMilliseconds.Date, symbolExchangeTimeZone),
                            symbol,
                            eod.Open,
                            eod.High,
                            eod.Low,
                            eod.Close,
                            eod.Volume,
                            period);
                    }
                    else
                    {
                        if (eod.AskPrice == 0 || eod.BidPrice == 0) continue;

                        var bar = new QuoteBar(
                            ConvertToSymbolTimeZone(eod.LastTradeDateTimeMilliseconds.Date, symbolExchangeTimeZone),
                            symbol,
                            null, decimal.Zero,
                            null, decimal.Zero,
                            period);
                        bar.UpdateQuote(eod.BidPrice, eod.BidSize, eod.AskPrice, eod.AskSize);
                        yield return bar;
                    }
                }
            }
        }

        private static string GetIntervalsInMilliseconds(Resolution resolution) => resolution switch
        {
            Resolution.Tick => "0",
            Resolution.Second => "1000",
            Resolution.Minute => "60000",
            Resolution.Hour => "3600000",
            _ => throw new NotSupportedException($"Resolution '{resolution}' not supported")
        };

        private static DateTime ConvertToSymbolTimeZone(DateTime thetaDataTime, DateTimeZone symbolTimeZone)
            => thetaDataTime.ConvertTo(TimeZoneThetaData, symbolTimeZone);

        private IEnumerable<BaseData> FilterHistory(
            IEnumerable<BaseData> history,
            HistoryRequest request,
            DateTime startTimeLocal,
            DateTime endTimeLocal)
        {
            foreach (var bar in history)
            {
                if (bar.Time >= startTimeLocal && bar.EndTime <= endTimeLocal)
                {
                    if (request.ExchangeHours.IsOpen(bar.Time, bar.EndTime, request.IncludeExtendedMarketHours))
                    {
                        yield return bar;
                    }
                }
            }
        }

        // IDataQueueHandler implementation (minimal, REST-only)
        public IEnumerator<BaseData>? Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            // REST-only provider, no streaming support
            return null;
        }

        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            // Nothing to do for REST-only
        }

        public void Dispose()
        {
            _restApiClient?.Dispose();
            _dataAggregator?.DisposeSafely();
        }
    }
}

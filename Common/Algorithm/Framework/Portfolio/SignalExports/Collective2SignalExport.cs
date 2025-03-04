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
using QuantConnect.Interfaces;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;

namespace QuantConnect.Algorithm.Framework.Portfolio.SignalExports
{
    /// <summary>
    /// Exports signals of desired positions to Collective2 API using JSON and HTTPS.
    /// Accepts signals in quantity(number of shares) i.e symbol:"SPY", quant:40
    /// </summary>
    public class Collective2SignalExport : BaseSignalExport
    {
        /// <summary>
        /// Hashset of symbols whose market is unknown but have already been seen by
        /// this signal export manager
        /// </summary>
        private HashSet<string> _unknownMarketSymbols;

        /// <summary>
        /// Hashset of security types seen that are unsupported by C2 API
        /// </summary>
        private HashSet<SecurityType> _unknownSecurityTypes;

        /// <summary>
        /// API key provided by Collective2
        /// </summary>
        private readonly string _apiKey;

        /// <summary>
        /// Trading system's ID number
        /// </summary>
        private readonly int _systemId;

        /// <summary>
        /// Algorithm being ran
        /// </summary>
        private IAlgorithm _algorithm;

        /// <summary>
        /// Flag to track if the warning has already been printed.
        /// </summary>
        private bool _isZeroPriceWarningPrinted;

        /// <summary>
        /// Collective2 API endpoint
        /// </summary>
        public Uri Destination { get; set; }

        /// <summary>
        /// The name of this signal export
        /// </summary>
        protected override string Name { get; } = "Collective2";

        /// <summary>
        /// Lazy initialization of ten seconds rate limiter
        /// </summary>
        private static Lazy<RateGate> _tenSecondsRateLimiter = new Lazy<RateGate>(() => new RateGate(100, TimeSpan.FromMilliseconds(1000)));

        /// <summary>
        /// Lazy initialization of one hour rate limiter
        /// </summary>
        private static Lazy<RateGate> _hourlyRateLimiter = new Lazy<RateGate>(() => new RateGate(1000, TimeSpan.FromHours(1)));

        /// <summary>
        /// Lazy initialization of one day rate limiter
        /// </summary>
        private static Lazy<RateGate> _dailyRateLimiter = new Lazy<RateGate>(() => new RateGate(20000, TimeSpan.FromDays(1)));


        /// <summary>
        /// Collective2SignalExport constructor. It obtains the entry information for Collective2 API requests.
        /// See API documentation at https://trade.collective2.com/c2-api
        /// </summary>
        /// <param name="apiKey">API key provided by Collective2</param>
        /// <param name="systemId">Trading system's ID number</param>
        /// <param name="useWhiteLabelApi">Whether to use the white-label API instead of the general one</param>
        public Collective2SignalExport(string apiKey, int systemId, bool useWhiteLabelApi = false)
        {
            _unknownMarketSymbols = new HashSet<string>();
            _unknownSecurityTypes = new HashSet<SecurityType>();
            _apiKey = apiKey;
            _systemId = systemId;
            Destination = new Uri(useWhiteLabelApi
                ? "https://api4-wl.collective2.com/Strategies/SetDesiredPositions"
                : "https://api4-general.collective2.com/Strategies/SetDesiredPositions");
        }

        /// <summary>
        /// Creates a JSON message with the desired positions using the expected
        /// Collective2 API format and then sends it
        /// </summary>
        /// <param name="parameters">A list of holdings from the portfolio
        /// expected to be sent to Collective2 API and the algorithm being ran</param>
        /// <returns>True if the positions were sent correctly and Collective2 sent no errors, false otherwise</returns>
        public override bool Send(SignalExportTargetParameters parameters)
        {
            if (!base.Send(parameters))
            {
                return false;
            }

            if (!ConvertHoldingsToCollective2(parameters, out List<Collective2Position> positions))
            {
                return false;
            }
            var message = CreateMessage(positions);
            _tenSecondsRateLimiter.Value.WaitToProceed();
            _hourlyRateLimiter.Value.WaitToProceed();
            _dailyRateLimiter.Value.WaitToProceed();
            var result = SendPositions(message);

            return result;
        }

        /// <summary>
        /// Converts a list of targets to a list of Collective2 positions
        /// </summary>
        /// <param name="parameters">A list of targets from the portfolio
        /// expected to be sent to Collective2 API and the algorithm being ran</param>
        /// <param name="positions">A list of Collective2 positions</param>
        /// <returns>True if the given targets could be converted to a Collective2Position list, false otherwise</returns>
        protected bool ConvertHoldingsToCollective2(SignalExportTargetParameters parameters, out List<Collective2Position> positions)
        {
            _algorithm = parameters.Algorithm;
            var targets = parameters.Targets;
            positions = [];
            foreach (var target in targets)
            {
                if (target == null)
                {
                    _algorithm.Error("One portfolio target was null");
                    return false;
                }

                var securityType = GetSecurityTypeAcronym(target.Symbol.SecurityType);
                if (securityType == null)
                {
                    continue;
                }

                var maturityMonthYear = GetMaturityMonthYear(target.Symbol);
                if (maturityMonthYear?.Length == 0)
                {
                    continue;
                }

                positions.Add(new Collective2Position
                {
                    ExchangeSymbol = new C2ExchangeSymbol
                    {
                        Symbol = GetSymbol(target.Symbol),
                        Currency = parameters.Algorithm.AccountCurrency,
                        SecurityExchange = GetMICExchangeCode(target.Symbol),
                        SecurityType = securityType,
                        MaturityMonthYear = maturityMonthYear,
                        PutOrCall = GetPutOrCallValue(target.Symbol),
                        StrikePrice = GetStrikePrice(target.Symbol)
                    },
                    Quantity = ConvertPercentageToQuantity(_algorithm, target),
                });
            }

            return true;
        }

        /// <summary>
        /// Converts a given percentage of a position into the number of shares of it
        /// </summary>
        /// <param name="algorithm">Algorithm being ran</param>
        /// <param name="target">Desired position to be sent to the Collective2 API</param>
        /// <returns>Number of shares hold of the given position</returns>
        protected int ConvertPercentageToQuantity(IAlgorithm algorithm, PortfolioTarget target)
        {
            var numberShares = PortfolioTarget.Percent(algorithm, target.Symbol, target.Quantity);
            if (numberShares == null)
            {
                if (algorithm.Securities.TryGetValue(target.Symbol, out var security) && security.Price == 0 && target.Quantity == 0)
                {
                    if (!_isZeroPriceWarningPrinted)
                    {
                        _isZeroPriceWarningPrinted = true;
                        algorithm.Debug($"Warning: Collective2 failed to calculate target quantity for {target}. The price for {target.Symbol} is 0, and the target quantity is 0. Will return 0 for all similar cases.");
                    }
                    return 0;
                }
                throw new InvalidOperationException($"Collective2 failed to calculate target quantity for {target}");
            }

            return (int)numberShares.Quantity;
        }

        /// <summary>
        /// Serializes the list of desired positions with the needed credentials in JSON format
        /// </summary>
        /// <param name="positions">List of Collective2 positions to be sent to Collective2 API</param>
        /// <returns>A JSON request string of the desired positions to be sent by a POST request to Collective2 API</returns>
        protected string CreateMessage(List<Collective2Position> positions)
        {
            var payload = new
            {
                StrategyId = _systemId,
                Positions = positions,
            };

            var jsonMessage = JsonConvert.SerializeObject(payload);
            return jsonMessage;
        }

        /// <summary>
        /// Sends the desired positions list in JSON format to Collective2 API using a POST request. It logs
        /// the message retrieved by the Collective2 API if there was a HttpRequestException
        /// </summary>
        /// <param name="message">A JSON request string of the desired positions list with the credentials</param>
        /// <returns>True if the positions were sent correctly and Collective2 API sent no errors, false otherwise</returns>
        private bool SendPositions(string message)
        {
            using var httpMessage = new StringContent(message, Encoding.UTF8, "application/json");

            //Add the QuantConnect app header
            httpMessage.Headers.Add("X-AppId", "OPA1N90E71");

            //Add the Authorization header
            HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            //Send the message
            using HttpResponseMessage response = HttpClient.PostAsync(Destination, httpMessage).Result;

            //Parse it
            var responseObject = response.Content.ReadFromJsonAsync<C2Response>().Result;

            //For debugging purposes, append the message sent to Collective2 to the algorithms log
            var debuggingMessage = Logging.Log.DebuggingEnabled ? $" | Message={message}" : string.Empty;

            if (!response.IsSuccessStatusCode)
            {
                _algorithm.Error($"Collective2 API returned the following errors: {string.Join(",", PrintErrors(responseObject.ResponseStatus.Errors))}{debuggingMessage}");
                return false;
            }
            else if (responseObject.Results.Count > 0)
            {
                _algorithm.Debug($"Collective2: NewSignals={string.Join(',', responseObject.Results[0].NewSignals)} | CanceledSignals={string.Join(',', responseObject.Results[0].CanceledSignals)}{debuggingMessage}");
            }

            return true;
        }

        private static string PrintErrors(List<ResponseError> errors)
        {
            if (errors?.Count == 0)
            {
                return "NULL";
            }

            StringBuilder sb = new StringBuilder();
            foreach (var error in errors)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"({error.ErrorCode}) {error.FieldName}: {error.Message}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// The main C2 response class for this endpoint
        /// </summary>
        private class C2Response
        {
            [JsonProperty(PropertyName = "Results")]
            public virtual List<DesiredPositionResponse> Results { get; set; }


            [JsonProperty(PropertyName = "ResponseStatus")]
            public ResponseStatus ResponseStatus { get; set; }
        }

        /// <summary>
        /// The Results object
        /// </summary>
        private class DesiredPositionResponse
        {
            [JsonProperty(PropertyName = "NewSignals")]
            public List<long> NewSignals { get; set; } = new List<long>();


            [JsonProperty(PropertyName = "CanceledSignals")]
            public List<long> CanceledSignals { get; set; } = new List<long>();
        }

        /// <summary>
        /// Returns the given symbol in the expected C2 format
        /// </summary>
        private string GetSymbol(Symbol symbol)
        {
            if (CurrencyPairUtil.TryDecomposeCurrencyPair(symbol, out var baseCurrency, out var quoteCurrency))
            {
                return $"{baseCurrency}/{quoteCurrency}";
            }
            else if (symbol.SecurityType.IsOption())
            {
                return symbol.Underlying.Value;
            }
            else
            {
                return symbol.ID.Symbol;
            }
        }

        private string GetMICExchangeCode(Symbol symbol)
        {
            if (symbol.SecurityType == SecurityType.Equity || symbol.SecurityType.IsOption())
            {
                return "DEFAULT";
            }

            switch (symbol.ID.Market)
            {
                case Market.India:
                    return "XNSE";
                case Market.HKFE:
                    return "XHKF";
                case Market.NYSELIFFE:
                    return "XNLI";
                case Market.EUREX:
                    return "XEUR";
                case Market.ICE:
                    return "IEPA";
                case Market.CBOE:
                    return "XCBO";
                case Market.CFE:
                    return "XCBF";
                case Market.CBOT:
                    return "XCBT";
                case Market.COMEX:
                    return "XCEC";
                case Market.NYMEX:
                    return "XNYM";
                case Market.SGX:
                    return "XSES";
                case Market.FXCM:
                    return symbol.ID.Market.ToUpper();
                case Market.OSE:
                case Market.CME:
                    return $"X{symbol.ID.Market.ToUpper()}";
                default:
                    if (_unknownMarketSymbols.Add(symbol.Value))
                    {
                        _algorithm.Debug($"The market of the symbol {symbol.Value} was unexpected: {symbol.ID.Market}. Using 'DEFAULT' as market");
                    }

                    return "DEFAULT";
            }
        }

        /// <summary>
        /// Returns the given security type in the format C2 expects
        /// </summary>
        private string GetSecurityTypeAcronym(SecurityType securityType)
        {
            switch (securityType)
            {
                case SecurityType.Equity:
                    return "CS";
                case SecurityType.Future:
                    return "FUT";
                case SecurityType.Option:
                case SecurityType.IndexOption:
                    return "OPT";
                case SecurityType.Forex:
                    return "FOR";
                default:
                    if (_unknownSecurityTypes.Add(securityType))
                    {
                        _algorithm.Debug($"Unexpected security type found: {securityType}. Collective2 just accepts: Equity, Future, Option, Index Option and Stock");
                    }
                    return null;
            }
        }

        /// <summary>
        /// Returns the expiration date in the format C2 expects
        /// </summary>
        private string GetMaturityMonthYear(Symbol symbol)
        {
            var delistingDate = symbol.GetDelistingDate();
            if (delistingDate == Time.EndOfTime) // The given symbol is equity or forex
            {
                return null;
            }

            if (delistingDate < _algorithm.Securities[symbol].LocalTime.Date) // The given symbol has already expired
            {
                _algorithm.Error($"Instrument {symbol} has already expired. Its delisting date was: {delistingDate}. This signal won't be sent to Collective2.");
                return string.Empty;
            }

            return $"{delistingDate:yyyyMMdd}";
        }

        private int? GetPutOrCallValue(Symbol symbol)
        {
            if (symbol.SecurityType.IsOption())
            {
                switch (symbol.ID.OptionRight)
                {
                    case OptionRight.Put:
                        return 0;
                    case OptionRight.Call:
                        return 1;
                }
            }

            return null;
        }

        private decimal? GetStrikePrice(Symbol symbol)
        {
            if (symbol.SecurityType.IsOption())
            {
                return symbol.ID.StrikePrice;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// The C2 ResponseStatus object
        /// </summary>
        private class ResponseStatus
        {
            /* Example:

                    "ResponseStatus":
                    {
                      "ErrorCode": ""401",
                      "Message": ""Unauthorized",
                      "Errors": [
                        {
                          "ErrorCode": "2015",
                          "FieldName": "APIKey",
                          "Message": ""Unknown API Key"
                        }
                      ]
                    }
            */


            [JsonProperty(PropertyName = "ErrorCode")]
            public string ErrorCode { get; set; }


            [JsonProperty(PropertyName = "Message")]
            public string Message { get; set; }


            [JsonProperty(PropertyName = "Errors")]
            public List<ResponseError> Errors { get; set; }

        }

        /// <summary>
        /// The ResponseError object
        /// </summary>
        private class ResponseError
        {
            [JsonProperty(PropertyName = "ErrorCode")]
            public string ErrorCode { get; set; }


            [JsonProperty(PropertyName = "FieldName")]
            public string FieldName { get; set; }


            [JsonProperty(PropertyName = "Message")]
            public string Message { get; set; }
        }

        /// <summary>
        /// Stores position's needed information to be serialized in JSON format
        /// and then sent to Collective2 API
        /// </summary>
        protected class Collective2Position
        {
            /// <summary>
            /// Position symbol
            /// </summary>
            [JsonProperty(PropertyName = "exchangeSymbol")]
            public C2ExchangeSymbol ExchangeSymbol { get; set; }

            /// <summary>
            /// Number of shares/contracts of the given symbol. Positive quantites are long positions
            /// and negative short positions.
            /// </summary>
            [JsonProperty(PropertyName = "quantity")]
            public decimal Quantity { get; set; } // number of shares, not % of the portfolio
        }

        /// <summary>
        /// The Collective2 symbol
        /// </summary>
        protected class C2ExchangeSymbol
        {
            /// <summary>
            /// The exchange root symbol e.g. AAPL
            /// </summary>
            [JsonProperty(PropertyName = "symbol")]
            public string Symbol { get; set; }

            /// <summary>
            /// The 3-character ISO instrument currency. E.g. 'USD'
            /// </summary>
            [JsonProperty(PropertyName = "currency")]
            public string Currency { get; set; }

            /// <summary>
            /// The MIC Exchange code e.g. DEFAULT (for stocks & options),
            /// XCME, XEUR, XICE, XLIF, XNYB, XNYM, XASX, XCBF, XCBT, XCEC,
            /// XKBT, XSES. See details at http://www.iso15022.org/MIC/homepageMIC.htm
            /// </summary>
            [JsonProperty(PropertyName = "securityExchange")]
            public string SecurityExchange { get; set; }


            /// <summary>
            /// The SecurityType e.g. 'CS'(Common Stock), 'FUT' (Future), 'OPT' (Option), 'FOR' (Forex)
            /// </summary>
            [JsonProperty(PropertyName = "securityType")]
            public string SecurityType { get; set; }

            /// <summary>
            /// The MaturityMonthYear e.g. '202103' (March 2021), or if the contract requires a day: '20210521' (May 21, 2021)
            /// </summary>
            [JsonProperty(PropertyName = "maturityMonthYear")]
            public string MaturityMonthYear { get; set; }

            /// <summary>
            /// The Option PutOrCall e.g. 0 = Put, 1 = Call
            /// </summary>
            [JsonProperty(PropertyName = "putOrCall")]
            public int? PutOrCall { get; set; }

            /// <summary>
            /// The ISO Option Strike Price. Zero means none
            /// </summary>
            [JsonProperty(PropertyName = "strikePrice")]
            public decimal? StrikePrice { get; set; }

            /// <summary>
            /// The multiplier to apply to the Exchange price to get the C2-formatted price. Default is 1
            /// </summary>
            [JsonProperty(PropertyName = "priceMultiplier")]
            public decimal PriceMultiplier { get; set; } = 1;
        }
    }
}

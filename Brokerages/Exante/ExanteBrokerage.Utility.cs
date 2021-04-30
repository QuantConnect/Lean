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
using Exante.Net.Enums;
using Exante.Net.Objects;
using QuantConnect.Orders;

namespace QuantConnect.Brokerages.Exante
{
    public partial class ExanteBrokerage
    {
        private static OrderStatus ConvertOrderStatus(ExanteOrderStatus status)
        {
            switch (status)
            {
                case ExanteOrderStatus.Pending:
                    return OrderStatus.Submitted;

                case ExanteOrderStatus.Placing:
                case ExanteOrderStatus.Working:
                    return OrderStatus.PartiallyFilled;

                case ExanteOrderStatus.Cancelled:
                    return OrderStatus.Canceled;

                case ExanteOrderStatus.Filled:
                    return OrderStatus.Filled;

                case ExanteOrderStatus.Rejected:
                    return OrderStatus.Invalid;

                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }

        private Holding ConvertHolding(ExantePosition position)
        {
            var exanteSymbol = _client.GetSymbol(position.SymbolId).Data;
            var symbol = ConvertSymbol(exanteSymbol);
            var holding = new Holding
            {
                Symbol = symbol,
                Quantity = position.Quantity,
                CurrencySymbol = Currencies.GetCurrencySymbol(position.Currency),
                Type = symbol.SecurityType
            };

            if (position.AveragePrice != null)
            {
                holding.AveragePrice = position.AveragePrice.Value;
            }

            if (position.PnL != null)
            {
                holding.UnrealizedPnL = position.PnL.Value;
            }

            if (position.Price != null)
            {
                holding.MarketPrice = position.Price.Value;
            }

            return holding;
        }

        private ExanteOrderSide ConvertOrderDirection(OrderDirection orderDirection)
        {
            var orderSide = default(ExanteOrderSide);
            switch (orderDirection)
            {
                case OrderDirection.Buy:
                    orderSide = ExanteOrderSide.Buy;
                    break;
                case OrderDirection.Sell:
                    orderSide = ExanteOrderSide.Sell;
                    break;
                case OrderDirection.Hold:
                    throw new NotSupportedException(
                        $"ExanteBrokerage.ConvertOrderDirection: Unsupported order direction: {orderDirection}");
            }

            return orderSide;
        }

        private OrderDirection ConvertOrderSide(ExanteOrderSide orderSide)
        {
            var orderDirection = default(OrderDirection);
            switch (orderSide)
            {
                case ExanteOrderSide.Buy:
                    orderDirection = OrderDirection.Buy;
                    break;
                case ExanteOrderSide.Sell:
                    orderDirection = OrderDirection.Sell;
                    break;
                default:
                    throw new NotSupportedException(
                        $"ExanteBrokerage.ConvertOrderDirection: Unsupported order direction: {orderDirection}");
            }

            return orderDirection;
        }

        private static string GetSymbolMarket(ExanteSymbol symbol)
        {
            switch (symbol.SymbolType)
            {
                case ExanteSymbolType.FXSpot:
                case ExanteSymbolType.Currency:
                {
                    const string unknownForexMarket = "";
                    return unknownForexMarket;
                }

                case ExanteSymbolType.Index:
                {
                    const string unknownIndexMarket = "";
                    return unknownIndexMarket;
                }

                case ExanteSymbolType.Stock:
                {
                    const string unknownStockMarket = "";
                    var exchange = symbol.Exchange.LazyToLower();
                    string market;
                    if (exchange == "nyse" ||
                        exchange == "nasdaq" ||
                        exchange == "arca" ||
                        exchange == "otcbb" ||
                        exchange == "bats" ||
                        exchange == "otcmkts" ||
                        exchange == "amex")
                    {
                        market = Market.USA;
                    }
                    else if (exchange == "hkex")
                    {
                        market = Market.HKFE;
                    }
                    else
                    {
                        market = unknownStockMarket;
                    }

                    return market;
                }

                case ExanteSymbolType.Bond:
                {
                    const string unknownBondMarket = "";
                    return unknownBondMarket;
                }

                case ExanteSymbolType.Fund when SupportedCryptoCurrencies.Contains(symbol.Ticker):
                {
                    const string unknownCryptoMarket = "EXANTE";
                    return unknownCryptoMarket;
                }

                case ExanteSymbolType.Fund:
                {
                    const string unknownFundMarket = "";
                    return unknownFundMarket;
                }

                case ExanteSymbolType.Future:
                {
                    const string unknownFutureMarket = "";
                    var exchange = symbol.Exchange.LazyToLower();
                    string market;
                    if (
                        exchange == Market.CME ||
                        exchange == Market.CBOT ||
                        exchange == Market.COMEX ||
                        exchange == Market.CBOE ||
                        exchange == Market.NYMEX ||
                        exchange == Market.ICE ||
                        exchange == Market.SGX)
                    {
                        market = exchange;
                    }
                    else if (exchange == "hkex")
                    {
                        market = Market.HKFE;
                    }
                    else
                    {
                        market = unknownFutureMarket;
                    }

                    return market;
                }

                case ExanteSymbolType.Option:
                {
                    const string unknownOptionMarket = "";
                    var exchange = symbol.Exchange.LazyToLower();
                    string market;
                    if (exchange == Market.CBOE ||
                        exchange == Market.CME ||
                        exchange == Market.COMEX ||
                        exchange == Market.NYMEX ||
                        exchange == Market.CBOE ||
                        exchange == Market.SGX)
                    {
                        market = exchange;
                    }
                    else if (exchange == "hkex")
                    {
                        market = Market.HKFE;
                    }
                    else
                    {
                        market = unknownOptionMarket;
                    }

                    return market;
                }

                case ExanteSymbolType.CFD:
                {
                    const string unknownCfdMarket = "";
                    return unknownCfdMarket;
                }

                case ExanteSymbolType.CalendarSpread:
                {
                    const string unknownCalendarSpreadMarket = "";
                    var exchange = symbol.Exchange.LazyToLower();
                    string market;
                    if (exchange == Market.NYMEX ||
                        exchange == Market.CBOT ||
                        exchange == Market.CME ||
                        exchange == Market.CBOE)
                    {
                        market = exchange;
                    }
                    else
                    {
                        market = unknownCalendarSpreadMarket;
                    }

                    return market;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(symbol.SymbolType), symbol.SymbolType, null);
            }
        }

        private static SecurityType GetSymbolSecurityType(ExanteSymbol symbol)
        {
            switch (symbol.SymbolType)
            {
                case ExanteSymbolType.FXSpot:
                case ExanteSymbolType.Currency:
                    return SecurityType.Forex;

                case ExanteSymbolType.Stock:
                    return SecurityType.Equity;

                case ExanteSymbolType.Future:
                    return SecurityType.Future;

                case ExanteSymbolType.Option:
                    return SecurityType.Option;

                case ExanteSymbolType.CFD:
                    return SecurityType.Cfd;

                case ExanteSymbolType.Index:
                    return SecurityType.Index;

                case ExanteSymbolType.Fund when SupportedCryptoCurrencies.Contains(symbol.Ticker):
                    return SecurityType.Crypto;

                case ExanteSymbolType.CalendarSpread:
                case ExanteSymbolType.Bond:
                case ExanteSymbolType.Fund:
                    throw new NotSupportedException(
                        $"An existing position or open order for an unsupported security type was found: {symbol}. " +
                        "Please manually close the position or cancel the order before restarting the algorithm.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(symbol.SymbolType), symbol.SymbolType, null);
            }
        }

        private OptionRight ConvertOptionRight(string optionRight)
        {
            switch (optionRight)
            {
                case "put":
                    return OptionRight.Put;
                case "call":
                    return OptionRight.Call;
                default:
                    return OptionRight.Call;
            }
        }

        private Symbol ConvertSymbol(ExanteSymbol symbol)
        {
            var market = GetSymbolMarket(symbol);
            var securityType = GetSymbolSecurityType(symbol);

            Symbol sym;
            switch (securityType)
            {
                case SecurityType.Option:
                {
                    var expiration = symbol.Expiration ?? default(DateTime);
                    var strikePrice = symbol.OptionData?.StrikePrice ?? 0m;
                    sym = _symbolMapper.GetLeanSymbol(symbol.Ticker, securityType, market, expiration,
                        strikePrice, ConvertOptionRight(symbol.OptionData?.OptionRight)
                    );
                    break;
                }

                case SecurityType.Future:
                {
                    var expiration = symbol.Expiration ?? default(DateTime);
                    sym = _symbolMapper.GetLeanSymbol(symbol.Ticker, securityType, market, expiration);
                    break;
                }

                default:
                {
                    sym = _symbolMapper.GetLeanSymbol(symbol.Ticker, securityType, market);
                    break;
                }
            }

            return sym;
        }
    }
}

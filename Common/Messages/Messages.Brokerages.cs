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
using System.Runtime.CompilerServices;

using QuantConnect.Brokerages;
using QuantConnect.Securities;
using QuantConnect.Orders;

using static QuantConnect.StringExtensions;
using System.Collections.Generic;
using QuantConnect.Orders.TimeInForces;

namespace QuantConnect
{
    /// <summary>
    /// Provides user-facing message construction methods and static messages for the <see cref="Brokerages"/> namespace
    /// </summary>
    public static partial class Messages
    {
        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.DefaultBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class DefaultBrokerageModel
        {
            public static string UnsupportedMarketOnOpenOrdersForFuturesAndFutureOptions =
                "MarketOnOpen orders are not supported for futures and future options.";

            public static string NoDataForSymbol =
                "There is no data for this symbol yet, please check the security.HasData flag to ensure there is at least one data point.";

            public static string OrderUpdateNotSupported = "Brokerage does not support update. You must cancel and re-create instead.";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedSecurityType(IBrokerageModel brokerageModel, Securities.Security security)
            {
                return Invariant($"The {brokerageModel.GetType().Name} does not support {security.Type} security type.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidSecurityTypeToGetFillModel(IBrokerageModel brokerageModel, Securities.Security security)
            {
                return Invariant($"{brokerageModel.GetType().Name}.GetFillModel: Invalid security type {security.Type}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidOrderQuantity(Securities.Security security, decimal quantity)
            {
                return Invariant($@"The minimum order size (in quote currency) for {security.Symbol.Value} is {
                    security.SymbolProperties.MinimumOrderSize}. Order quantity was {quantity}.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidOrderSize(Securities.Security security, decimal quantity, decimal price)
            {
                return Invariant($@"The minimum order size (in quote currency) for {security.Symbol.Value} is {security.SymbolProperties.MinimumOrderSize}. Order size was {quantity * price}.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedOrderType(IBrokerageModel brokerageModel, Orders.Order order, IEnumerable<OrderType> supportedOrderTypes)
            {
                return Invariant($"The {brokerageModel.GetType().Name} does not support {order.Type} order type. Only supports [{string.Join(',', supportedOrderTypes)}]");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedTimeInForce(IBrokerageModel brokerageModel, Orders.Order order)
            {
                return Invariant($@"The {brokerageModel.GetType().Name} does not support {
                    order.TimeInForce.GetType().Name} time in force.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidSecurityTypeForLeverage(Securities.Security security)
            {
                return Invariant($"Invalid security type: {security.Type}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.AlphaStreamsBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class AlphaStreamsBrokerageModel
        {
            public static string UnsupportedAccountType = "The Alpha Streams brokerage does not currently support Cash trading.";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.AxosClearingBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class AxosBrokerageModel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string NonIntegerOrderQuantity(Orders.Order order)
            {
                return Invariant($"Order Quantity must be Integer, but provided {order.Quantity}.");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.BinanceBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class BinanceBrokerageModel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedOrderTypeForSecurityType(Orders.Order order, Securities.Security security)
            {
                return Invariant($"{order.Type} orders are not supported for this symbol ${security.Symbol}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedOrderTypeWithLinkToSupportedTypes(Orders.Order order, Securities.Security security)
            {
                return Invariant($@"{order.Type} orders are not supported for this symbol. Please check 'https://api.binance.com/api/v3/exchangeInfo?symbol={
                    security.SymbolProperties.MarketTicker}' to see supported order types.");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.BinanceUSBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class BinanceUSBrokerageModel
        {
            public static string UnsupportedAccountType = "The Binance.US brokerage does not currently support Margin trading.";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.BrokerageMessageEvent"/> class and its consumers or related classes
        /// </summary>
        public static class BrokerageMessageEvent
        {
            public static string DisconnectCode = "Disconnect";

            public static string ReconnectCode = "Reconnect";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Brokerages.BrokerageMessageEvent messageEvent)
            {
                return Invariant($"{messageEvent.Type} - Code: {messageEvent.Code} - {messageEvent.Message}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.DefaultBrokerageMessageHandler"/> class and its consumers or related classes
        /// </summary>
        public static class DefaultBrokerageMessageHandler
        {
            public static string BrokerageErrorContext = "Brokerage Error";

            public static string Disconnected = "DefaultBrokerageMessageHandler.Handle(): Disconnected.";

            public static string Reconnected = "DefaultBrokerageMessageHandler.Handle(): Reconnected.";

            public static string DisconnectedWhenExchangesAreClosed =
                "DefaultBrokerageMessageHandler.Handle(): Disconnect when exchanges are closed, checking back before exchange open.";

            public static string StillDisconnected = "DefaultBrokerageMessageHandler.Handle(): Still disconnected, goodbye.";

            public static string BrokerageDisconnectedShutDownContext = "Brokerage Disconnect";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string BrokerageInfo(Brokerages.BrokerageMessageEvent messageEvent)
            {
                return $"Brokerage Info: {messageEvent.Message}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string BrokerageWarning(Brokerages.BrokerageMessageEvent messageEvent)
            {
                return $"Brokerage Warning: {messageEvent.Message}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string DisconnectedWhenExchangesAreOpen(TimeSpan reconnectionTimeout)
            {
                return Invariant($@"DefaultBrokerageMessageHandler.Handle(): Disconnect when exchanges are open, trying to reconnect for {
                    reconnectionTimeout.TotalMinutes} minutes.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string TimeUntilNextMarketOpen(TimeSpan timeUntilNextMarketOpen)
            {
                return Invariant($"DefaultBrokerageMessageHandler.Handle(): TimeUntilNextMarketOpen: {timeUntilNextMarketOpen}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.ExanteBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class ExanteBrokerageModel
        {
            public static string NullOrder = "Order is null.";

            public static string PriceNotSet = "Price is not set.";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.FTXBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class FTXBrokerageModel
        {
            public static string TriggerPriceTooHigh = "Trigger price too high: must be below current market price.";

            public static string TriggerPriceTooLow = "Trigger price too low: must be above current market price.";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.FxcmBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class FxcmBrokerageModel
        {
            public static string InvalidOrderPrice =
                "Limit Buy orders and Stop Sell orders must be below market, Limit Sell orders and Stop Buy orders must be above market.";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidOrderQuantityForLotSize(Securities.Security security)
            {
                return Invariant($"The order quantity must be a multiple of LotSize: [{security.SymbolProperties.LotSize}].");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string PriceOutOfRange(OrderType orderType, OrderDirection orderDirection, decimal orderPrice, decimal currentPrice)
            {
                return Invariant($@"The {orderType} {orderDirection} order price ({
                    orderPrice}) is too far from the current market price ({currentPrice}).");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.CoinbaseBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class CoinbaseBrokerageModel
        {
            public static string UnsupportedAccountType = "The Coinbase brokerage does not currently support Margin trading.";
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string StopMarketOrdersNoLongerSupported(DateTime stopMarketOrderSupportEndDate)
            {
                return Invariant($"Stop Market orders are no longer supported since {stopMarketOrderSupportEndDate}.");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.InteractiveBrokersBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class InteractiveBrokersBrokerageModel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedExerciseForIndexAndCashSettledOptions(Brokerages.InteractiveBrokersBrokerageModel brokerageModel,
                Orders.Order order)
            {
                return Invariant($@"The {brokerageModel.GetType().Name} does not support {
                    order.Type} exercises for index and cash-settled options.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidForexOrderSize(decimal min, decimal max, string currency)
            {
                return Invariant($"The minimum and maximum limits for the allowable order size are ({min}, {max}){currency}.");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.TradierBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class TradierBrokerageModel
        {
            public static string UnsupportedSecurityType = "This model only supports equities and options.";

            public static string UnsupportedTimeInForceType = $"This model only supports orders with the following time in force types: {typeof(DayTimeInForce)} and {typeof(GoodTilCanceledTimeInForce)}";

            public static string ExtendedMarketHoursTradingNotSupported =
                "Tradier does not support extended market hours trading. Your order will be processed at market open.";

            public static string OrderQuantityUpdateNotSupported = "Tradier does not support updating order quantities.";

            public static string OpenOrdersCancelOnReverseSplitSymbols = "Tradier Brokerage cancels open orders on reverse split symbols";

            public static string ShortOrderIsGtc = "You cannot place short stock orders with GTC, only day orders are allowed";

            public static string SellShortOrderLastPriceBelow5 = "Sell Short order cannot be placed for stock priced below $5";

            public static string IncorrectOrderQuantity = "Quantity should be between 1 and 10,000,000";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.TradingTechnologiesBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class TradingTechnologiesBrokerageModel
        {
            public static string InvalidStopMarketOrderPrice =
                "StopMarket Sell orders must be below market, StopMarket Buy orders must be above market.";

            public static string InvalidStopLimitOrderPrice =
                "StopLimit Sell orders must be below market, StopLimit Buy orders must be above market.";

            public static string InvalidStopLimitOrderLimitPrice =
                "StopLimit Buy limit price must be greater than or equal to stop price, StopLimit Sell limit price must be smaller than or equal to stop price.";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.WolverineBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class WolverineBrokerageModel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedOrderType(Orders.Order order)
            {
                return Invariant($"{order.Type} order is not supported by Wolverine. Currently, only Market Order is supported.");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.RBIBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class RBIBrokerageModel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedOrderType(Orders.Order order)
            {
                return Invariant($"{order.Type} order is not supported by RBI. Currently, only Market Order, Limit Order, StopMarket Order and StopLimit Order are supported.");
            }
        }
    }
}

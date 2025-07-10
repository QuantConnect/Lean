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
using QuantConnect.Orders;

using static QuantConnect.StringExtensions;
using System.Collections.Generic;
using QuantConnect.Orders.TimeInForces;
using System.Globalization;

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
            /// <summary>
            /// String message saying: MarketOnOpen orders are not supported for futures and future options
            /// </summary>
            public static string UnsupportedMarketOnOpenOrdersForFuturesAndFutureOptions =
                "MarketOnOpen orders are not supported for futures and future options.";

            /// <summary>
            /// String message saying: There is no data for this symbol yet
            /// </summary>
            public static string NoDataForSymbol =
                "There is no data for this symbol yet, please check the security.HasData flag to ensure there is at least one data point.";

            /// <summary>
            /// String message saying: Brokerage does not support update. You must cancel and re-create instead
            /// </summary>
            public static string OrderUpdateNotSupported = "Brokerage does not support update. You must cancel and re-create instead.";

            /// <summary>
            /// Retunrns a string message saying the type of the given security is not supported by the given brokerage
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedSecurityType(IBrokerageModel brokerageModel, Securities.Security security)
            {
                return Invariant($"The {brokerageModel.GetType().Name} does not support {security.Type} security type.");
            }

            /// <summary>
            /// Returns a string message saying the given brokerage does not support updating the quantity of Cross Zero orders
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedCrossZeroOrderUpdate(IBrokerageModel brokerageModel)
            {
                return Invariant($"Unfortunately, the {brokerageModel.GetType().Name} brokerage model does not support updating the quantity of Cross Zero Orders.");
            }

            /// <summary>
            /// Returns a string message saying the type of the given security is invalid for the given brokerage GetFillModel() method
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidSecurityTypeToGetFillModel(IBrokerageModel brokerageModel, Securities.Security security)
            {
                return Invariant($"{brokerageModel.GetType().Name}.GetFillModel: Invalid security type {security.Type}");
            }

            /// <summary>
            /// Returns a string message saying the quantity given was invalid for the given security
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidOrderQuantity(Securities.Security security, decimal quantity)
            {
                return Invariant($@"The minimum order size (in quote currency) for {security.Symbol.Value} is {
                    security.SymbolProperties.MinimumOrderSize}. Order quantity was {quantity}.");
            }

            /// <summary>
            /// Returns a string message saying the given order size (quantity * price) was invalid for the given security
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidOrderSize(Securities.Security security, decimal quantity, decimal price)
            {
                return Invariant($@"The minimum order size (in quote currency) for {security.Symbol.Value} is {security.SymbolProperties.MinimumOrderSize}. Order size was {quantity * price}.");
            }

            /// <summary>
            /// Returns a string message saying the type of the given order is unsupported by the given brokerage model. It also
            /// mentions the supported order types
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedOrderType(IBrokerageModel brokerageModel, Orders.Order order, IEnumerable<OrderType> supportedOrderTypes)
            {
                return Invariant($"The {brokerageModel.GetType().Name} does not support {order.Type} order type. Only supports [{string.Join(',', supportedOrderTypes)}]");
            }

            /// <summary>
            /// Returns a string message saying the Time In Force of the given order is unsupported by the given brokerage
            /// model
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedTimeInForce(IBrokerageModel brokerageModel, Orders.Order order)
            {
                return Invariant($@"The {brokerageModel.GetType().Name} does not support {
                    order.TimeInForce.GetType().Name} time in force.");
            }

            /// <summary>
            /// Returns a string message saying the type of the given security is invalid
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidSecurityTypeForLeverage(Securities.Security security)
            {
                return Invariant($"Invalid security type: {security.Type}");
            }

            /// <summary>
            /// Returns a message indicating that the specified order type is not supported for orders that cross the zero holdings threshold.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedCrossZeroByOrderType(IBrokerageModel brokerageModel, OrderType orderType)
            {
                return Invariant($"Order type '{orderType}' is not supported for orders that cross the zero holdings threshold in the {brokerageModel.GetType().Name}. This means you cannot change a position from positive to negative or vice versa using this order type. Please close the existing position first.");
            }

            /// <summary>
            /// Returns a message indicating that the specified order type cannot be updated quantity using the given brokerage model.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedUpdateQuantityOrder(IBrokerageModel brokerageModel, OrderType orderType)
            {
                return Invariant($"Order type '{orderType}' is not supported to update quantity in the {brokerageModel.GetType().Name}.");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.AlpacaBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class AlpacaBrokerageModel
        {
            /// <summary>
            /// Returns a message indicating that the specified order type is not supported for trading outside
            /// regular hours by the given brokerage model.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string TradingOutsideRegularHoursNotSupported(IBrokerageModel brokerageModel, OrderType orderType, TimeInForce timeInForce)
            {
                return Invariant($"The {brokerageModel.GetType().Name} does not support {orderType} orders with {timeInForce} TIF outside regular hours. ") +
                    Invariant($"Only {OrderType.Limit} orders with {TimeInForce.Day} TIF are supported outside regular trading hours.");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.AlphaStreamsBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class AlphaStreamsBrokerageModel
        {
            /// <summary>
            /// String message saying: The Alpha Streams brokerage does not currently support Cash trading
            /// </summary>
            public static string UnsupportedAccountType = "The Alpha Streams brokerage does not currently support Cash trading.";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.AxosClearingBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class AxosBrokerageModel
        {
            /// <summary>
            /// Returns a string message saying the order quantity must be Integer. It also contains
            /// the quantity of the given order
            /// </summary>
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
            /// <summary>
            /// Returns a string message saying the type of the given order is unsupported for the symbol of the given
            /// security
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedOrderTypeForSecurityType(Orders.Order order, Securities.Security security)
            {
                return Invariant($"{order.Type} orders are not supported for this symbol ${security.Symbol}");
            }

            /// <summary>
            /// Returns a string message saying the type of the given order is unsupported for the symbol of the given
            /// security. The message also contains a link to the supported order types in Binance
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedOrderTypeWithLinkToSupportedTypes(string baseApiEndpoint, Orders.Order order, Securities.Security security)
            {
                return Invariant($@"{order.Type} orders are not supported for this symbol. Please check '{baseApiEndpoint}/exchangeInfo?symbol={security.SymbolProperties.MarketTicker}' to see supported order types.");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.BinanceUSBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class BinanceUSBrokerageModel
        {
            /// <summary>
            /// String message saying: The Binance.US brokerage does not currently support Margin trading
            /// </summary>
            public static string UnsupportedAccountType = "The Binance.US brokerage does not currently support Margin trading.";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.BrokerageMessageEvent"/> class and its consumers or related classes
        /// </summary>
        public static class BrokerageMessageEvent
        {
            /// <summary>
            /// String message saying: Disconnect
            /// </summary>
            public static string DisconnectCode = "Disconnect";

            /// <summary>
            /// String message saying: Reconnect
            /// </summary>
            public static string ReconnectCode = "Reconnect";

            /// <summary>
            /// Parses a given BrokerageMessageEvent object into a string containing basic information about it
            /// </summary>
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
            /// <summary>
            /// String message saying: Brokerage Error
            /// </summary>
            public static string BrokerageErrorContext = "Brokerage Error";

            /// <summary>
            /// String message saying: DefaultBrokerageMessageHandler.Handle(): Disconnected
            /// </summary>
            public static string Disconnected = "DefaultBrokerageMessageHandler.Handle(): Disconnected.";

            /// <summary>
            /// String message saying: DefaultBrookerageMessageHandler.Handle(): Reconnected
            /// </summary>
            public static string Reconnected = "DefaultBrokerageMessageHandler.Handle(): Reconnected.";

            /// <summary>
            /// String message saying: DefaultBrokerageMessageHandler.Handle(): Disconnect when exchanges are closed,
            /// checking back before exchange open
            /// </summary>
            public static string DisconnectedWhenExchangesAreClosed =
                "DefaultBrokerageMessageHandler.Handle(): Disconnect when exchanges are closed, checking back before exchange open.";

            /// <summary>
            /// String message saying: DefaultBrokerageMessageHandler.Handle(): Still disconnected, goodbye
            /// </summary>
            public static string StillDisconnected = "DefaultBrokerageMessageHandler.Handle(): Still disconnected, goodbye.";

            /// <summary>
            /// String message saying: Brokerage Disconnect
            /// </summary>
            public static string BrokerageDisconnectedShutDownContext = "Brokerage Disconnect";

            /// <summary>
            /// Returns a string message with basic information about the given message event
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string BrokerageInfo(Brokerages.BrokerageMessageEvent messageEvent)
            {
                return $"Brokerage Info: {messageEvent.Message}";
            }

            /// <summary>
            /// Returns a string message warning from the given message event
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string BrokerageWarning(Brokerages.BrokerageMessageEvent messageEvent)
            {
                return $"Brokerage Warning: {messageEvent.Message}";
            }

            /// <summary>
            /// Returns a string message saying the brokerage is disconnected when exchanges are open and that it's
            /// trying to reconnect for the given reconnection timeout minutes
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string DisconnectedWhenExchangesAreOpen(TimeSpan reconnectionTimeout)
            {
                return Invariant($@"DefaultBrokerageMessageHandler.Handle(): Disconnect when exchanges are open, trying to reconnect for {
                    reconnectionTimeout.TotalMinutes} minutes.");
            }

            /// <summary>
            /// Returns a string message with the time until the next market open
            /// </summary>
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
            /// <summary>
            /// String message saying: Order is null
            /// </summary>
            public static string NullOrder = "Order is null.";

            /// <summary>
            /// String message saying: Price is not set
            /// </summary>
            public static string PriceNotSet = "Price is not set.";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.FTXBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class FTXBrokerageModel
        {
            /// <summary>
            /// String message saying: Trigger price too high, must be below current market price
            /// </summary>
            public static string TriggerPriceTooHigh = "Trigger price too high: must be below current market price.";

            /// <summary>
            /// String message saying: Trigger price too low, must be above current market price
            /// </summary>
            public static string TriggerPriceTooLow = "Trigger price too low: must be above current market price.";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.FxcmBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class FxcmBrokerageModel
        {
            /// <summary>
            /// String message saying: Limit Buy orders and Stop Sell orders must be below market, Limit Sell orders and Stop Buy orders
            /// must be above market
            /// </summary>
            public static string InvalidOrderPrice =
                "Limit Buy orders and Stop Sell orders must be below market, Limit Sell orders and Stop Buy orders must be above market.";

            /// <summary>
            /// Returns a string message saying the order quantity must be a multiple of LotSize. It also contains the security's Lot
            /// Size
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidOrderQuantityForLotSize(Securities.Security security)
            {
                return Invariant($"The order quantity must be a multiple of LotSize: [{security.SymbolProperties.LotSize}].");
            }

            /// <summary>
            /// Returns a string message saying the order price is too far from the current market price
            /// </summary>
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
            /// <summary>
            /// String message saying: The Coinbase brokerage does not currently support Margin trading
            /// </summary>
            public static string UnsupportedAccountType = "The Coinbase brokerage does not currently support Margin trading.";

            /// <summary>
            /// Returns a string message saying the Stop Market orders are no longer supported since the given end date
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string StopMarketOrdersNoLongerSupported(DateTime stopMarketOrderSupportEndDate)
            {
                return Invariant($"Stop Market orders are no longer supported since {stopMarketOrderSupportEndDate}.");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.InteractiveBrokersFixModel"/> class and its consumers or related classes
        /// </summary>
        public static class InteractiveBrokersFixModel
        {
            /// <summary>
            /// Returns a string message saying the given brokerage model does not support order exercises
            /// for index and cash-settled options
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedComboOrdersForFutureOptions(Brokerages.InteractiveBrokersFixModel brokerageModel, Orders.Order order)
            {
                return Invariant($@"The {brokerageModel.GetType().Name} does not support {order.Type} for future options.");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.InteractiveBrokersBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class InteractiveBrokersBrokerageModel
        {
            /// <summary>
            /// Returns a string message saying the given brokerage model does not support order exercises
            /// for index and cash-settled options
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedExerciseForIndexAndCashSettledOptions(Brokerages.InteractiveBrokersBrokerageModel brokerageModel,
                Orders.Order order)
            {
                return Invariant($@"The {brokerageModel.GetType().Name} does not support {
                    order.Type} exercises for index and cash-settled options.");
            }

            /// <summary>
            /// Returns a string message containing the minimum and maximum limits for the allowable order size as well as the currency
            /// </summary>
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
            /// <summary>
            /// Unsupported Security Type string message
            /// </summary>
            public static string UnsupportedSecurityType = "This model only supports equities and options.";

            /// <summary>
            /// Unsupported Time In Force Type string message
            /// </summary>
            public static string UnsupportedTimeInForceType = $"This model only supports orders with the following time in force types: {typeof(DayTimeInForce)} and {typeof(GoodTilCanceledTimeInForce)}";

            /// <summary>
            /// Extended Market Hours Trading Not Supported string message
            /// </summary>
            public static string ExtendedMarketHoursTradingNotSupported =
                "Tradier does not support extended market hours trading. Your order will be processed at market open.";

            /// <summary>
            /// Order Quantity Update Not Supported string message
            /// </summary>
            public static string OrderQuantityUpdateNotSupported = "Tradier does not support updating order quantities.";

            /// <summary>
            /// Open Orders Cancel On Reverse Split Symbols string message
            /// </summary>
            public static string OpenOrdersCancelOnReverseSplitSymbols = "Tradier Brokerage cancels open orders on reverse split symbols";

            /// <summary>
            /// Short Order Is GTC string message
            /// </summary>
            public static string ShortOrderIsGtc = "You cannot place short stock orders with GTC, only day orders are allowed";

            /// <summary>
            /// Sell Short Order Last Price Below 5 string message
            /// </summary>
            public static string SellShortOrderLastPriceBelow5 = "Sell Short order cannot be placed for stock priced below $5";

            /// <summary>
            /// Incorrect Order Quantity string message
            /// </summary>
            public static string IncorrectOrderQuantity = "Quantity should be between 1 and 10,000,000";

            /// <summary>
            /// Extended Market Hours Trading Not Supported Outside Extended Session string message
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ExtendedMarketHoursTradingNotSupportedOutsideExtendedSession(Securities.MarketHoursSegment preMarketSegment,
                Securities.MarketHoursSegment postMarketSegment)
            {
                return "Tradier does not support explicitly placing out-of-regular-hours orders if not currently " +
                    $"during the pre or post market session. {preMarketSegment}. {postMarketSegment}. " +
                    "Only equity limit orders are allowed during extended market hours.";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.TradingTechnologiesBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class TradingTechnologiesBrokerageModel
        {
            /// <summary>
            /// Invalid Stop Market Order Price string message
            /// </summary>
            public static string InvalidStopMarketOrderPrice =
                "StopMarket Sell orders must be below market, StopMarket Buy orders must be above market.";

            /// <summary>
            /// Invalid Stop Limit Order Price string message
            /// </summary>
            public static string InvalidStopLimitOrderPrice =
                "StopLimit Sell orders must be below market, StopLimit Buy orders must be above market.";

            /// <summary>
            /// Invalid Stop Limit Order Limit Price string message
            /// </summary>
            public static string InvalidStopLimitOrderLimitPrice =
                "StopLimit Buy limit price must be greater than or equal to stop price, StopLimit Sell limit price must be smaller than or equal to stop price.";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Brokerages.WolverineBrokerageModel"/> class and its consumers or related classes
        /// </summary>
        public static class WolverineBrokerageModel
        {
            /// <summary>
            /// Returns a message for an unsupported order type in Wolverine Brokerage Model
            /// </summary>
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
            /// <summary>
            /// Returns a message for an unsupported order type in RBI Brokerage Model
            /// </summary>
            /// <param name="order"></param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedOrderType(Orders.Order order)
            {
                return Invariant($"{order.Type} order is not supported by RBI. Currently, only Market Order, Limit Order, StopMarket Order and StopLimit Order are supported.");
            }
        }
    }
}

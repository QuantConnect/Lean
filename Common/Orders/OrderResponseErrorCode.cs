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

namespace QuantConnect.Orders
{
    /// <summary>
    /// Error detail code
    /// </summary>
    public enum OrderResponseErrorCode
    {
        /// <summary>
        /// No error
        /// </summary>
        None = 0,

        /// <summary>
        /// Unknown error
        /// </summary>
        ProcessingError = -1,

        /// <summary>
        /// Cannot submit because order already exists
        /// </summary>
        OrderAlreadyExists = -2,

        /// <summary>
        /// Not enough money to to submit order
        /// </summary>
        InsufficientBuyingPower = -3,

        /// <summary>
        /// Internal logic invalidated submit order
        /// </summary>
        BrokerageModelRefusedToSubmitOrder = -4,

        /// <summary>
        /// Brokerage submit error
        /// </summary>
        BrokerageFailedToSubmitOrder = -5,

        /// <summary>
        /// Brokerage update error
        /// </summary>
        BrokerageFailedToUpdateOrder = -6,

        /// <summary>
        /// Internal logic invalidated update order
        /// </summary>
        BrokerageHandlerRefusedToUpdateOrder = -7,

        /// <summary>
        /// Brokerage cancel error
        /// </summary>
        BrokerageFailedToCancelOrder = -8,

        /// <summary>
        /// Only pending orders can be canceled
        /// </summary>
        InvalidOrderStatus = -9,

        /// <summary>
        /// Missing order
        /// </summary>
        UnableToFindOrder = -10,

        /// <summary>
        /// Cannot submit or update orders with zero quantity
        /// </summary>
        OrderQuantityZero = -11,

        /// <summary>
        /// This type of request is unsupported
        /// </summary>
        UnsupportedRequestType = -12,

        /// <summary>
        /// Unknown error during pre order request validation
        /// </summary>
        PreOrderChecksError = -13,

        /// <summary>
        /// Security is missing. Probably did not subscribe.
        /// </summary>
        MissingSecurity = -14,

        /// <summary>
        /// Some order types require open exchange
        /// </summary>
        ExchangeNotOpen = -15,

        /// <summary>
        /// Zero security price is probably due to bad data
        /// </summary>
        SecurityPriceZero = -16,

        /// <summary>
        /// Need both currencies in cashbook to trade a pair
        /// </summary>
        ForexBaseAndQuoteCurrenciesRequired = -17,

        /// <summary>
        /// Need conversion rate to account currency
        /// </summary>
        ForexConversionRateZero = -18,

        /// <summary>
        /// Should not attempt trading without at least one data point
        /// </summary>
        SecurityHasNoData = -19,

        /// <summary>
        /// Transaction manager's cache is full
        /// </summary>
        ExceededMaximumOrders = -20,

        /// <summary>
        /// Need 11 minute buffer before exchange close
        /// </summary>
        MarketOnCloseOrderTooLate = -21,

        /// <summary>
        /// Request is invalid or null
        /// </summary>
        InvalidRequest = -22,

        /// <summary>
        /// Request was canceled by user
        /// </summary>
        RequestCanceled = -23,

        /// <summary>
        /// All orders are invalidated while algorithm is warming up
        /// </summary>
        AlgorithmWarmingUp = -24,

        /// <summary>
        /// Internal logic invalidated update order
        /// </summary>
        BrokerageModelRefusedToUpdateOrder = -25,

        /// <summary>
        /// Need quote currency in cashbook to trade
        /// </summary>
        QuoteCurrencyRequired = -26,

        /// <summary>
        /// Need conversion rate to account currency
        /// </summary>
        ConversionRateZero = -27,

        /// <summary>
        /// The order's symbol references a non-tradable security
        /// </summary>
        NonTradableSecurity = -28,

        /// <summary>
        /// The order's symbol references a non-exercisable security
        /// </summary>
        NonExercisableSecurity = -29,

        /// <summary>
        /// Cannot submit or update orders with quantity that is less than lot size
        /// </summary>
        OrderQuantityLessThanLoteSize = -30,

        /// <summary>
        /// The order's quantity exceeds the max shortable quantity set by the brokerage
        /// </summary>
        ExceedsShortableQuantity = -31,
    }
}

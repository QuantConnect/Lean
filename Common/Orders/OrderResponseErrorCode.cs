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
        /// No error (0)
        /// </summary>
        None = 0,

        /// <summary>
        /// Unknown error (-1)
        /// </summary>
        ProcessingError = -1,

        /// <summary>
        /// Cannot submit because order already exists (-2)
        /// </summary>
        OrderAlreadyExists = -2,

        /// <summary>
        /// Not enough money to to submit order (-3)
        /// </summary>
        InsufficientBuyingPower = -3,

        /// <summary>
        /// Internal logic invalidated submit order (-4)
        /// </summary>
        BrokerageModelRefusedToSubmitOrder = -4,

        /// <summary>
        /// Brokerage submit error (-5)
        /// </summary>
        BrokerageFailedToSubmitOrder = -5,

        /// <summary>
        /// Brokerage update error (-6)
        /// </summary>
        BrokerageFailedToUpdateOrder = -6,

        /// <summary>
        /// Internal logic invalidated update order (-7)
        /// </summary>
        BrokerageHandlerRefusedToUpdateOrder = -7,

        /// <summary>
        /// Brokerage cancel error (-8)
        /// </summary>
        BrokerageFailedToCancelOrder = -8,

        /// <summary>
        /// Only pending orders can be canceled (-9)
        /// </summary>
        InvalidOrderStatus = -9,

        /// <summary>
        /// Missing order (-10)
        /// </summary>
        UnableToFindOrder = -10,

        /// <summary>
        /// Cannot submit or update orders with zero quantity (-11)
        /// </summary>
        OrderQuantityZero = -11,

        /// <summary>
        /// This type of request is unsupported (-12)
        /// </summary>
        UnsupportedRequestType = -12,

        /// <summary>
        /// Unknown error during pre order request validation (-13)
        /// </summary>
        PreOrderChecksError = -13,

        /// <summary>
        /// Security is missing. Probably did not subscribe (-14)
        /// </summary>
        MissingSecurity = -14,

        /// <summary>
        /// Some order types require open exchange (-15)
        /// </summary>
        ExchangeNotOpen = -15,

        /// <summary>
        /// Zero security price is probably due to bad data (-16)
        /// </summary>
        SecurityPriceZero = -16,

        /// <summary>
        /// Need both currencies in cashbook to trade a pair (-17)
        /// </summary>
        ForexBaseAndQuoteCurrenciesRequired = -17,

        /// <summary>
        /// Need conversion rate to account currency (-18)
        /// </summary>
        ForexConversionRateZero = -18,

        /// <summary>
        /// Should not attempt trading without at least one data point (-19)
        /// </summary>
        SecurityHasNoData = -19,

        /// <summary>
        /// Transaction manager's cache is full (-20)
        /// </summary>
        ExceededMaximumOrders = -20,

        /// <summary>
        /// Below buffer time for MOC order to be placed before exchange closes. 15.5 minutes by default (-21)
        /// </summary>
        MarketOnCloseOrderTooLate = -21,

        /// <summary>
        /// Request is invalid or null (-22)
        /// </summary>
        InvalidRequest = -22,

        /// <summary>
        /// Request was canceled by user (-23)
        /// </summary>
        RequestCanceled = -23,

        /// <summary>
        /// All orders are invalidated while algorithm is warming up (-24)
        /// </summary>
        AlgorithmWarmingUp = -24,

        /// <summary>
        /// Internal logic invalidated update order (-25)
        /// </summary>
        BrokerageModelRefusedToUpdateOrder = -25,

        /// <summary>
        /// Need quote currency in cashbook to trade (-26)
        /// </summary>
        QuoteCurrencyRequired = -26,

        /// <summary>
        /// Need conversion rate to account currency (-27)
        /// </summary>
        ConversionRateZero = -27,

        /// <summary>
        /// The order's symbol references a non-tradable security (-28)
        /// </summary>
        NonTradableSecurity = -28,

        /// <summary>
        /// The order's symbol references a non-exercisable security (-29)
        /// </summary>
        NonExercisableSecurity = -29,

        /// <summary>
        /// Cannot submit or update orders with quantity that is less than lot size (-30)
        /// </summary>
        OrderQuantityLessThanLotSize = -30,

        /// <summary>
        /// The order's quantity exceeds the max shortable quantity set by the brokerage (-31)
        /// </summary>
        ExceedsShortableQuantity = -31,

        /// <summary>
        /// Cannot update/cancel orders with OrderStatus.New (-32)
        /// </summary>
        InvalidNewOrderStatus = -32,

        /// <summary>
        /// Exercise time before expiry for European options (-33)
        /// </summary>
        EuropeanOptionNotExpiredOnExercise = -33,

        /// <summary>
        /// Option order is invalid due to underlying stock split (-34)
        /// </summary>
        OptionOrderOnStockSplit = -34
    }
}

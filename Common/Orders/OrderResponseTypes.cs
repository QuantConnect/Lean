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

namespace QuantConnect.Orders
{
    /// <summary>
    /// Order Response Type
    /// </summary>
    public enum OrderResponseType
    {
        /// <summary>
        /// Uninitialized
        /// </summary>
        None,
        /// <summary>
        /// Success
        /// </summary>
        Processed,
        /// <summary>
        /// Failure
        /// </summary>
        Error
    }

    /// <summary>
    /// Error detail code
    /// </summary>
    public enum OrderResponseErrorCode
    {
        /// <summary>
        /// No error
        /// </summary>
        None,
        /// <summary>
        /// Unknown error
        /// </summary>
        ProcessingError,
        /// <summary>
        /// Cannot submit because order already exists
        /// </summary>
        OrderAlreadyExists,
        /// <summary>
        /// Not enough money to to submit order 
        /// </summary>
        InsufficientBuyingPower,
        /// <summary>
        /// Internal logic invalidated submit order
        /// </summary>
        BrokerageModelRefusedToSubmitOrder,
        /// <summary>
        /// Brokerage submit error
        /// </summary>
        BrokerageFailedToSubmitOrder,
        /// <summary>
        /// Brokerage update error
        /// </summary>
        BrokerageFailedToUpdateOrder,
        /// <summary>
        /// Internal logic invalidated update order
        /// </summary>
        BrokerageHandlerRefusedToUpdateOrder,
        /// <summary>
        /// Brokerage cancel error
        /// </summary>
        BrokerageFailedToCancelOrder,
        /// <summary>
        /// Only pending orders can be canceled
        /// </summary>
        InvalidOrderStatus,
        /// <summary>
        /// Missing order
        /// </summary>
        UnableToFindOrder,
        /// <summary>
        /// Cannot submit or update orders with zero quantity
        /// </summary>
        OrderQuantityZero,
        /// <summary>
        /// This type of request is unsupported
        /// </summary>
        UnsupportedRequestType,
        /// <summary>
        /// Unknown error during pre order request validation
        /// </summary>
        PreOrderChecksError,
        /// <summary>
        /// Security is missing. Probably did not subscribe.
        /// </summary>
        MissingSecurity,
        /// <summary>
        /// Some order types require open exchange
        /// </summary>
        ExchangeNotOpen,
        /// <summary>
        /// Zero security price is probably due to bad data
        /// </summary>
        SecurityPriceZero,
        /// <summary>
        /// Need both currencies in cashbook to trade a pair
        /// </summary>
        ForexBaseAndQuoteCurrenciesRequired,
        /// <summary>
        /// Need conversion rate to account currency
        /// </summary>
        ForexConversionRateZero,
        /// <summary>
        /// Should not attempt trading without at least one data point
        /// </summary>
        SecurityHasNoData,
        /// <summary>
        /// Transaction manager's cache is full
        /// </summary>
        ExceededMaximumOrders,
        /// <summary>
        /// Need 11 minute buffer before exchange close
        /// </summary>
        MarketOnCloseOrderTooLate

    }
}

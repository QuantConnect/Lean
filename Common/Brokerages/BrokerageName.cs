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

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Specifices what transaction model and submit/execution rules to use
    /// </summary>
    public enum BrokerageName
    {
        /// <summary>
        /// Transaction and submit/execution rules will be the default as initialized
        /// </summary>
        Default,

        /// <summary>
        /// Transaction and submit/execution rules will be the default as initialized
        /// Alternate naming for default brokerage
        /// </summary>
        QuantConnectBrokerage = Default,

        /// <summary>
        /// Transaction and submit/execution rules will use interactive brokers models
        /// </summary>
        InteractiveBrokersBrokerage,

        /// <summary>
        /// Transaction and submit/execution rules will use tradier models
        /// </summary>
        TradierBrokerage,

        /// <summary>
        /// Transaction and submit/execution rules will use oanda models
        /// </summary>
        OandaBrokerage,

        /// <summary>
        /// Transaction and submit/execution rules will use fxcm models
        /// </summary>
        FxcmBrokerage,

        /// <summary>
        /// Transaction and submit/execution rules will use bitfinex models
        /// </summary>
        Bitfinex,

        /// <summary>
        /// Transaction and submit/execution rules will use gdax models
        /// </summary>
        GDAX = 12,

        /// <summary>
        /// Transaction and submit/execution rules will use alpaca models
        /// </summary>
        Alpaca,

        /// <summary>
        /// Transaction and submit/execution rules will use AlphaStream models
        /// </summary>
        AlphaStreams
    }
}

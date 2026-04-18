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
        /// Transaction and submit/execution rules will use binance models
        /// </summary>
        Binance,

        /// <summary>
        /// Transaction and submit/execution rules will use gdax models
        /// </summary>
        [Obsolete("GDAX brokerage name is deprecated. Use Coinbase instead.")]
        GDAX = 12,

        /// <summary>
        /// Transaction and submit/execution rules will use alpaca models
        /// </summary>
        Alpaca,

        /// <summary>
        /// Transaction and submit/execution rules will use AlphaStream models
        /// </summary>
        AlphaStreams,

        /// <summary>
        /// Transaction and submit/execution rules will use Zerodha models
        /// </summary>
        Zerodha,

        /// <summary>
        /// Transaction and submit/execution rules will use Samco models
        /// </summary>
        Samco,

        /// <summary>
        /// Transaction and submit/execution rules will use atreyu models
        /// </summary>
        Atreyu,

        /// <summary>
        /// Transaction and submit/execution rules will use TradingTechnologies models
        /// </summary>
        TradingTechnologies,

        /// <summary>
        /// Transaction and submit/execution rules will use Kraken models
        /// </summary>
        Kraken,

        /// <summary>
        /// Transaction and submit/execution rules will use ftx models
        /// </summary>
        FTX,

        /// <summary>
        /// Transaction and submit/execution rules will use ftx us models
        /// </summary>
        FTXUS,

        /// <summary>
        /// Transaction and submit/execution rules will use Exante models
        /// </summary>
        Exante,

        /// <summary>
        /// Transaction and submit/execution rules will use Binance.US models
        /// </summary>
        BinanceUS,

        /// <summary>
        /// Transaction and submit/execution rules will use Wolverine models
        /// </summary>
        Wolverine,

        /// <summary>
        /// Transaction and submit/execution rules will use TDameritrade models
        /// </summary>
        TDAmeritrade,

        /// <summary>
        /// Binance Futures USDⓈ-Margined contracts are settled and collateralized in their quote cryptocurrency, USDT or BUSD
        /// </summary>
        BinanceFutures,

        /// <summary>
        /// Binance Futures COIN-Margined contracts are settled and collateralized in their based cryptocurrency.
        /// </summary>
        BinanceCoinFutures,

        /// <summary>
        /// Transaction and submit/execution rules will use RBI models
        /// </summary>
        RBI,

        /// <summary>
        /// Transaction and submit/execution rules will use Bybit models
        /// </summary>
        Bybit,

        /// <summary>
        /// Transaction and submit/execution rules will use Eze models
        /// </summary>
        Eze,

        /// <summary>
        /// Transaction and submit/execution rules will use Axos models
        /// </summary>
        Axos,
        
        /// <summary>
        /// Transaction and submit/execution rules will use Coinbase broker's model
        /// </summary>
        Coinbase,

        /// <summary>
        /// Transaction and submit/execution rules will use TradeStation models
        /// </summary>
        TradeStation,

        /// <summary>
        /// Transaction and submit/execution rules will use Terminal link models
        /// </summary>
        TerminalLink,

        /// <summary>
        /// Transaction and submit/execution rules will use Charles Schwab models
        /// </summary>
        CharlesSchwab,

        /// <summary>
        /// Transaction and submit/execution rules will use Tastytrade models
        /// </summary>
        Tastytrade,

        /// <summary>
        /// Transaction and submit/execution rules will use interactive brokers Fix models
        /// </summary>
        InteractiveBrokersFix
    }
}

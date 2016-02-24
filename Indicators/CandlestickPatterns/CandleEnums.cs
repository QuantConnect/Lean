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

namespace QuantConnect.Indicators.CandlestickPatterns
{
    /// <summary>
    /// Types of candlestick settings
    /// </summary>
    public enum CandleSettingType
    {
        /// <summary>
        /// Real body is long when it's longer than the average of the 10 previous candles' real body
        /// </summary>
        BodyLong,

        /// <summary>
        /// Real body is very long when it's longer than 3 times the average of the 10 previous candles' real body
        /// </summary>
        BodyVeryLong,

        /// <summary>
        /// Real body is short when it's shorter than the average of the 10 previous candles' real bodies
        /// </summary>
        BodyShort,

        /// <summary>
        /// Real body is like doji's body when it's shorter than 10% the average of the 10 previous candles' high-low range
        /// </summary>
        BodyDoji,

        /// <summary>
        /// Shadow is long when it's longer than the real body
        /// </summary>
        ShadowLong,

        /// <summary>
        /// Shadow is very long when it's longer than 2 times the real body
        /// </summary>
        ShadowVeryLong,

        /// <summary>
        /// Shadow is short when it's shorter than half the average of the 10 previous candles' sum of shadows
        /// </summary>
        ShadowShort,

        /// <summary>
        /// Shadow is very short when it's shorter than 10% the average of the 10 previous candles' high-low range
        /// </summary>
        ShadowVeryShort,

        /// <summary>
        /// When measuring distance between parts of candles or width of gaps
        /// "near" means "&lt;= 20% of the average of the 5 previous candles' high-low range"
        /// </summary>
        Near,

        /// <summary>
        /// When measuring distance between parts of candles or width of gaps
        /// "far" means "&gt;= 60% of the average of the 5 previous candles' high-low range"
        /// </summary>
        Far,

        /// <summary>
        /// When measuring distance between parts of candles or width of gaps
        /// "equal" means "&lt;= 5% of the average of the 5 previous candles' high-low range"
        /// </summary>
        Equal
    }

    /// <summary>
    /// Types of candlestick ranges
    /// </summary>
    public enum CandleRangeType
    {
        /// <summary>
        /// The part of the candle between open and close
        /// </summary>
        RealBody,

        /// <summary>
        /// The complete range of the candle
        /// </summary>
        HighLow,

        /// <summary>
        /// The shadows (or tails) of the candle
        /// </summary>
        Shadows
    }

    /// <summary>
    /// Colors of a candle
    /// </summary>
    public enum CandleColor
    {
        /// <summary>
        /// White is an up candle (close higher or equal than open)
        /// </summary>
        White = 1,

        /// <summary>
        /// Black is a down candle (close lower than open)
        /// </summary>
        Black = -1
    }
}

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
    /// Type of the order: market, limit or stop
    /// </summary>
    public enum OrderType 
    {
        /// <summary>
        /// Market Order Type
        /// </summary>
        Market,

        /// <summary>
        /// Limit Order Type
        /// </summary>
        Limit,

        /// <summary>
        /// Stop Market Order Type - Fill at market price when break target price
        /// </summary>
        StopMarket,

        /// <summary>
        /// Stop limit order type - trigger fill once pass the stop price; but limit fill to limit price.
        /// </summary>
        StopLimit,

        /// <summary>
        /// Market on open type - executed on exchange open
        /// </summary>
        MarketOnOpen,

        /// <summary>
        /// Market on close type - executed on exchange close
        /// </summary>
        MarketOnClose,

        /// <summary>
        /// Option Exercise Order Type
        /// </summary>
        OptionExercise
    }


    /// <summary>
    /// Order duration in market
    /// </summary>
    public enum OrderDuration
    {
        /// <summary>
        /// Order good until its filled.
        /// </summary>
        GTC,

        /*
        /// <summary>
        /// Order valid for today only: -- CURRENTLY ONLY GTC ORDER DURATION TYPE IN BACKTESTS.
        /// </summary>
        Day
        */

        /// <summary>
        /// Order valid until a custom set date time value.
        /// </summary>
        Custom
        
    }


    /// <summary>
    /// Direction of the order
    /// </summary>
    public enum OrderDirection {

        /// <summary>
        /// Buy Order 
        /// </summary>
        Buy,

        /// <summary>
        /// Sell Order
        /// </summary>
        Sell,

        /// <summary>
        /// Default Value - No Order Direction
        /// </summary>
        /// <remarks>
        /// Unfortunately this does not have a value of zero because
        /// there are backtests saved that reference the values in this order
        /// </remarks>
        Hold
    }


    /// <summary>
    /// Fill status of the order class.
    /// </summary>
    public enum OrderStatus {
        
        /// <summary>
        /// New order pre-submission to the order processor.
        /// </summary>
        New = 0,

        /// <summary>
        /// Order submitted to the market
        /// </summary>
        Submitted = 1,

        /// <summary>
        /// Partially filled, In Market Order.
        /// </summary>
        PartiallyFilled = 2,

        /// <summary>
        /// Completed, Filled, In Market Order.
        /// </summary>
        Filled = 3,

        /// <summary>
        /// Order cancelled before it was filled
        /// </summary>
        Canceled = 5,

        /// <summary>
        /// No Order State Yet
        /// </summary>
        None = 6,

        /// <summary>
        /// Order invalidated before it hit the market (e.g. insufficient capital)..
        /// </summary>
        Invalid = 7,

        /// <summary>
        /// Order waiting for confirmation of cancellation
        /// </summary>
        CancelPending = 8
    }

    /// <summary>
    /// Interactive Brokers algorithm selection
    /// </summary>
    public enum OrderAlgorithm
    {
        /// <summary>
        /// No algorithm
        /// </summary>
        None,
        /// <summary>
        /// The Adaptive Algo combines IB's Smartrouting capabilities with user-defined priority settings in an effort to achieve further cost efficiency at the point of execution. Using the Adaptive algo leads to better execution prices on average than for regular limit or market orders.
        /// </summary>
        AdaptiveAlgo,
        /// <summary>
        /// The Arrival Price algorithmic order type will attempt to achieve, over the course of the order, the bid/ask midpoint at the time the order is submitted. The Arrival Price algo is designed to keep hidden orders that will impact a high percentage of the average daily volume (ADV).
        /// </summary>
        ArrivalPrice,
        /// <summary>
        /// In order to help investors attempting to execute towards the end of the trading session we have developed the Close Price algo Strategy. This algo breaks down large order amounts and determines the timing of order entry so that it will continuously execute in order to minimize slippage.
        /// </summary>
        ClosePrice,
        /// <summary>
        /// The Dark Ice order type develops the concept of privacy adopted by orders such as Iceberg or Reserve, using a proprietary algorithm to further hide the volume displayed to the market by the order.
        /// </summary>
        DarkIce,
        /// <summary>
        /// The Accumulate/Distribute algo can help you to achieve the best price for a large volume order without being noticed in the market, and can be set up for high frequency trading.
        /// </summary>
        AccumulateDistribute,
        /// <summary>
        /// The Percent of Volume algo can limit the contribution of orders to overall average daily volume in order to minimize impact.
        /// </summary>
        PercentageOfVolume,
        /// <summary>
        /// The TWAP algo aims to achieve the time-weighted average price calculated from the time you submit the order to the time it completes.
        /// </summary>
        TWAP,
        /// <summary>
        /// This algo allows you to participate in volume at a user-defined rate that varies over time depending on the market price of the security.
        /// </summary>
        PriceVariantPercentageOfVolumeStrategy,
        /// <summary>
        /// This algo allows you to participate in volume at a user-defined rate that varies over time depending on the remaining size of the order.
        /// </summary>
        SizeVariantPercentageOfVolumeStrategy,
        /// <summary>
        /// This algo allows you to participate in volume at a user-defined rate that varies with time.
        /// </summary>
        TimeVariantPercentageOfVolumeStrategy,
        /// <summary>
        /// IB's best-efforts VWAP algo seeks to achieve the Volume-Weighted Average price (VWAP), calculated from the time you submit the order to the close of the market.
        /// </summary>
        VWAP,
        /// <summary>
        /// The Balance Impact Risk balances the market impact of trading the option with the risk of price change over the time horizon of the order.
        /// </summary>
        BalanceImpactRisk,
        /// <summary>
        /// The Minimise Impact algo minimises market impact by slicing the order over time to achieve a market average without going over the given maximum percentage value.
        /// </summary>
        MinimiseImpact
    }

    /// <summary>
    /// Storage of Interactive Brokers algorithm parameters
    /// </summary>
    public class AlgoParams
    {
        /// <summary>
        /// For setting the options for the algorithm. See http://interactivebrokers.github.io/tws-api/ibalgos.html
        /// </summary>
        /// <param name="tag">Tag of the algorithm parameter</param>
        /// <param name="value">Value of the algorithm parameter</param>
        public AlgoParams(string tag, string value)
        {
            this.Tag = tag;
            this.Value = value;
        }
        /// <summary>
        /// Tag of the algorithm parameter
        /// </summary>
        public string Tag { get; set; }
        /// <summary>
        /// Value of the algorithm parameter
        /// </summary>
        public string Value { get; set; }
    }
} // End QC Namespace:

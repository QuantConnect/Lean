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
using System.ComponentModel;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Flag system for trade conditions
    /// </summary>
    [Flags]
    public enum TradeConditionFlags: long
    {
        None = 0,

        [Description("A trade made without stated conditions is deemed regular way for settlement on the third business day following the transaction date.")]
        Regular = 1L << 0,
        [Description("A transaction which requires delivery of securities and payment on the same day the trade takes place.")]
        Cash = 1L << 1,

        [Description("A transaction that requires the delivery of securities on the first business day following the trade date.")]
        NextDay = 1L << 2,

        [Description("A Seller’s Option transaction gives the seller the right to deliver the security at any time within a specific period, " +
                     "ranging from not less than two calendar days, to not more than sixty calendar days.")]
        Seller = 1L << 3,

        [Description("Market Centers will have the ability to identify regular trades being reported during specific events as out of the ordinary " +
                     "by appending a new sale condition code Yellow Flag (Y) on each transaction reported to the UTP SIP." +
                     "The new sale condition will be eligible to update all market center and consolidated statistics.")]
        YellowFlag = 1L << 4,

        [Description("The transaction that constituted the trade-through was the execution of an order identified as an Intermarket Sweep Order.")]
        IntermarketSweep = 1L << 5,

        [Description("The trade that constituted the trade-through was a single priced opening transaction by the Market Center.")]
        OpeningPrints = 1L << 6,

        [Description("The transaction that constituted the trade-through was a single priced closing transaction by the Market Center.")]
        ClosingPrints = 1L << 7,

        [Description("The trade that constituted the trade-through was a single priced reopening transaction by the Market Center.")]
        ReOpeningPrints = 1L << 8,

        [Description("The transaction that constituted the trade-through was the execution of an order at a price that was not based, directly or indirectly, " +
                     "on the quoted price of the security at the time of execution and for which the material terms were not reasonably determinable " +
                     "at the time the commitment to execute the order was made.")]
        DerivativelyPriced = 1L << 9,

        [Description("Trading in extended hours enables investors to react quickly to events that typically occur outside regular market hours, such as earnings reports." +
                     "However, liquidity may be constrained during such Form T trading, resulting in wide bid-ask spreads.")]
        FormT = 1L << 10,

        [Description("Sold Last is used when a trade prints in sequence but is reported late or printed in conformance to the One or Two Point Rule.")]
        Sold = 1L << 11,

        [Description("The transaction that constituted the trade-through was the execution by a trading center of an order for which, at the time" +
                     "of receipt of the order, the execution at no worse than a specified price a 'stopped order'")]
        Stopped = 1L << 12,

        [Description("Identifies a trade that was executed outside of regular primary market hours and is reported as an extended hours trade.")]
        ExtendedHours = 1L << 13,

        [Description("Identifies a trade that takes place outside of regular market hours.")]
        OutOfSequence = 1L << 14,

        [Description("An execution in two markets when the specialist or Market Maker in the market first receiving the order agrees to execute a portion of it " +
                     "at whatever price is realized in another market to which the balance of the order is forwarded for execution.")]
        Split = 1L << 15,

        [Description("A transaction made on the Exchange as a result of an Exchange acquisition.")]
        Acquisition = 1L << 16,

        [Description("A trade representing an aggregate of two or more regular trades in a security occurring at the same price either simultaneously " +
                     "or within the same 60-second period, with no individual trade exceeding 10,000 shares.")]
        Bunched = 1L << 17,

        [Description("Stock-Option Trade is used to identify cash equity transactions which are related to options transactions and therefore" +
                     "potentially subject to cancellation if market conditions of the options leg(s) prevent the execution of the stock-option" +
                     "order at the price agreed upon.")]
        StockOption = 1L << 18,

        [Description("Sale of a large block of stock in such a manner that the price is not adversely affected.")]
        Distribution = 1L << 19,

        [Description("A trade where the price reported is based upon an average of the prices for transactions in a security during all or any portion of the trading day.")]
        AveragePrice = 1L << 20,

        [Description("Indicates that the trade resulted from a Market Center’s crossing session.")]
        Cross = 1L << 21,

        [Description("Indicates a regular market session trade transaction that carries a price that is significantly away from the prevailing consolidated or primary market value at the time of the transaction.")]
        PriceVariation = 1L << 22,

        [Description("To qualify as a NYSE AMEX Rule 155")]
        Rule155 = 1L << 23,

        [Description("Indicates the ‘Official’ closing value as determined by a Market Center. This transaction report will contain the market center generated closing price.")]
        OfficialClose = 1L << 24,

        [Description("A sale condition that identifies a trade based on a price at a prior point in time i.e. more than 90 seconds prior to the time of the trade report. " +
                     "The execution time of the trade will be the time of the prior reference price.")]
        PriorReferencePrice = 1L << 25,

        [Description("Indicates the ‘Official’ open value as determined by a Market Center. This transaction report will contain the market")]
        OfficialOpen = 1L << 26,

        [Description("The CAP Election Trade highlights sales as a result of a sweep execution on the NYSE, whereby CAP orders have been elected and executed " +
                     "outside the best price bid or offer and the orders appear as repeat trades at subsequent execution prices. " +
                     "This indicator provides additional information to market participants that an automatic sweep transaction has occurred with repeat " +
                     "trades as one continuous electronic transaction.")]
        CapElection = 1L << 27,

        [Description("A sale condition code that identifies a NYSE trade that has been automatically executed without the potential benefit of price improvement.")]
        AutoExecution = 1L << 28,

        [Description("Denotes whether or not a trade is exempt (Rule 611) and when used jointly with certain Sale Conditions, " +
                     "will more fully describe the characteristics of a particular trade.")]
        TradeThroughExempt = 1L << 29,

        [Description("This flag is present in raw data, but AlgoSeek document does not describe it.")]
        UndocumentedFlag = 1L << 30,

        [Description("Denotes the trade is an odd lot less than a 100 shares.")]
        OddLot = 1L << 31,
    }
}

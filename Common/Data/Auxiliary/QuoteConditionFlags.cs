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
    /// Flag system for quote conditions
    /// </summary>
    [Flags]
    public enum QuoteConditionFlags : long
    {
        None = 0,

        [Description("This condition is used for the majority of quotes to indicate a normal trading environment.")]
        Regular = 1L << 0,

        [Description("This condition is used to indicate that the quote is a Slow Quote on both the Bid and Offer " +
            "sides due to a Set Slow List that includes High Price securities.")]
        Slow = 1L << 1,

        [Description("While in this mode, auto-execution is not eligible, the quote is then considered manual and non-firm in the Bid and Offer and " +
            "either or both sides can be traded through as per Regulation NMS.")]
        Gap = 1L << 2,

        [Description("This condition can be disseminated to indicate that this quote was the last quote for a security for that Participant.")]
        Closing = 1L << 3,

        [Description("This regulatory Opening Delay or Trading Halt is used when relevant news influencing the security is being disseminated." +
            "Trading is suspended until the primary market determines that an adequate publication or disclosure of information has occurred.")]
        NewsDissemination = 1L << 4,

        [Description("This condition is used to indicate a regulatory Opening Delay or Trading Halt due to an expected news announcement, " +
            "which may influence the security. An Opening Delay or Trading Halt may be continued once the news has been disseminated.")]
        NewsPending = 1L << 5,

        [Description("The condition is used to denote the probable trading range (bid and offer prices, no sizes) of a security that is not Opening Delayed or" +
            "Trading Halted. The Trading Range Indication is used prior to or after the opening of a security.")]
        TradingRangeIndication = 1L << 6,

        [Description("This non-regulatory Opening Delay or Trading Halt is used when there is a significant imbalance of buy or sell orders.")]
        OrderImbalance = 1L << 7,

        [Description("This condition is disseminated by each individual FINRA Market Maker to signify either the last quote of the day or" +
            "the premature close of an individual Market Maker for the day.")]
        ClosedMarketMaker = 1L << 8,

        [Description("This quote condition indicates a regulatory Opening Delay or Trading Halt due to conditions in which " +
            "a security experiences a 10 % or more change in price over a five minute period.")]
        VolatilityTradingPause = 1L << 9,

        [Description("This quote condition suspends a Participant's firm quote obligation for a quote for a security.")]
        NonFirmQuote = 1L << 10,

        [Description("This condition can be disseminated to indicate that this quote was the opening quote for a security for that Participant.")]
        OpeningQuote = 1L << 11,

        [Description("This non-regulatory Opening Delay or Trading Halt is used when events relating to one security will affect the price and performance of " +
            "another related security. This non-regulatory Opening Delay or Trading Halt is also used when non-regulatory halt reasons such as " +
            "Order Imbalance, Order Influx and Equipment Changeover are combined with Due to Related Security on CTS.")]
        DueToRelatedSecurity = 1L << 12,

        [Description("This quote condition along with zero-filled bid, offer and size fields is used to indicate that trading for a Participant is no longer " +
            "suspended in a security which had been Opening Delayed or Trading Halted.")]
        Resume = 1L << 13,

        [Description("This quote condition is used when matters affecting the common stock of a company affect the performance of the non-common " +
            "associated securities, e.g., warrants, rights, preferred, classes, etc.")]
        InViewOfCommon = 1L << 14,

        [Description("This non-regulatory Opening Delay or Trading Halt is used when the ability to trade a security by a Participant is temporarily " +
            "inhibited due to a systems, equipment or communications facility problem or for other technical reasons.")]
        EquipmentChangeover = 1L << 15,

        [Description("This non-regulatory Opening Delay or Trading Halt is used to indicate an Opening Delay or Trading Halt for a security whose price" +
            " may fall below $1.05, possibly leading to a sub-penny execution.")]
        SubPennyTrading = 1L << 16,

        [Description("This quote condition is used to indicate that an Opening Delay or a Trading Halt is to be in effect for the rest " +
            "of the trading day in a security for a Participant.")]
        NoOpenNoResume = 1L << 17,

        [Description("This quote condition is used to indicate that a Limit Up-Limit Down Price Band is applicable for a security.")]
        LimitUpLimitDownPriceBand = 1L << 18,

        [Description("This quote condition is used to indicate that a Limit Up-Limit Down Price Band that is being disseminated " +
            "is a ‘republication’ of the latest Price Band for a security.")]
        RepublishedLimitUpLimitDownPriceBand = 1L << 19,

        [Description("This indicates that the market participant is in a manual mode on both the Bid and Ask. While in this mode, " +
            "automated execution is not eligible on the Bid and Ask side and can be traded through pursuant to Regulation NMS requirements.")]
        Manual = 1L << 20,

        [Description("For extremely active periods of short duration. While in this mode, the UTP participant will enter quotations on a “best efforts” basis.")]
        FastTrading = 1L << 21,

        [Description("A halt condition used when there is a sudden order influx. To prevent a disorderly market, trading is temporarily suspended by the UTP participant.")]
        OrderInflux = 1L << 22
    }
}

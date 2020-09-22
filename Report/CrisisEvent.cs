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

namespace QuantConnect.Report
{
    /// <summary>
    /// Crisis Events
    /// </summary>
    public enum CrisisEvent
    {
        /// <summary>
        /// DotCom bubble - https://en.wikipedia.org/wiki/Dot-com_bubble
        /// </summary>
        DotCom,

        /// <summary>
        /// September 11, 2001 attacks - https://en.wikipedia.org/wiki/September_11_attacks
        /// </summary>
        SeptemberEleventh,

        /// <summary>
        /// United States housing bubble - https://en.wikipedia.org/wiki/United_States_housing_bubble
        /// </summary>
        USHousingBubble2003,

        /// <summary>
        /// https://en.wikipedia.org/wiki/Financial_crisis_of_2007%E2%80%9308
        /// </summary>
        GlobalFinancialCrisis,

        /// <summary>
        /// The flash crash of 2010 - https://en.wikipedia.org/wiki/2010_Flash_Crash
        /// </summary>
        FlashCrash,

        /// <summary>
        /// Fukushima nuclear power plant meltdown - https://en.wikipedia.org/wiki/Fukushima_Daiichi_nuclear_disaster
        /// </summary>
        FukushimaMeltdown,

        /// <summary>
        /// United States credit rating downgrade - https://en.wikipedia.org/wiki/United_States_federal_government_credit-rating_downgrades
        /// European debt crisis - https://en.wikipedia.org/wiki/European_debt_crisis
        /// </summary>
        USDowngradeEuropeanDebt,

        /// <summary>
        /// European debt crisis - https://en.wikipedia.org/wiki/European_debt_crisis
        /// </summary>
        EurozoneSeptember2012,

        /// <summary>
        /// European debt crisis - https://en.wikipedia.org/wiki/European_debt_crisis
        /// </summary>
        EurozoneOctober2014,

        /// <summary>
        /// 2015-2016 market sell off https://en.wikipedia.org/wiki/2015%E2%80%9316_stock_market_selloff
        /// </summary>
        MarketSellOff2015,

        /// <summary>
        /// Crisis recovery (2010 - 2012)
        /// </summary>
        Recovery,

        /// <summary>
        /// 2014 - 2019 market performance
        /// </summary>
        NewNormal,

        /// <summary>
        /// COVID-19 pandemic market crash
        /// </summary>
        COVID19
    }
}

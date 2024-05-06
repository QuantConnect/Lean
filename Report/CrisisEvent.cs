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
        /// DotCom bubble - https://en.wikipedia.org/wiki/Dot-com_bubble (0)
        /// </summary>
        DotCom,

        /// <summary>
        /// September 11, 2001 attacks - https://en.wikipedia.org/wiki/September_11_attacks (1)
        /// </summary>
        SeptemberEleventh,

        /// <summary>
        /// United States housing bubble - https://en.wikipedia.org/wiki/United_States_housing_bubble (2)
        /// </summary>
        USHousingBubble2003,

        /// <summary>
        /// https://en.wikipedia.org/wiki/Financial_crisis_of_2007%E2%80%9308 (3)
        /// </summary>
        GlobalFinancialCrisis,

        /// <summary>
        /// The flash crash of 2010 - https://en.wikipedia.org/wiki/2010_Flash_Crash (4)
        /// </summary>
        FlashCrash,

        /// <summary>
        /// Fukushima nuclear power plant meltdown - https://en.wikipedia.org/wiki/Fukushima_Daiichi_nuclear_disaster (5)
        /// </summary>
        FukushimaMeltdown,

        /// <summary>
        /// United States credit rating downgrade - https://en.wikipedia.org/wiki/United_States_federal_government_credit-rating_downgrades
        /// European debt crisis - https://en.wikipedia.org/wiki/European_debt_crisis (6)
        /// </summary>
        USDowngradeEuropeanDebt,

        /// <summary>
        /// European debt crisis - https://en.wikipedia.org/wiki/European_debt_crisis (7)
        /// </summary>
        EurozoneSeptember2012,

        /// <summary>
        /// European debt crisis - https://en.wikipedia.org/wiki/European_debt_crisis (8)
        /// </summary>
        EurozoneOctober2014,

        /// <summary>
        /// 2015-2016 market sell off https://en.wikipedia.org/wiki/2015%E2%80%9316_stock_market_selloff (9)
        /// </summary>
        MarketSellOff2015,

        /// <summary>
        /// Crisis recovery (2010 - 2012) (10)
        /// </summary>
        Recovery,

        /// <summary>
        /// 2014 - 2019 market performance (11)
        /// </summary>
        NewNormal,

        /// <summary>
        /// COVID-19 pandemic market crash (12)
        /// </summary>
        COVID19,

        /// <summary>
        /// Post COVID-19 recovery (13)
        /// </summary>
        PostCOVIDRunUp,

        /// <summary>
        /// Meme-craze era like GME, AMC, and DOGE (14)
        /// </summary>
        MemeSeason,

        /// <summary>
        /// Russia invased Ukraine (15)
        /// </summary>
        RussiaInvadesUkraine,

        /// <summary>
        /// Artificial intelligence boom (16)
        /// </summary>
        AIBoom
    }
}

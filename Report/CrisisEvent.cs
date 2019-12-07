using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Report
{
    /// <summary>
    /// Crisis Events
    /// </summary>
    public enum CrisisEvent
    {
        /// <summary>
        /// https://en.wikipedia.org/wiki/Financial_crisis_of_2007%E2%80%9308
        /// </summary>
        GlobalFinancialCrisis,

        /// <summary>
        /// The flash crash of 2010 - https://en.wikipedia.org/wiki/2010_Flash_Crash
        /// </summary>
        FlashCrash,

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
        NewNormal
    }
}

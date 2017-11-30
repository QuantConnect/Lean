using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Data.Custom.Intrinio
{
    public class BofAMerrillLynch
    {
        /// <summary>
        /// The BofA Merrill Lynch Option-Adjusted Spreads (OASs) are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond’s OAS, weighted by market capitalization.
        /// Source: https://fred.stlouisfed.org/series/BAMLH0A0HYM2
        /// </summary>
        public string USHighYieldOptionAdjustedSpread => "$BAMLH0A0HYM2";

        /// <summary>
        /// This data represents the effective yield of the BofA Merrill Lynch US High Yield Master II Index, which tracks the performance of US dollar denominated below investment grade rated corporate debt publically issued in the US domestic market.
        /// Source: https://fred.stlouisfed.org/series/BAMLH0A0HYM2EY
        /// </summary>
        public string USHighYieldEffectiveYield => "$BAMLH0A0HYM2EY";

        /// <summary>
        /// This data represents the effective yield of the BofA Merrill Lynch US High Yield Master II Index, which tracks the performance of US dollar denominated below investment grade rated corporate debt publically issued in the US domestic market.
        /// </summary>
        /// Source: https://fred.stlouisfed.org/series/BAMLH0A3HYCEY
        public string USHighYieldCCCorBelowEffectiveYield => "$BAMLH0A3HYCEY";

        /// <summary>
        /// The BofA Merrill Lynch Option-Adjusted Spreads (OASs) are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond’s OAS, weighted by market capitalization. 
        /// </summary>
        /// Source: https://fred.stlouisfed.org/series/BAMLC0A0CM
        public string USCorporateMasterOptionAdjustedSpread => "$BAMLC0A0CM";

        /// <summary>
        /// This data represents the effective yield of the BofA Merrill Lynch US Corporate Master Index, which tracks the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. 
        /// </summary>
        /// Source: https://fred.stlouisfed.org/series/BAMLC0A0CMEY
        public string USCorporateMasterEffectiveYield => "$BAMLC0A0CMEY";
    }

    public class Libor
    {

    }


    public class IntrinioEconomicDataSources
    {

    }
}

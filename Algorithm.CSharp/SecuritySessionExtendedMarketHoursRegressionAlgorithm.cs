using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm.CSharp
{
    public class SecuritySessionExtendedMarketHoursRegressionAlgorithm : SecuritySessionRegressionAlgorithm
    {
        protected override bool ExtendedMarketHours => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 118;
    }
}

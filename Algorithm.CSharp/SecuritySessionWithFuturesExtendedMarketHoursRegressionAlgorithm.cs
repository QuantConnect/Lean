using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    public class SecuritySessionWithFuturesExtendedMarketHoursRegressionAlgorithm : SecuritySessionWithFuturesRegressionAlgorithm
    {
        protected override bool ExtendedMarketHours => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 388962;
    }
}

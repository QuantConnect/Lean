using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Report
{
    /// <summary>
    /// Represents a period of time where the drawdown ranks amongst the top N drawdowns.
    /// </summary>
    public class DrawdownPeriod
    {
        /// <summary>
        /// Start of the drawdown period
        /// </summary>
        public DateTime Start { get; private set; }

        /// <summary>
        /// End of the drawdown period
        /// </summary>
        public DateTime End { get; private set; }

        /// <summary>
        /// Loss in percent from peak to trough
        /// </summary>
        public double PeakToTrough { get; private set; }

        /// <summary>
        /// Loss in percent from peak to trough - Alias for <see cref="PeakToTrough"/>
        /// </summary>
        public double Drawdown => PeakToTrough;

        /// <summary>
        /// Creates an instance with the given start, end, and drawdown
        /// </summary>
        /// <param name="start">Start of the drawdown period</param>
        /// <param name="end">End of the drawdown period</param>
        /// <param name="drawdown">Max drawdown of the period</param>
        public DrawdownPeriod(DateTime start, DateTime end, double drawdown)
        {
            Start = start;
            End = end;
            PeakToTrough = drawdown;
        }
    }
}

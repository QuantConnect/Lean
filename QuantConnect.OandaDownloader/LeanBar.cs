using System;

namespace QuantConnect.OandaDownloader
{
    /// <summary>
    /// Represents a candlestick bar
    /// </summary>
    public class LeanBar
    {
        /// <summary>
        /// The starting time of the bar
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// The open value of the bar
        /// </summary>
        public double Open { get; set; }

        /// <summary>
        /// The high value of the bar
        /// </summary>
        public double High { get; set; }

        /// <summary>
        /// The low value of the bar
        /// </summary>
        public double Low { get; set; }

        /// <summary>
        /// The close value of the bar
        /// </summary>
        public double Close { get; set; }

        /// <summary>
        /// The number of ticks in the bar
        /// </summary>
        public int TickVolume { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect
{
    /// <summary>
    /// Model class for passing in parameters for historical data
    /// </summary>
    public class DataDownloaderGetParameters
    {
        /// <summary>
        /// Symbol for the data we're looking for.
        /// </summary>
        public Symbol Symbol { get; set; }

        /// <summary>
        /// Resolution of the data request
        /// </summary>
        public Resolution Resolution { get; set; }

        /// <summary>
        /// Start time of the data in UTC
        /// </summary>
        public DateTime StartUtc { get; set; }

        /// <summary>
        /// End time of the data in UTC
        /// </summary>
        public DateTime EndUtc { get; set; }

        /// <summary>
        /// The type of tick to get
        /// </summary>
        public TickType TickType { get; set; }

        /// <summary>
        /// Initialize model class for passing in parameters for historical data
        /// </summary>
        /// <param name="symbol">Symbol for the data we're looking for.</param>
        /// <param name="resolution">Resolution of the data request</param>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <param name="tickType">[Optional] The type of tick to get. Defaults to <see cref="TickType.Trade"/></param>
        public DataDownloaderGetParameters(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc, TickType? tickType = null)
        {
            Symbol = symbol;
            Resolution = resolution;
            StartUtc = startUtc;
            EndUtc = endUtc;
            TickType = tickType ?? TickType.Trade;
        }

    }
}

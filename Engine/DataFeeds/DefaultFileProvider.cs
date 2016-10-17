using System;
using QuantConnect.Interfaces;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Default file provider functionality that does not attempt to retrieve any data
    /// </summary>
    public class DefaultFileProvider : IFileProvider
    {
        /// <summary>
        /// Does not attempt to retrieve any data
        /// </summary>
        /// <param name="symbol">Symbol of the security</param>
        /// <param name="resolution">Resolution of the data requested</param>
        /// <param name="date">DateTime of the data requested</param>
        /// <returns>False</returns>
        public bool Fetch(Symbol symbol, Resolution resolution, DateTime date)
        {
            return false;
        }
    }
}

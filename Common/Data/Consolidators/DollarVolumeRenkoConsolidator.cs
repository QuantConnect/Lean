using System;
using QuantConnect.Data.Market;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data;
using QuantConnect;

namespace Common.Data.Consolidators
{
    /// <summary>
    /// This consolidator transforms a stream of <see cref="BaseData"/> instances into a stream of <see cref="RenkoBar"/>
    /// with a constant dollar volume for each bar.
    /// </summary>
    public class DollarVolumeRenkoConsolidator : VolumeRenkoConsolidator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DollarVolumeRenkoConsolidator"/> class using the specified <paramref name="barSize"/>.
        /// </summary>
        /// <param name="barSize">The constant dollar volume size of each bar</param>
        public DollarVolumeRenkoConsolidator(decimal barSize)
            : base(barSize)
        {
        }

        /// <summary>
        /// Converts raw volume into dollar volume by multiplying it with the trade price.
        /// </summary>
        /// <param name="volume">The raw trade volume</param>
        /// <param name="price">The trade price</param>
        /// <returns>The dollar volume</returns>
        protected override decimal AdjustVolume(decimal volume, decimal price)
        {
            return volume * price;
        }
    }
}
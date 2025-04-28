using System;
using QuantConnect.Data.Market;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data;
using QuantConnect;

namespace Common.Data.Consolidators
{
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

        protected override decimal AdjustVolume(decimal volume, decimal price)
        {
            return volume * price;
        }
    }
}
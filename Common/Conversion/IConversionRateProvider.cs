using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Securities;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Conversion
{
    public interface IConversionRateProvider
    {
        /// <summary>
        /// A source currency we calculate conversion rate for
        /// </summary>
        Cash SourceCurrency { get; }

        /// <summary>
        /// A target currency, usually <see cref="CashBook.AccountCurrency"/>, from which conversion rate is based from
        /// </summary>
        Cash TargetCurrency { get; }

        /// <summary>
        /// Gets current conversion rate. Rate can change over time.
        /// </summary>
        /// <returns>Conversion rate</returns>
        decimal GetRate();

        /// <summary>
        /// A Factory from which this object was made
        /// </summary>
        IConversionRateProviderFactory ConversionRateProviderFactory { get; }

        /// <summary>
        /// Ensure if all needed securities are contained for conversion
        /// </summary>
        /// <returns>Returns true if all needed securities are added, else false.</returns>
        bool EnsureCompleteConversionPath();

    }
}
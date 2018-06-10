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
        string SourceCurrencyCode { get; }

        /// <summary>
        /// A target currency, usually <see cref="CashBook.AccountCurrency"/>, from which conversion rate is based from
        /// </summary>
        string TargetCurrencyCode { get; }

        /// <summary>
        /// Gets current conversion rate. Rate can change over time.
        /// </summary>
        /// <returns>Conversion rate</returns>
        decimal ConversionRate { get; }

        /// <summary>
        /// Ensure if all needed securities are contained for conversion
        /// </summary>
        /// <returns>Returns true if all needed securities are added, else false.</returns>
        bool EnsureCompleteConversionPath();
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Securities;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Conversion
{
    /// <summary>
    /// This class holds single variable as a rate conversion 
    /// </summary>
    public class VariableConversionRateProvider : IConversionRateProvider
    {
        public string SourceCurrencyCode { get; private set; }

        public string TargetCurrencyCode { get; private set; }

        public decimal ConversionRate { get; set; }

        public bool EnsureCompleteConversionPath() => true;

        public VariableConversionRateProvider(string sourceCurrencyCode, string targetCurrencyCode, decimal conversionRate)
        {
            SourceCurrencyCode = sourceCurrencyCode;
            TargetCurrencyCode = targetCurrencyCode;
            ConversionRate = conversionRate;
        }
    }
}
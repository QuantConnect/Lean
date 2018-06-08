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
    /// This class holds single security for conversion
    /// </summary>
    public class SingleSecurityConversionRateProvider : IConversionRateProvider
    {
        public string SourceCurrency { get; private set; }

        public string TargetCurrency { get; private set; }

        public decimal ConversionRate 
        {
            get
            {
                return ConversionSecurity.Price;
            }
        }

        public Security ConversionSecurity;

        public bool EnsureCompleteConversionPath() => true;

        public SingleSecurityConversionRateProvider(string source, string target, Security conversionSecurity)
        {

            SourceCurrency = source;
            TargetCurrency = target;
            ConversionSecurity = conversionSecurity;
        }
    }
}
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
    /// Fixed conversion rate
    /// </summary>
    public class FixedConversionRateProvider : IConversionRateProvider
    {
        public string SourceCurrency { get; private set; }

        public string TargetCurrency { get; private set; }

        public decimal ConversionRate { get; private set; }

        public bool EnsureCompleteConversionPath() => true; 

        public FixedConversionRateProvider(string source, string target, decimal fixedRate) 
        {
            SourceCurrency = source;
            TargetCurrency = target;
            this.ConversionRate = fixedRate;
        }
    }
}
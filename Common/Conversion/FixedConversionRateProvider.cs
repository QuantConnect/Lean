using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Securities;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Conversion
{
    public class FixedConversionRateProvider : IConversionRateProvider
    {
        public Cash SourceCurrency { get; private set; }

        public Cash TargetCurrency { get; private set; }

        private decimal fixedRate;

        public decimal GetRate()
        {
            return fixedRate;
        }

        public IConversionRateProviderFactory ConversionRateProviderFactory { get; private set; }

        public bool EnsureCompleteConversionPath()
        {
            return true;
        }

        public FixedConversionRateProvider(Cash source, Cash target, decimal fixedRate, IConversionRateProviderFactory factory) 
        {
            SourceCurrency = source;
            TargetCurrency = target;
            this.fixedRate = fixedRate;
            ConversionRateProviderFactory = factory;
        }
    }
}
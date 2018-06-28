using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Data;
using QuantConnect;
using QuantConnect.Util;
namespace QuantConnect.Securities.CurrencyConversion
{
    public class SecurityCurrencyConversion : ICurrencyConversion
    {
        /// <summary>
        /// Class that holds Security for conversion and bool, which tells if rate should be inverted (1/rate)
        /// </summary>
        public class Step
        {
            public readonly Security RateSecurity;
            public readonly bool Inverted;
            public decimal ConversionRate;

            public string PairSymbol => RateSecurity.Symbol.Value;

            public Step(Security rateSecurity, bool inverted = false)
            {
                this.RateSecurity = rateSecurity;
                this.Inverted = inverted;

                if (RateSecurity.Price != 0)
                {
                    if (inverted == false)
                    {
                        ConversionRate = RateSecurity.Price;
                    }

                    else
                    {
                        ConversionRate = 1m / RateSecurity.Price;
                    }
                }
            }

            public void Update(BaseData data)
            {
                if (RateSecurity.Symbol == data.Symbol)
                {
                    var rate = data.Value;

                    if (Inverted)
                    {
                        rate = 1 / rate;
                    }

                    ConversionRate = rate;
                }
            }
        }

        private decimal _conversionRate = 1m;

        private List<Security> _securities;

        private List<Step> _steps;

        // constructor is private, can't use new on this class
        private SecurityCurrencyConversion()
        {

        }

        /// <summary> List of available securities to use for conversion </summary>
        public IReadOnlyList<Security> Securities { get { return _securities; } }

        public IReadOnlyList<Step> ConversionSteps { get { return _steps;  } }

        public string SourceCurrency { get; private set; }

        public string DestinationCurrency { get; private set; }

        public decimal GetConversion() { return _conversionRate; }

        public decimal Update()
        {
            decimal newConversionRate = 1;

            _steps.ForEach(step => {

                if(step.Inverted)
                {
                    newConversionRate /= step.RateSecurity.GetLastData().Value;
                }
                else
                {
                    newConversionRate *= step.RateSecurity.GetLastData().Value;
                }
            });

            _conversionRate = newConversionRate;

            return _conversionRate;
        }

        // linear search for path
        public static SecurityCurrencyConversion LinearSearch(string sourceCurrency, string destinationCurrency, IEnumerable<Security> availableSecurities)
        {
            var conversion = new SecurityCurrencyConversion();

            conversion._securities = availableSecurities.ToList();

            // search existing _securities list, and if anything found, calculate if inverted and then return step
            // 1 leg
            foreach (var sec in availableSecurities)
            {
                if (sec.Symbol.Value.PairContainsCode(sourceCurrency))
                {
                    if (sec.Symbol.Value.PairsOtherCode(sourceCurrency) == destinationCurrency)
                    {
                        bool inverted = sec.Symbol.Value.ComparePair(sourceCurrency, destinationCurrency) == CurrencyPairUtil.Match.InverseMatch;

                        conversion._steps = new List<Step>() { new Step(sec, inverted) };

                        return conversion;
                    }
                }
            }

            // 2 legs
            foreach (var sec1 in availableSecurities)
            {
                if (sec1.Symbol.Value.PairContainsCode(sourceCurrency))
                {
                    var midCode = sec1.Symbol.Value.PairsOtherCode(sourceCurrency);

                    foreach (var sec2 in availableSecurities)
                    {
                        if (sec2.Symbol.Value.PairContainsCode(midCode))
                        {
                            if (sec2.Symbol.Value.PairsOtherCode(midCode) == destinationCurrency)
                            {
                                string baseCode;
                                string quoteCode;

                                CurrencyPairUtil.DecomposeCurrencyPair(sec1.Symbol.Value, out baseCode, out quoteCode);
                                Step step1 = new Step(sec1, sourceCurrency == quoteCode);

                                CurrencyPairUtil.DecomposeCurrencyPair(sec2.Symbol.Value, out baseCode, out quoteCode);
                                Step step2 = new Step(sec2, midCode == quoteCode);

                                conversion._steps = new List<Step>() { step1, step2 };

                                return conversion;
                            }
                        }
                    }
                }
            }

            throw new ArgumentException($"No conversion path found in availableSecurities list for sourceCurrency {sourceCurrency} and destinationCurrency {destinationCurrency}");
        }
    }
}

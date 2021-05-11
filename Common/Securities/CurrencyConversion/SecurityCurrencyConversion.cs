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
            public readonly Symbol Symbol;
            public Security RateSecurity { get; private set; }
            public readonly bool Inverted;
            public decimal ConversionRate;

            public string PairSymbol => RateSecurity.Symbol.Value;

            public Step(Symbol symbol, Security rateSecurity, bool inverted = false)
            {
                this.Symbol = symbol;
                this.RateSecurity = rateSecurity;
                this.Inverted = inverted;

                if (RateSecurity != null)
                {
                    Update(RateSecurity.GetLastData());
                }
            }

            public void Update(BaseData data)
            {
                if (data != null && Symbol == data.Symbol)
                {
                    var rate = data.Value;

                    if (Inverted)
                    {
                        rate = 1 / rate;
                    }

                    ConversionRate = rate;
                }
            }

            public void SetSecurity(Security security)
            {
                if (security.Symbol.ID == Symbol.ID)
                {
                    RateSecurity = security;
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
            decimal newConversionRate = 0;
            var stepWithDataFound = false;

            _steps.ForEach(step =>
            {
                var lastData = step.RateSecurity.GetLastData();
                if (lastData == null)
                {
                    return;
                }

                decimal price = lastData.Price;

                if (price != 0m)
                {
                    if (!stepWithDataFound)
                    {
                        newConversionRate = 1;
                        stepWithDataFound = true;
                    }

                    if (step.Inverted)
                    {
                        newConversionRate /= price;
                    }
                    else
                    {
                        newConversionRate *= price;
                    }
                }
            });

            _conversionRate = newConversionRate;

            return _conversionRate;
        }

        public IEnumerable<Security> GetConversionRateSecurities()
        {
            return _steps.Select(step => step.RateSecurity);
        }

        // linear search for path
        public static SecurityCurrencyConversion LinearSearch(string sourceCurrency, string destinationCurrency, IEnumerable<Security> existingSecurities, IEnumerable<Symbol> potentialSymbols, Func<Symbol, Security> makeNewSecurity)
        {
            var conversion = new SecurityCurrencyConversion();

            conversion._securities = existingSecurities.ToList();

            var symToSecMap = new Dictionary<Symbol, Security>();

            var allSymbols = existingSecurities.Select(sec => sec.Symbol).Concat(potentialSymbols).Where(x => x.SecurityType == SecurityType.Crypto || x.Value.Length == 6);

            // search existing _securities list, and if anything found, calculate if inverted and then return step
            // 1 leg
            foreach (var sym in allSymbols)
            {
                if (sym.PairContainsCode(sourceCurrency))
                {
                    if (sym.CurrencyPairDual(sourceCurrency) == destinationCurrency)
                    {
                        bool inverted = sym.ComparePair(sourceCurrency, destinationCurrency) == CurrencyPairUtil.Match.InverseMatch;


                        var selection = existingSecurities.FirstOrDefault(s => s.Symbol == sym);

                        if (selection != null)
                        {
                            conversion._steps = new List<Step>() { new Step(sym, selection, inverted) };
                        }
                        else
                        {
                            conversion._steps = new List<Step>() { new Step(sym, makeNewSecurity(sym), inverted) };
                        }

                        return conversion;
                    }
                }
            }

            // 2 legs
            foreach (var sym1 in allSymbols)
            {
                if (sym1.PairContainsCode(sourceCurrency))
                {
                    var midCode = sym1.CurrencyPairDual(sourceCurrency);

                    foreach (var sym2 in allSymbols)
                    {
                        if (sym2.PairContainsCode(midCode))
                        {
                            if (sym2.CurrencyPairDual(midCode) == destinationCurrency)
                            {
                                string baseCode;
                                string quoteCode;

                                CurrencyPairUtil.DecomposeCurrencyPair(sym1, out baseCode, out quoteCode);

                                var selection = existingSecurities.Where(s => s.Symbol == sym1);

                                // Step 1

                                Step step1;

                                if (selection.Any())
                                {
                                    step1 = new Step(sym1, selection.Single(), sourceCurrency == quoteCode);
                                }
                                else
                                {
                                    step1 = new Step(sym1, makeNewSecurity(sym1), sourceCurrency == quoteCode);
                                }

                                CurrencyPairUtil.DecomposeCurrencyPair(sym2, out baseCode, out quoteCode);

                                selection = existingSecurities.Where(s => s.Symbol == sym2);

                                // Step 2

                                Step step2;

                                if(selection.Any())
                                {
                                    step2 = new Step(sym2, selection.Single(), midCode == quoteCode);
                                }
                                else
                                {
                                    step2 = new Step(sym2, makeNewSecurity(sym2), midCode == quoteCode);
                                }

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

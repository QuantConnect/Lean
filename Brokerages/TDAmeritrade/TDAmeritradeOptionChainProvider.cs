using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TDAmeritradeApi.Client.Models.MarketData;

namespace QuantConnect.Brokerages.TDAmeritrade
{
    public partial class TDAmeritradeBrokerage : IOptionChainProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public IEnumerable<Symbol> GetOptionContractList(Symbol symbol, DateTime date)
        {
            var optionsChain = tdClient.MarketDataApi.GetOptionChainAsync(symbol.Value, expirationsfromDate: date, expirationsToDate: date).Result;

            List<Symbol> options = new List<Symbol>();
            
            options.AddRange(CreateSymbols(symbol, date, optionsChain.callExpDateMap, OptionRight.Call));
            options.AddRange(CreateSymbols(symbol, date, optionsChain.putExpDateMap, OptionRight.Put));

            return options;
        }

        private static List<Symbol> CreateSymbols(Symbol symbol, DateTime date, Dictionary<string, Dictionary<decimal, List<ExpirationDateMap>>> optionChain, OptionRight optionRight)
        {
            List<Symbol> options = new List<Symbol>();
            foreach (var option in optionChain)
            {
                var dateAndDaysToExpiration = option.Key;
                var strikes = option.Value.Keys.ToList();

                foreach (var strike in strikes)
                {
                    options.Add(Symbol.CreateOption(symbol, Market.USA.ToString(), OptionStyle.American, optionRight, strike, date));
                }
            }
            return options;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Brokerages;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    public class AllShortableSymbolsCoarseSelectionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2014, 3, 24);
            SetEndDate(2014, 4, 7);
            SetCash(100000);

            AddUniverse(CoarseSelection, FineSelection);
            SetBrokerageModel();
        }

        private IEnumerable<Symbol> CoarseSelection(IEnumerable<CoarseFundamental> coarse)
        {
            var shortableSymbols = AllShortableSymbols();

            coarse.Select(x => x.Symbol).Where(x => )
            return new Symbol[0];
        }

        private IEnumerable<Symbol> FineSelection(IEnumerable<FineFundamental> fine)
        {
            Log("Another one!");
            return new Symbol[0];
        }

        private class AllShortableSymbolsRegressionAlgorithmBrokerageModel : DefaultBrokerageModel
        {
            public AllShortableSymbolsCoarseSelectionRegressionAlgorithm() : base()
            {
                ShortableProvider = new AllShortableSymbolsRegressionShortableProvider();
            }
        }

        private class AllShortableSymbolsRegressionShortableProvider : IShortableProvider
        {
            public readonly Symbol _spy = Symbol("SPY");
            public readonly Symbol _wtw = Symbol("WTW");
            public readonly Symbol _bac = Symbol("BAC");
            public readonly Symbol _aapl = Symbol("AAPL");
            public readonly Symbol _goog = Symbol("GOOG");
            public readonly Symbol _gme = Symbol("GME");
            public readonly Symbol _jpm = Symbol("JPM");
            public readonly Symbol _qqq = Symbol("QQQ");

            public Dictionary<Symbol, long> AllShortableSymbols(DateTime localTime)
            {
                if (localTime.Date == new DateTime(2014, 3, 24))
                {
                    return new Dictionary<Symbol, long>
                    {

                    }
                }
            }

            public long? ShortableQuantity(Symbol symbol, DateTime localTime)
            {
                throw new NotImplementedException();
            }

            private static Symbol Symbol(string ticker) => QuantConnect.Symbol.Create(ticker, SecurityType.Equity, Market.USA);
        }

        public bool CanRunLocally { get; }
        public Language[] Languages { get; }
        public Dictionary<string, string> ExpectedStatistics { get; }
    }
}

using System;
using QuantConnect.Data;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    public class BasicTemplateAlgorithmSB : QCAlgorithm
    {
        private Symbol symbol = QuantConnect.Symbol.Create("ETHEUR", SecurityType.Crypto, Market.GDAX);
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            AddCrypto("ETHEUR", Resolution.Hour);

            Debug("Account currency: " + Portfolio.CashBook.AccountCurrency);

            SetAccountCurrency("EUR");

            Debug("Account currency: " + Portfolio.CashBook.AccountCurrency);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings(symbol, 1);
                Debug("Bought " + symbol.Value + " | Account currency: " + Portfolio.CashBook.AccountCurrency);
            }
        }
    }
}
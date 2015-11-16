using NUnit.Framework;
using QuantConnect;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Securities.Forex;

[SetUpFixture]
public class AssemblyInitialize
{
    [SetUp]
    public void SetLogHandler()
    {
        // save output to file as well
        Log.LogHandler = new ConsoleLogHandler();
    }

    [SetUp]
    public void InitializeSymbolCache()
    {
        AddEquity("SPY");
        AddEquity("AAPL");

        // add all the forex pairs for lifting as FXCM
        foreach (var pair in Forex.CurrencyPairs)
        {
            AddForex(pair);
        }
    }

    private static void AddForex(string symbol)
    {
        SymbolCache.Set(symbol, new Symbol(SecurityIdentifier.GenerateForex(symbol, Market.FXCM), symbol));
    }

    private static void AddEquity(string symbol)
    {
        SymbolCache.Set(symbol, new Symbol(SecurityIdentifier.GenerateEquity(symbol, Market.USA), symbol));
    }
}

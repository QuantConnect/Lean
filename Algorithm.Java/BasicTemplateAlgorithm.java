package QuantConnect;
import cli.QuantConnect.Resolution;
import cli.QuantConnect.SecurityType;
import cli.QuantConnect.Algorithm.QCAlgorithm;
import cli.QuantConnect.Data.Market.TradeBars;

public class BasicTemplateAlgorithm extends QCAlgorithm
{
    public void Initialize()
    {
        SetStartDate(2013, 10, 07);  //Set Start Date
        SetEndDate(2013, 10, 11);    //Set End Date
        SetCash(100000);             //Set Strategy Cash
        // Find more symbols here: http://quantconnect.com/data
        AddSecurity(SecurityType.wrap(SecurityType.Equity), "SPY", Resolution.wrap(Resolution.Second),true,false);
    }
    
     public void OnData(TradeBars data)
     {
         if (!get_Portfolio().get_Invested())
         {
             SetHoldings(Symbol("SPY"), 1, false);
             Debug("Hello From Java");
         }
     } 
}

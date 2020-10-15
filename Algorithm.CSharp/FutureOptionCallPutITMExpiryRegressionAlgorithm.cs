using System;
using System.Linq;
using System.Reflection;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    public class FutureOptionCallPutITMExpiryRegressionAlgorithm : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2020, 9, 22);
            typeof(QCAlgorithm)
                .GetField("_endDate", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(this, new DateTime(2021, 3, 22));

            var es18z20 = AddFutureContract(
                QuantConnect.Symbol.CreateFuture(
                    Futures.Indices.SP500EMini,
                    Market.CME,
                    new DateTime(2020, 12, 18)),
                Resolution.Minute).Symbol;

            var es19h21 = AddFutureContract(
                QuantConnect.Symbol.CreateFuture(
                    Futures.Indices.SP500EMini,
                    Market.CME,
                    new DateTime(2021, 3, 19)),
                Resolution.Minute).Symbol;

            var esChains = OptionChainProvider.GetOptionContractList(es18z20, Time)
                .Concat(OptionChainProvider.GetOptionContractList(es19h21, Time))
                .Where(x => x.ID.StrikePrice == 2550m)
                .Select(x => AddFutureOptionContract(x, Resolution.Minute))
                .ToList();

            //var esChains = OptionChainProvider.GetOptionContractList(es18z20, Time)
            //    .Concat(OptionChainProvider.GetOptionContractList(es19h21, Time))
            //    .Select(x => AddFutureOptionContract(x, Resolution.Minute))
            //    .OrderByDescending(x => x.StrikePrice)
            //    .Where(x => x.StrikePrice < x.Underlying.Price)
            //    .Take(2);

            Schedule.On(DateRules.Today, TimeRules.AfterMarketOpen(es19h21, 1), () =>
            {
                //var sign = -1;
                foreach (var contract in esChains)
                {
                    //sign *= -1;
                    MarketOrder(contract.Symbol, 1);
                }
            });

            Schedule.On(DateRules.Today, TimeRules.BeforeMarketClose(es19h21, 60), () =>
            {
                foreach (var contract in esChains)
                {
                    //Liquidate(contract);
                }
            });
        }

        public override void OnData(Slice data)
        {
        }
    }
}

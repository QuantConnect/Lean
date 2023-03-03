using QuantConnect.Algorithm.CSharp.Benchmarks;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Portfolio.SignalExports;
using QuantConnect.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm.CSharp
{
    public class SignalExportDemonstration : QCAlgorithm
    {
        Dictionary<string, decimal> targetPortfolio = new Dictionary<string, decimal>() {
            { "SPY", (decimal)(0.2) }, { "ES", (decimal)(0.8)}};

        List<PortfolioTarget> targetList = new List<PortfolioTarget>();

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Minute;

            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 08);    //Set End Date
            SetCash(50000);             //Set Strategy Cash

            var spy = AddEquity("SPY");
            targetList.Add(new PortfolioTarget(spy.Symbol, (decimal)(0.2)));

            var es = AddFuture("ES");
            targetList.Add(new PortfolioTarget(es.Symbol, (decimal)(0.8)));

            SignalExportManager.SetSignalExportProviders(new Collective2SignalExport("fnmzppYk0HO8YTrMRCPA2MBa3mLna6frsMjAJab1SyA5lpfbhY", 143679411, Portfolio));
            SignalExportManager.SetTargetPortfolio(targetList);
        }


        public override void OnWarmupFinished()
        {
            Debug("EVENT: Warmup Finished **************************************************");
        }


        public override void OnData(Slice slice)
        {
            Debug("EVENT: OnData **************************************************");
            SignalExportManager.ExportSignals();
        }



        public override void OnEndOfAlgorithm()
        {
            Debug("EVENT: End of Algorithm  **************************************************");

        }
    }
}

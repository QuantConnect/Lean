using System;
using System.Collections.Generic;
using System.IO;
using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.Results
{
    public class BaseResultsHandler
    {
        public virtual void SaveLogs(string logs)
        {
            throw new NotImplementedException();
        }

        public virtual void SaveCharts(string chartName, Dictionary<string, Chart> charts)
        {
            File.WriteAllText(chartName, "");
        }

        public virtual void SaveResults(string name, Result result)
        {
            
        }
    }
}

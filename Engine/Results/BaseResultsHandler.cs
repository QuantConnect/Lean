using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.Results
{
    public class BaseResultsHandler
    {
        public virtual string SaveLogs(IEnumerable<string> logs)
        {
            var joined = string.Join("\r\n", logs);
            return Path.Combine(Directory.GetCurrentDirectory(), "log.txt");
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

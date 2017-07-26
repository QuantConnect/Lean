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
        public virtual void SaveLogs(IEnumerable<LogEntry> logs)
        {
            var joined = string.Join("\r\n", logs.Select(x => x.Message));
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

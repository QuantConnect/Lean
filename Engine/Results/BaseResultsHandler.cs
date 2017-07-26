using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace QuantConnect.Lean.Engine.Results
{
    /// <summary>
    /// Provides base functionality to the implementations of <see cref="IResultHandler"/>
    /// </summary>
    public class BaseResultsHandler
    {
        /// <summary>
        /// Returns the location of the logs
        /// </summary>
        /// <param name="logs">The logs to save</param>
        /// <returns>The path to the logs</returns>
        public virtual string SaveLogs(IEnumerable<string> logs)
        {
            // When running lean locally, the logs are written to disk by the LogHandler
            // Just return the location of this log file
            return Path.Combine(Directory.GetCurrentDirectory(), "log.txt");
        }

        /// <summary>
        /// Save the charts to disk
        /// </summary>
        /// <param name="chartName">The name of the chart</param>
        /// <param name="charts">The charts to save</param>
        public virtual void SaveCharts(string chartName, Dictionary<string, Chart> charts)
        {
            File.WriteAllText(chartName, JsonConvert.SerializeObject(charts));
        }

        /// <summary>
        /// Save the results to disk
        /// </summary>
        /// <param name="name">The name of the results</param>
        /// <param name="result">The results to save</param>
        public virtual void SaveResults(string name, Result result)
        {
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), name), JsonConvert.SerializeObject(result));
        }
    }
}

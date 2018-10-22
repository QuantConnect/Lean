using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Lean.Engine.Results
{
    /// <summary>
    /// Provides base functionality to the implementations of <see cref="IResultHandler"/>
    /// </summary>
    public class BaseResultsHandler
    {
        protected IDataFeedSubscriptionManager DataManager;

        /// <summary>
        /// Gets or sets the current alpha runtime statistics
        /// </summary>
        protected AlphaRuntimeStatistics AlphaRuntimeStatistics { get; set; }

        /// <summary>
        /// Returns the location of the logs
        /// </summary>
        /// <param name="id">Id that will be incorporated into the algorithm log name</param>
        /// <param name="logs">The logs to save</param>
        /// <returns>The path to the logs</returns>
        public virtual string SaveLogs(string id, IEnumerable<string> logs)
        {
            var path = $"{id}-log.txt";
            File.WriteAllLines(path, logs);
            return Path.Combine(Directory.GetCurrentDirectory(), path);
        }

        /// <summary>
        /// Save the results to disk
        /// </summary>
        /// <param name="name">The name of the results</param>
        /// <param name="result">The results to save</param>
        public virtual void SaveResults(string name, Result result)
        {
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), name), JsonConvert.SerializeObject(result, Formatting.Indented));
        }

        /// <summary>
        /// Sets the current alpha runtime statistics
        /// </summary>
        /// <param name="statistics">The current alpha runtime statistics</param>
        public virtual void SetAlphaRuntimeStatistics(AlphaRuntimeStatistics statistics)
        {
            AlphaRuntimeStatistics = statistics;
        }

        /// <summary>
        /// Sets the current Data Manager instance
        /// </summary>
        public virtual void SetDataManager(IDataFeedSubscriptionManager dataManager)
        {
            DataManager = dataManager;
        }
    }
}

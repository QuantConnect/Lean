using System;
using QuantConnect.Algorithm.Framework.Alphas.Analysis;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Abstraction point to handle the various concerns from a common api.
    /// At the time of writing, these concerns are charting, scoring, perisistence and messaging.
    /// </summary>
    public interface IAlphaManagerExtension
    {
        /// <summary>
        /// Invokes the manager at the end of the time step.
        /// </summary>
        /// <param name="frontierTimeUtc">The current frontier time utc</param>
        void Step(DateTime frontierTimeUtc);

        /// <summary>
        /// Allows the extension to initialize itself over the expected range
        /// </summary>
        /// <param name="algorithmStartDate">The start date of the algorithm</param>
        /// <param name="algorithmEndDate">The end date of the algorithm</param>
        /// <param name="algorithmUtcTime">The algorithm's current utc time</param>
        void InitializeForRange(DateTime algorithmStartDate, DateTime algorithmEndDate, DateTime algorithmUtcTime);

        /// <summary>
        /// Pipes new
        /// </summary>
        /// <param name="context">Context whose alpha has just generated</param>
        void OnAlphaGenerated(AlphaAnalysisContext context);

        /// <summary>
        /// Invoked when the alpha manager detects that an alpha has closed (frontier has passed alpha period)
        /// </summary>
        /// <param name="context">Context whose alpha has just closed</param>
        void OnAlphaClosed(AlphaAnalysisContext context);

        /// <summary>
        /// Invoked when the alpha manager has completed analysis on an alpha
        /// </summary>
        /// <param name="context">Context whose alpha has just completed analysis</param>
        void OnAlphaAnalysisCompleted(AlphaAnalysisContext context);
    }
}
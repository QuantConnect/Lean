using System;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// Provides a default implementation o <see cref="IBrokerageMessageHandler"/> that will forward
    /// messages as follows:
    /// Information -> IResultHandler.Debug
    /// Warning     -> IResultHandler.Error && IApi.SendUserEmail
    /// Error       -> IResultHandler.Error && IAlgorithm.RunTimeError
    /// </summary>
    public class DefaultBrokerageMessageHandler : IBrokerageMessageHandler
    {
        private readonly IApi _api;
        private readonly IAlgorithm _algorithm;
        private readonly IResultHandler _results;
        private readonly AlgorithmNodePacket _job;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultBrokerageMessageHandler"/> class
        /// </summary>
        /// <param name="algorithm">The running algorithm</param>
        /// <param name="job">The job that produced the algorithm</param>
        /// <param name="results">The result handler for the algorithm</param>
        /// <param name="api">The api for the algorithm</param>
        public DefaultBrokerageMessageHandler(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler results, IApi api)
        {
            _results = results;
            _api = api;
            _algorithm = algorithm;
            _job = job;
        }

        /// <summary>
        /// Handles the message
        /// </summary>
        /// <param name="message">The message to be handled</param>
        public void Handle(BrokerageMessageEvent message)
        {
            // based on message type dispatch to result handler
            switch (message.Type)
            {
                case BrokerageMessageType.Information:
                    _results.DebugMessage("Brokerage Info: " + message.Message);
                    break;
                case BrokerageMessageType.Warning:
                    _results.ErrorMessage("Brokerage Warning: " + message.Message);
                    _api.SendUserEmail(_job.AlgorithmId, "Brokerage Warning", message.Message);
                    break;
                case BrokerageMessageType.Error:
                    _results.ErrorMessage("Brokerage Error: " + message.Message);
                    _algorithm.RunTimeError = new Exception(message.Message);
                    break;
            }
        }
    }
}
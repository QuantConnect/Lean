using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QuantConnect.Packets
{
    /// <summary>
    /// Packet for history jobs
    /// </summary>
    public class HistoryPacket : Packet
    {
        /// <summary>
        /// The queue where the data should be sent
        /// </summary>
        public string QueueName;

        /// <summary>
        /// The individual requests to be processed
        /// </summary>
        public List<HistoryRequest> Requests = new List<HistoryRequest>();

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryPacket"/> class
        /// </summary>
        public HistoryPacket()
            : base(PacketType.History)
        {
        }
    }

    /// <summary>
    /// Specifies request parameters for a single historical request.
    /// A HistoryPacket is made of multiple requests for data. These
    /// are used to request data during live mode from a data server
    /// </summary>
    public class HistoryRequest
    {
        /// <summary>
        /// The start time to request data in UTC
        /// </summary>
        public DateTime StartTimeUtc;

        /// <summary>
        /// The end time to request data in UTC
        /// </summary>
        public DateTime EndTimeUtc;

        /// <summary>
        /// The symbol to request data for
        /// </summary>
        public Symbol Symbol;

        /// <summary>
        /// The symbol's security type
        /// </summary>
        public SecurityType SecurityType;

        /// <summary>
        /// The requested resolution
        /// </summary>
        public Resolution Resolution;

        /// <summary>
        /// The market the symbol belongs to
        /// </summary>
        public string Market;
    }

    /// <summary>
    /// Provides a container for results from history requests. This contains
    /// the file path relative to the /Data folder where the data can be written
    /// </summary>
    public class HistoryResult
    {
        /// <summary>
        /// The relative file path where the data should be written
        /// </summary>
        public string Filepath;

        /// <summary>
        /// The file's contents, this is a zipped csv file
        /// </summary>
        public byte[] File;

        /// <summary>
        /// Default constructor for serializers
        /// </summary>
        public HistoryResult()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryResult"/> class
        /// </summary>
        /// <param name="filepath">The relative file path where the file should be written</param>
        /// <param name="file">The zipped csv file content in bytes</param>
        public HistoryResult(string filepath, byte[] file)
        {
            Filepath = filepath;
            File = file;
        }

        /// <summary>
        /// Flag used to determine if this is a completed message
        /// </summary>
        [JsonIgnore]
        public bool IsCompletedMessage
        {
            get { return this is CompletedHistoryResult; }
        }

        /// <summary>
        /// Flag used to determine if this is an error message
        /// </summary>
        [JsonIgnore]
        public bool IsErrorMessage
        {
            get { return this is ErrorHistoryResult; }
        }
    }

    /// <summary>
    /// Specifies the completed message from a history result
    /// </summary>
    public class CompletedHistoryResult : HistoryResult
    {
        
    }
    /// <summary>
    /// Specfies an error message in a history result
    /// </summary>
    public class ErrorHistoryResult : HistoryResult
    {
        /// <summary>
        /// Gets the error that was encountered
        /// </summary>
        public string Message;

        public ErrorHistoryResult(string message)
        {
            Message = message;
        }
    }
}

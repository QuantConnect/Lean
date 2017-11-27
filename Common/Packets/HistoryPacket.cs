/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

using System;
using System.Collections.Generic;

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
        /// The requested resolution
        /// </summary>
        public Resolution Resolution;

        /// <summary>
        /// The type of data to retrieve
        /// </summary>
        public TickType TickType;
    }

    /// <summary>
    /// Specifies various types of history results
    /// </summary>
    public enum HistoryResultType
    {
        /// <summary>
        /// The requested file data
        /// </summary>
        File,

        /// <summary>
        /// The request's status
        /// </summary>
        Status,

        /// <summary>
        /// The request is completed
        /// </summary>
        Completed,

        /// <summary>
        /// The request had an error
        /// </summary>
        Error
    }

    /// <summary>
    /// Provides a container for results from history requests. This contains
    /// the file path relative to the /Data folder where the data can be written
    /// </summary>
    public abstract class HistoryResult
    {
        /// <summary>
        /// Gets the type of history result
        /// </summary>
        public HistoryResultType Type { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryResult"/> class
        /// </summary>
        /// <param name="type">The type of history result</param>
        protected HistoryResult(HistoryResultType type)
        {
            Type = type;
        }
    }

    /// <summary>
    /// Defines requested file data for a history request
    /// </summary>
    public class FileHistoryResult : HistoryResult
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
        public FileHistoryResult()
            : base(HistoryResultType.File)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryResult"/> class
        /// </summary>
        /// <param name="filepath">The relative file path where the file should be written, rooted in /Data, so for example ./forex/fxcm/daily/eurusd.zip</param>
        /// <param name="file">The zipped csv file content in bytes</param>
        public FileHistoryResult(string filepath, byte[] file)
            : this()
        {
            Filepath = filepath;
            File = file;
        }
    }

    /// <summary>
    /// Specifies the completed message from a history result
    /// </summary>
    public class CompletedHistoryResult : HistoryResult
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CompletedHistoryResult"/> class
        /// </summary>
        public CompletedHistoryResult()
            : base(HistoryResultType.Completed)
        {
        }
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

        /// <summary>
        /// Default constructor for serializers
        /// </summary>
        public ErrorHistoryResult()
            : base(HistoryResultType.Error)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorHistoryResult"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        public ErrorHistoryResult(string message)
            : this()
        {
            Message = message;
        }
    }

    /// <summary>
    /// Specifies the progress of a request
    /// </summary>
    public class StatusHistoryResult : HistoryResult
    {
        /// <summary>
        /// Gets the progress of the request
        /// </summary>
        public int Progress;

        /// <summary>
        /// Default constructor for serializers
        /// </summary>
        public StatusHistoryResult()
            : base(HistoryResultType.Status)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusHistoryResult"/> class
        /// </summary>
        /// <param name="progress">The progress, from 0 to 100</param>
        public StatusHistoryResult(int progress)
            : this()
        {
            Progress = progress;
        }
    }
}

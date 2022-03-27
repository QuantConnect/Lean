// This class is derived from the Apache log4net class log4net.Appender.QuantConnectAppender.
// Written by: David Thielen, contributed to QuantConnect LEAN project.

#region Apache License
//
// Licensed to the Apache Software Foundation (ASF) under one or more 
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership. 
// The ASF licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with 
// the License. You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Globalization;
using log4net.Appender;
using log4net.Core;
using QuantConnect.Algorithm;

// Cannot name it QuantConnect.log4net because then "log4net." referencing references this namespace
// when you intend to reference the log4net library namespace. It is ok to use dash or mash the text together.
namespace QuantConnect.ToolBox.QuantConnect_log4net
{
    /// <summary>
    /// Writes logging events to the QCAlgorithm Log() and Error() logging output. Also implements, if
    /// specified, a limitation to the number of lines logged (to avoid hitting the QC limit in a
    /// single backtest).
    /// </summary>
    /// <remarks>
    /// <para>
    /// QuantConnectAppender appends log events to the QCAlgorithm.Log()
    /// or QCAlgorithm.Error() output stream using a layout specified by the 
    /// user.
    /// </para>
    /// <para>
    /// By default, all output is written to the QCAlgorithm.Log() output stream.
    /// The <see cref="Target"/> property can be set to direct the output to the
    /// QCAlgorithm.Error() stream.
    /// </para>
    /// </remarks>
    /// <author>David Thielen</author>
    public class QuantConnectAppender : AppenderSkeleton
    {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantConnectAppender" /> class.
        /// </summary>
        /// <remarks>
        /// The instance of the <see cref="QuantConnectAppender" /> class is set up to write 
        /// to the standard output stream.
        /// </remarks>
        public QuantConnectAppender()
        {
            algo = log4net.GlobalContext.Properties["QCAlgorithm"] as QCAlgorithm;
            if (algo == null)
                Console.Error.WriteLine(
                    "WARN: Global property QCAlgorithm not set. QuantConnectAppender reverting to Console output.");
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// The maximum number of lines to log. After hit this number, nothing more will be logged in a run.
        /// Set to 0 for no maximum.
        /// </summary>
        public int MaximumNumberLines { get; set; }

        /// <summary>
        /// Target is the value of the output stream.
        /// This is either <c>"Console.Out"</c> or <c>"Console.Error"</c>.
        /// </summary>
        /// <value>
        /// Target is the value of the console output stream.
        /// This is either <c>"Console.Out"</c> or <c>"Console.Error"</c>.
        /// </value>
        /// <remarks>
        /// <para>
        /// Target is the value of the console output stream.
        /// This is either <c>"Console.Out"</c> or <c>"Console.Error"</c>.
        /// </para>
        /// </remarks>
        public virtual string Target
        {
            get => m_writeToErrorStream ? Error : Log;
            set
            {
                string v = value.Trim();

                if (string.Compare(Error, v, true, CultureInfo.InvariantCulture) == 0)
                {
                    m_writeToErrorStream = true;
                }
                else
                {
                    m_writeToErrorStream = false;
                }
            }
        }

        #endregion Public Instance Properties

        #region Override implementation of AppenderSkeleton

        /// <summary>
        /// This method is called by the <see cref="AppenderSkeleton.DoAppend(LoggingEvent)"/> method.
        /// </summary>
        /// <param name="loggingEvent">The event to log.</param>
        /// <remarks>
        /// <para>
        /// Writes the event to the console.
        /// </para>
        /// <para>
        /// The format of the output will depend on the appender's layout.
        /// </para>
        /// </remarks>
        protected override void Append(LoggingEvent loggingEvent)
        {
            // handle not going over max.
            if (MaximumNumberLines > 0)
            {
                if (++numLogLines > MaximumNumberLines)
                    return;
                Write(RenderLoggingEvent(loggingEvent));
                if (numLogLines == MaximumNumberLines)
                    Write($"Exceeded {MaximumNumberLines} lines - logging truncated.");
                return;
            }

            Write(RenderLoggingEvent(loggingEvent));
        }

        private void Write(string message)
        {
            // if we have the algo, use that.
            if (algo != null)
            {
                if (m_writeToErrorStream)
                    algo.Error(message);
                else
                    algo.Log(message);
                return;
            }

            // no algo, write to console.
#if NETCF_1_0
			// Write to the output stream
			Console.Write(RenderLoggingEvent(loggingEvent));
#else
            if (m_writeToErrorStream)
            {
                // Write to the error stream
                Console.Error.Write(message);
            }
            else
            {
                // Write to the output stream
                Console.Write(message);
            }
#endif
        }

        /// <summary>
        /// This appender requires a <see cref="Layout"/> to be set.
        /// </summary>
        /// <value><c>true</c></value>
        /// <remarks>
        /// <para>
        /// This appender requires a <see cref="Layout"/> to be set.
        /// </para>
        /// </remarks>
        override protected bool RequiresLayout
        {
            get { return true; }
        }

        #endregion Override implementation of AppenderSkeleton

        #region Public Static Fields

        /// <summary>
        /// The <see cref="QuantConnectAppender.Target"/> to use when writing to the QuantConnectAppender.Log() stream.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="QuantConnectAppender.Target"/> to use when writing to the QuantConnectAppender.Log() stream.
        /// </para>
        /// </remarks>
        public const string Log = "Log";

        /// <summary>
        /// The <see cref="QuantConnectAppender.Target"/> to use when writing to the QuantConnectAppender.Error() output stream.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="QuantConnectAppender.Target"/> to use when writing to the QuantConnectAppender.Error() stream.
        /// </para>
        /// </remarks>
        public const string Error = "Error";

        #endregion Public Static Fields

        #region Private Instances Fields

        private bool m_writeToErrorStream = false;

        /// <summary>
        /// The QC algorithm we call for Log() and Error().
        /// </summary>
        private readonly QCAlgorithm algo;

        /// <summary>
        /// The number of lines logged so far.
        /// </summary>
        private int numLogLines = 0;

        #endregion Private Instances Fields
    }
}

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
*/

using System;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect
{
    /// <summary>
    /// Defines a base class for <see cref="IDataProviderEvents"/>
    /// </summary>
    public abstract class DataProviderEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the symbol being processed that generated the event
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataProviderEventArgs"/> class
        /// </summary>
        /// <param name="symbol">Symbol being processed that generated the event</param>
        protected DataProviderEventArgs(Symbol symbol)
        {
            Symbol = symbol;
        }
    }

    /// <summary>
    /// Event arguments for the <see cref="IDataProviderEvents.InvalidConfigurationDetected"/> event
    /// </summary>
    public sealed class InvalidConfigurationDetectedEventArgs : DataProviderEventArgs
    {
        /// <summary>
        /// Gets the error message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidConfigurationDetectedEventArgs"/> class
        /// </summary>
        /// <param name="symbol">Symbol being processed that generated the event</param>
        /// <param name="message">The error message</param>
        public InvalidConfigurationDetectedEventArgs(Symbol symbol, string message)
            : base(symbol)
        {
            Message = message;
        }
    }

    /// <summary>
    /// Event arguments for the <see cref="IDataProviderEvents.NumericalPrecisionLimited"/> event
    /// </summary>
    public sealed class NumericalPrecisionLimitedEventArgs : DataProviderEventArgs
    {
        /// <summary>
        /// Gets the error message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NumericalPrecisionLimitedEventArgs"/> class
        /// </summary>
        /// <param name="symbol">Symbol being processed that generated the event</param>
        /// <param name="message">The error message</param>
        public NumericalPrecisionLimitedEventArgs(Symbol symbol, string message)
            : base(symbol)
        {
            Message = message;
        }
    }

    /// <summary>
    /// Event arguments for the <see cref="IDataProviderEvents.DownloadFailed"/> event
    /// </summary>
    public sealed class DownloadFailedEventArgs : DataProviderEventArgs
    {
        /// <summary>
        /// Gets the error message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the error stack trace
        /// </summary>
        public string StackTrace { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloadFailedEventArgs"/> class
        /// </summary>
        /// <param name="symbol">Symbol being processed that generated the event</param>
        /// <param name="message">The error message</param>
        /// <param name="stackTrace">The error stack trace</param>
        public DownloadFailedEventArgs(Symbol symbol, string message, string stackTrace = "")
            : base(symbol)
        {
            Message = message;
            StackTrace = stackTrace;
        }
    }

    /// <summary>
    /// Event arguments for the <see cref="IDataProviderEvents.ReaderErrorDetected"/> event
    /// </summary>
    public sealed class ReaderErrorDetectedEventArgs : DataProviderEventArgs
    {
        /// <summary>
        /// Gets the error message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the error stack trace
        /// </summary>
        public string StackTrace { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReaderErrorDetectedEventArgs"/> class
        /// </summary>
        /// <param name="symbol">Symbol being processed that generated the event</param>
        /// <param name="message">The error message</param>
        /// <param name="stackTrace">The error stack trace</param>
        public ReaderErrorDetectedEventArgs(Symbol symbol, string message, string stackTrace = "")
            : base(symbol)
        {
            Message = message;
            StackTrace = stackTrace;
        }
    }

    /// <summary>
    /// Event arguments for the <see cref="IDataProviderEvents.StartDateLimited"/> event
    /// </summary>
    public sealed class StartDateLimitedEventArgs : DataProviderEventArgs
    {
        /// <summary>
        /// Gets the error message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StartDateLimitedEventArgs"/> class
        /// </summary>
        /// <param name="symbol">Symbol being processed that generated the event</param>
        /// <param name="message">The error message</param>
        public StartDateLimitedEventArgs(Symbol symbol, string message)
            : base(symbol)
        {
            Message = message;
        }
    }

    /// <summary>
    /// Event arguments for the NewTradableDate event
    /// </summary>
    public sealed class NewTradableDateEventArgs : DataProviderEventArgs
    {
        /// <summary>
        /// The new tradable date
        /// </summary>
        public DateTime Date { get; }

        /// <summary>
        /// The last <see cref="BaseData"/> of the <see cref="Security"/>
        /// for which we are enumerating
        /// </summary>
        public BaseData LastBaseData { get; }

        /// <summary>
        /// The last raw security price we have
        /// </summary>
        public decimal? LastRawPrice { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NewTradableDateEventArgs"/> class
        /// </summary>
        /// <param name="date">The new tradable date</param>
        /// <param name="lastBaseData">The last <see cref="BaseData"/> of the
        /// <see cref="Security"/> for which we are enumerating</param>
        /// <param name="symbol">The <see cref="Symbol"/> of the new tradable date</param>
        /// <param name="lastRawPrice">The last raw security price we have</param>
        public NewTradableDateEventArgs(DateTime date, BaseData lastBaseData, Symbol symbol, decimal? lastRawPrice)
            : base(symbol)
        {
            Date = date;
            LastBaseData = lastBaseData;
            LastRawPrice = lastRawPrice;
        }
    }
}

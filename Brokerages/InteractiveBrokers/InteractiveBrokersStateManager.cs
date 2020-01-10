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

using System.Collections.Generic;

namespace QuantConnect.Brokerages.InteractiveBrokers
{
    /// <summary>
    /// The IBAutomater error type
    /// </summary>
    public enum IbAutomaterErrorType
    {
        /// <summary>
        /// No error
        /// </summary>
        None,

        /// <summary>
        /// The login failed
        /// </summary>
        LoginFailed,

        /// <summary>
        /// An existing session was detected
        /// </summary>
        ExistingSessionDetected,

        /// <summary>
        /// A security dialog (2FA/code card) was detected
        /// </summary>
        SecurityDialogDetected
    }

    /// <summary>
    /// Holds the brokerage state information (connection status, error conditions, etc.)
    /// </summary>
    public class InteractiveBrokersStateManager
    {
        private volatile bool _disconnected1100Fired;
        private volatile bool _previouslyInResetTime;
        private volatile IbAutomaterErrorType _ibAutomaterErrorType = IbAutomaterErrorType.None;

        private readonly Dictionary<IbAutomaterErrorType, string> _ibAutomaterErrorMessages =
            new Dictionary<IbAutomaterErrorType, string>
            {
                {
                    IbAutomaterErrorType.LoginFailed,
                    "Login failed. Please check the validity of your login credentials."
                },
                {
                    IbAutomaterErrorType.ExistingSessionDetected,
                    "An existing session was detected and will not be automatically disconnected. " +
                    "Please close the existing session manually."
                },
                {
                    IbAutomaterErrorType.SecurityDialogDetected,
                    "A security dialog was detected for Second Factor/Code Card Authentication. " +
                    "Please opt out of the Secure Login System: Manage Account > Security > Secure Login System > SLS Opt Out"
                },
            };

        /// <summary>
        /// Gets/sets whether the IB client has received a Disconnect (1100) message
        /// </summary>
        public bool Disconnected1100Fired
        {
            get
            {
                return _disconnected1100Fired;
            }

            set
            {
                _disconnected1100Fired = value;
            }
        }

        /// <summary>
        /// Gets/sets whether the previous reconnection attempt was performed during the IB reset period
        /// </summary>
        public bool PreviouslyInResetTime
        {
            get
            {
                return _previouslyInResetTime;
            }

            set
            {
                _previouslyInResetTime = value;
            }
        }

        /// <summary>
        /// Gets/sets whether an IBAutomater error occurred
        /// </summary>
        public IbAutomaterErrorType IbAutomaterErrorType
        {
            get
            {
                return _ibAutomaterErrorType;
            }

            set
            {
                _ibAutomaterErrorType = value;
            }
        }

        /// <summary>
        /// Checks if any IBAutomater error has occurred
        /// </summary>
        /// <returns>true if any IBAutomater error has occurred</returns>
        public bool HasIbAutomaterErrors()
        {
            return _ibAutomaterErrorType != IbAutomaterErrorType.None;
        }

        /// <summary>
        /// Returns an error message for the given IBAutomater error
        /// </summary>
        /// <returns>The error message</returns>
        public string GetIbAutomaterErrorMessage()
        {
            string errorMessage;
            return _ibAutomaterErrorMessages.TryGetValue(_ibAutomaterErrorType, out errorMessage) ? errorMessage : string.Empty;
        }

        /// <summary>
        /// Resets the state to the default values
        /// </summary>
        public void Reset()
        {
            _disconnected1100Fired = false;
            _previouslyInResetTime = false;

            // IBAutomater errors are not recoverable, so we don't reset them
        }
    }
}

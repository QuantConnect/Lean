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

namespace QuantConnect.Brokerages.InteractiveBrokers
{
    /// <summary>
    /// Holds the brokerage state information (connection status, error conditions, etc.)
    /// </summary>
    public class InteractiveBrokersStateManager
    {
        private volatile bool _disconnected1100Fired;
        private volatile bool _previouslyInResetTime;
        private volatile bool _loginFailed;
        private volatile bool _existingSessionDetected;
        private volatile bool _securityDialogDetected;

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
        /// Gets/sets whether a login failure was detected by IBAutomater
        /// </summary>
        public bool LoginFailed
        {
            get
            {
                return _loginFailed;
            }

            set
            {
                _loginFailed = value;
            }
        }

        /// <summary>
        /// Gets/sets whether an existing session was detected by IBAutomater
        /// </summary>
        public bool ExistingSessionDetected
        {
            get
            {
                return _existingSessionDetected;
            }

            set
            {
                _existingSessionDetected = value;
            }
        }

        /// <summary>
        /// Gets/sets whether a security dialog (2FA/code card) was detected by IBAutomater
        /// </summary>
        public bool SecurityDialogDetected
        {
            get
            {
                return _securityDialogDetected;
            }

            set
            {
                _securityDialogDetected = value;
            }
        }

        /// <summary>
        /// Checks if any IBAutomater error has occurred
        /// </summary>
        /// <returns>true if any IBAutomater error has occurred</returns>
        public bool HasIbAutomaterErrors()
        {
            return _loginFailed || _existingSessionDetected || _securityDialogDetected;
        }

        /// <summary>
        /// Resets the state to the default values
        /// </summary>
        public void Reset()
        {
            _disconnected1100Fired = false;
            _previouslyInResetTime = false;
            _loginFailed = false;
            _existingSessionDetected = false;
            _securityDialogDetected = false;
        }
    }
}

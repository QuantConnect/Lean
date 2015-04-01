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
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages
{
    public abstract class BrokerageAuthentication
    {
        /// <summary>
        /// Input Validation
        /// </summary>
        /// <param name="error">Error Message (out)</param>
        /// <returns>true for OK (ie. no error)</returns>
        public abstract bool Validate(out string error);

        public abstract string IntroMessage { get; } 

        [AttributeUsage(AttributeTargets.Property)]
        public class WebUIAttribute : System.Attribute
        {
            private string prompt;
            private string helpUrl;
            private bool mandatory;

            /// <summary>
            /// User Prompt
            /// </summary>
            public string Prompt
            {
                get
                {
                    return prompt;
                }
                set
                {

                    prompt = value;
                }
            }

            /// <summary>
            /// User Help URL
            /// </summary>
            public string HelpUrl
            {
                get
                {
                    return helpUrl;
                }
                set
                {

                    helpUrl = value;
                }
            }

            /// <summary>
            /// Mandatory User Input
            /// </summary>
            public bool Mandatory
            {
                get
                {
                    return mandatory;
                }
                set
                {

                    mandatory = value;
                }
            }

        }

    }
}

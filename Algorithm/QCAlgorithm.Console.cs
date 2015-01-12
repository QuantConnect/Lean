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
/**********************************************************
* USING NAMESPACES
**********************************************************/
namespace QuantConnect.Algorithm
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Console is a helper class to override default behaviour of Console.WriteLine(); and force the write-line messages 
    /// to be managed by the application, and if required appear in the web browser IDE console.
    /// </summary>
    /// <![CDATA[{ "caption":"Console.WriteLine(string)", "value":"Debug(string message)", "meta":"method" } ]]>
    public static class Console
    {
        private static QCAlgorithm _algorithmNamespace;

        /// <summary>
        /// Initialize the Console override to send the messages to the algorithm debug function.
        /// </summary>
        /// <param name="algorithmNamespace">Algorithm Debug Function Access</param>
        public static void Initialize(QCAlgorithm algorithmNamespace)
        {
            _algorithmNamespace = algorithmNamespace;
        }

        /// <summary>
        /// Override method to write a line to the console in the browser. Made to appear like the System.Console handler.
        /// </summary>
        /// <param name="consoleMessage">String message to send to console.</param>
        /// <seealso cref="Write"/>
        public static void WriteLine(string consoleMessage)
        {
            _algorithmNamespace.Debug(consoleMessage);
        }

        /// <summary>
        /// Override method to write a line to the console in the browser. Made to appear like the System.Console handler.
        /// </summary>
        /// <param name="consoleMessage">String message to send.</param>
        /// <seealso cref="WriteLine"/>
        public static void Write(string consoleMessage)
        {
            _algorithmNamespace.Debug(consoleMessage);
        }

        /// <summary>
        /// Error handler override assing messages via static method to the console.
        /// </summary>
        /// <param name="errorMessage">String message to send.</param>
        /// <seealso cref="WriteLine"/>
        public static void Error(string errorMessage)
        {
            _algorithmNamespace.Error(errorMessage);
        }

        /// <summary>
        /// Error handler override assing messages via static method to the console.
        /// </summary>
        /// <param name="logMessage">String message to send.</param>
        /// <seealso cref="WriteLine"/>
        public static void Log(string logMessage)
        {
            _algorithmNamespace.Log(logMessage);
        }
    }

} // End QC Namespace

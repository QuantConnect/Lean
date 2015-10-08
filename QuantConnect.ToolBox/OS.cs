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

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Operating System Assistance Methods:
    /// </summary>
    public class OS
    {

        /// <summary>
        /// Flag indicating the platform is linux
        /// </summary>
        public static bool IsLinux
        {
            get
            {
                var p = (int)System.Environment.OSVersion.Platform;
                return p == 4 || p == 6 || p == 128;
            }
        }

        /// <summary>
        /// Flag indicating the platform is windows.
        /// </summary>
        public static bool IsWindows
        {
            get
            {
                return !OS.IsLinux;
            }
        }

        /// <summary>
        /// Path separation character
        /// </summary>
        public static string PathSeparation
        {
            get
            {
                return System.IO.Path.DirectorySeparatorChar.ToString();
            }
        }
    }
}

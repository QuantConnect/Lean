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

using System.Diagnostics;
using Microsoft.Win32;
using System.Threading;
using QuantConnect.Configuration;

namespace QuantConnect.ToolBox.IQFeed
{

    public class IQConnect
    {
        public IQConnect(string product = "", string version = "")
        {
            _product = product;
            _version = version;
        }
        public string Product { get { return _product; } }
        public string Version { get { return _version; } }
        
        public IQCredentials getCredentials()
        {
            return new IQCredentials(
                Config.Get("iqfeed-username"),
                Config.Get("iqfeed-password"),
                true
                );
        }

        public string getPath()
        {
            var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\DTN\\IQFeed");
            if (key == null)
            {
                // if it isn't in that location, it is possible the user is running and x64 OS.  Check the windows virtualized registry location
                key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\DTN\\IQFeed");
            }
            string sLocation = null;
            if (key != null)
            {
                sLocation = key.GetValue("EXEDIR", "").ToString();
                // close the key since we don't need it anymore
                key.Close();
                // verify there is a \ on the end before we append the exe name
                if (!(sLocation.EndsWith("\\") || sLocation.EndsWith("/")))
                {
                    sLocation += "\\";
                }
                sLocation += "IQConnect.exe";
            }
            return sLocation;
        }
        public string getArguments(IQCredentials iqc)
        {
            var arguments = "";

            if (Product != "") { arguments += "-product " + Product + " "; }
            if (Version != "") { arguments += "-version " + Version + " "; }
            if (iqc.LoginId != "") { arguments += "-login " + iqc.LoginId + " "; }
            if (iqc.Password != "") { arguments += "-password " + iqc.Password + " "; }
            if (iqc.SaveCredentials) { arguments += "-savelogininfo "; }
            if (iqc.AutoConnect) { arguments += "-autoconnect"; }
            arguments.TrimEnd(' ');

            return arguments;
        }

        public bool Launch(IQCredentials iqc = null, string arguments = null, int pauseMilliseconds = 6000)
        {
            if (iqc == null) { iqc = getCredentials(); }
            if (iqc == null) { iqc = new IQCredentials(); }
            if (arguments == null) { arguments = getArguments(iqc); }

            var psi = new ProcessStartInfo("IQConnect.exe", arguments) { UseShellExecute = true };
            Process.Start(psi);
            Thread.Sleep(pauseMilliseconds);
            return true;
        }

        #region private
        private string _product;
        private string _version;
        #endregion
    }
}

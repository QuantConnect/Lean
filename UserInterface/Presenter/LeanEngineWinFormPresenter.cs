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
using System.IO;
using System.Windows.Forms;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Messaging;
using QuantConnect.Packets;
using QuantConnect.Views.Model;
using QuantConnect.Views.View;
using QuantConnect.Views.WinForms;

namespace QuantConnect.Views.Presenter
{
    public class LeanEngineWinFormPresenter : IPresenter
    {
        private readonly ILeanEngineWinFormView _view;
        private readonly LeanEngineWinFormModel _model;

        private void HandleDebugPacket(DebugPacket packet)
        {
            Log.Trace("Debug: " + packet.Message);
        }
        private void HandleLogPacket(LogPacket packet)
        {
            Log.Trace("Log: " + packet.Message);
        }
        private void HandleRuntimeErrorPacket(RuntimeErrorPacket packet)
        {
            var rstack = (!string.IsNullOrEmpty(packet.StackTrace) ? (Environment.NewLine + " " + packet.StackTrace) : string.Empty);
            Log.Error(packet.Message + rstack);
        }

        private void HandleHandledErrorPacket(HandledErrorPacket packet)
        {
            var hstack = (!string.IsNullOrEmpty(packet.StackTrace) ? (Environment.NewLine + " " + packet.StackTrace) : string.Empty);
            Log.Error(packet.Message + hstack);

        }

        private void HandleBacktestResultPacket(BacktestResultPacket packet)
        {
            if (packet.Progress == 1)
            {
                foreach (var pair in packet.Results.Statistics)
                {
                    _model.LogText += "STATISTICS:: " + pair.Key + " " + pair.Value;
                    Log.Trace("STATISTICS:: " + pair.Key + " " + pair.Value);
                }

                if (StreamingApi.IsEnabled)
                {
                     if ((LeanEngineWinForm) _view != null)
                     {
                         var url = string.Format("https://www.quantconnect.com/terminal/embedded?user={0}&token={1}&bid={2}&pid={3}",
                             packet.UserId, Config.Get("api-access-token"), packet.BacktestId, packet.ProjectId);
                         ((LeanEngineWinForm)_view).Browser.Navigate(url);
                     }
                }
            }
        }


        public LeanEngineWinFormPresenter(ILeanEngineWinFormView view, LeanEngineWinFormModel model, EventMessagingHandler messageHandler)
        {
            _view = view;
            _model = model;
            view.ExitApplication += ExitApplication;
            view.TickerTick += TimerOnTick;
            view.ConsoleOnKeyUp += ConsoleOnKeyUp;
            messageHandler.DebugEvent += HandleDebugPacket;
            messageHandler.LogEvent += HandleLogPacket;
            messageHandler.RuntimeErrorEvent += HandleRuntimeErrorPacket;
            messageHandler.HandledErrorEvent += HandleHandledErrorPacket;
            messageHandler.BacktestResultEvent += HandleBacktestResultPacket;
        }


        /// <summary>
        /// Binding to the Console Key Press. In the console there's virtually nothing for user input other than the end of the backtest.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="keyEventArgs"></param>
        private void ConsoleOnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            if ((LeanEngineWinForm) _view != null && ((LeanEngineWinForm) _view).ResultsHandler != null && !((LeanEngineWinForm) _view).ResultsHandler.IsActive)
            {
                Environment.Exit(0);
            }
        }
        
        /// <summary>
        /// Update performance counters
        /// </summary>
        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            _model.StatusStripStatisticsText = string.Concat("Performance: CPU: " , OS.CpuUsage.NextValue().ToString("0.0") , "%", 
                                                                " Ram: " , OS.TotalPhysicalMemoryUsed , " Mb");
            _view.OnPropertyChanged(_model);
        }
        
        private void ExitApplication(object sender, EventArgs e)
        {
            if (((LeanEngineWinForm)_view).Engine != null)
            { 
                ((LeanEngineWinForm) _view).Engine.SystemHandlers.Dispose();
                ((LeanEngineWinForm) _view).Engine.AlgorithmHandlers.Dispose();
            }
            Environment.Exit(0);
        }
    }
}
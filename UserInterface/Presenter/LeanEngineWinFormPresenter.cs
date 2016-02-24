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
using System.Drawing;
using System.Windows.Forms;
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
        private readonly EventMessagingHandler _eventMessagingHandler;

        public LeanEngineWinFormPresenter(ILeanEngineWinFormView view, LeanEngineWinFormModel model)
        {
            _view = view;
            _model = model;
            view.ExitApplication += ExitApplication;
            //view.PollingTick += PollingTick;
            view.TickerTick += TimerOnTick;
            view.ConsoleOnKeyUp += ConsoleOnKeyUp;
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

        /// <summary>
        ///     Primary polling thread for the logging and chart display.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void PollingTick(object sender, EventArgs eventArgs)
        {
            Packet message;
            if (((LeanEngineWinForm) _view).ResultsHandler == null) return;
            while (((LeanEngineWinForm) _view).ResultsHandler.Messages.TryDequeue(out message))
            {
                //get the messaging system instance

                //Process the packet request:
                switch (message.Type)
                {
                    case PacketType.BacktestResult:
                        //Draw chart
                        break;

                    case PacketType.LiveResult:
                        //Draw streaming chart
                        break;

                    case PacketType.AlgorithmStatus:
                        //Algorithm status update
                        break;

                    case PacketType.RuntimeError:
                        var runError = message as RuntimeErrorPacket;
                        if (runError != null) AppendConsole(runError.Message, Color.Red);
                        break;

                    case PacketType.HandledError:
                        var handledError = message as HandledErrorPacket;
                        if (handledError != null) AppendConsole(handledError.Message, Color.Red);
                        break;

                    case PacketType.Log:
                        var log = message as LogPacket;
                        if (log != null) AppendConsole(log.Message);
                        break;

                    case PacketType.Debug:
                        var debug = message as DebugPacket;
                        if (debug != null) AppendConsole(debug.Message);
                        break;

                    case PacketType.OrderEvent:
                        //New order event.
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        ///     Write to the console in specific font color.
        /// </summary>
        /// <param name="message">String to append</param>
        /// <param name="color">Defaults to black</param>
        private void AppendConsole(string message, Color color = default(Color))
        {
            message = DateTime.Now.ToString("u") + " " + message + Environment.NewLine;
            //Add to console:
            var console = ((LeanEngineWinForm) _view).RichTextBoxLog;
            console.AppendText(message);
            console.Refresh();
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
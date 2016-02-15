using System;
using System.Windows.Forms;
using QuantConnect.Views.Model;

namespace QuantConnect.Views.View
{
    public interface ILeanEngineWinFormView
    {
        event EventHandler PollingTick;
        event EventHandler ExitApplication;
        event EventHandler TickerTick;
        event KeyEventHandler ConsoleOnKeyUp;

        void OnPropertyChanged(LeanEngineWinFormModel model);
    }
}
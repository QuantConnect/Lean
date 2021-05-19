using System;
using System.Collections.Generic;

namespace QuantConnect.ToolBox.ExanteDownloader
{
    public class ExanteDownloaderProgram
    {
        public static void ExanteDownloader(IList<string> tickers, string resolution, DateTime fromDate, DateTime toDate)
        {
            var downloader = new ExanteDataDownloader();
            downloader.Download();
        }
    }
}

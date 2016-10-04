using System;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using QuantConnect.Logging;

namespace QuantConnect.Optimizer
{
    //An executable project taking an IAlgorithm object and launching backtests.
    class Program
    {
        private static AppDomainSetup _ads;
        private static string _callingDomainName;
        private static string _exeAssembly;

        public static void Main(string[] args)
        {
            _ads = SetupAppDomain();

            string algoName = "ParameterizedAlgorithm";
            
            IDictionary<string,string> backtestStatistics = RunAlgorithm(algoName);
            
            Log.Trace("Optimization(): ===================================================");
            Log.Trace("Optimization(): Backtest Result: ");
            foreach (var stat in backtestStatistics.Keys)
            {
                Log.Trace("Optimization(): "+ stat + ": " + backtestStatistics[stat]);
            }

            Console.ReadKey();
        }

        static AppDomainSetup SetupAppDomain()
        {
            _callingDomainName = Thread.GetDomain().FriendlyName;
           
            // Get and display the full name of the EXE assembly.
            _exeAssembly = Assembly.GetEntryAssembly().FullName;
           
            // Construct and initialize settings for a second AppDomain.
            AppDomainSetup ads = new AppDomainSetup();
            ads.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;

            ads.DisallowBindingRedirects = false;
            ads.DisallowCodeDownload = true;
            ads.ConfigurationFile =
                AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            return ads;
        }

        static RunClass CreateRunClassInAppDomain(ref AppDomain ad)
        {

            // Create the second AppDomain.
            var name = Guid.NewGuid().ToString("x");
            ad = AppDomain.CreateDomain(name, null, _ads);

            // Create an instance of MarshalbyRefType in the second AppDomain. 
            // A proxy to the object is returned.

            RunClass rc =
                (RunClass)ad.CreateInstanceAndUnwrap(
                    _exeAssembly,
                    typeof(RunClass).FullName
                );

            return rc;
        }
        private static IDictionary<string, string> RunAlgorithm(string algoName)
        {
            IDictionary<string, string>  backTestStatistics = null;

            try
            {
                AppDomain ad = null;
                RunClass rc = CreateRunClassInAppDomain(ref ad);
                
                backTestStatistics = rc.Run(algoName);

                AppDomain.Unload(ad);

            }
            catch (Exception ex)
            {

                Log.Error("Optimization(): Exception: " + ex.Message);
                Log.Error("Optimization(): InnerException: " + ex.InnerException);
                Log.Error("Optimization(): StackTrace: " + ex.StackTrace);
            }
            return backTestStatistics;
        }

    }
}

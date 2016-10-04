using System;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Algorithm;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Algorithm.CSharp;

namespace QuantConnect.Optimization
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

            QCAlgorithm algo = new ParameterizedAlgorithm();


            string algoName = "ParameterizedAlgorithm";
            
            string[] results = RunAlgorithm(algoName);

            var ratio = results[0];
            
            Console.WriteLine("Ratio: " + ratio);
            Console.ReadLine();
        }

        static AppDomainSetup SetupAppDomain()
        {
            _callingDomainName = Thread.GetDomain().FriendlyName;
            //Console.WriteLine(callingDomainName);

            // Get and display the full name of the EXE assembly.
            _exeAssembly = Assembly.GetEntryAssembly().FullName;
            //Console.WriteLine(exeAssembly);

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
        private static string[] RunAlgorithm(string algoName)
        {
            string[] results = null;

            try
            {
                AppDomain ad = null;
                RunClass rc = CreateRunClassInAppDomain(ref ad);
                
                results = rc.Run(algoName);

                AppDomain.Unload(ad);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                Console.WriteLine("InnerException: " + ex.InnerException);
                Console.WriteLine("StackTrace: " + ex.StackTrace);
                Console.ReadLine();
            }
            return results;
        }

    }
}

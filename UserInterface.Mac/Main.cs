using AppKit;
using System;

namespace QuantConnect.Views.Mac
{
    static class MainClass
    {
        static void Main(string[] args)
        {
			var port = args[0];

			// when no arg passed in
			if (port.StartsWith("-psn"))
			{
                Console.WriteLine("QuantConnect Views must be started from Lean Launcher.");
            } else {
				NSApplication.Init();
				NSApplication.Main(args);
			}
        }
    }
}

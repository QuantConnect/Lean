using System;
using System.IO;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public class ConsoleLeveledOutput
    {
        public TextWriter Info { get; }
        public TextWriter Warn { get; }
        public TextWriter Error { get; }
        public bool ErrorMessageWritten { get; private set; }

        public ConsoleLeveledOutput()
        {
            Info = Console.Out;
            Warn = new FuncTextWriter(line =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(line);
                Console.ResetColor();
            });
            Error = new FuncTextWriter(line =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(line);
                Console.ResetColor();
                ErrorMessageWritten = true;
            });
        }

        public ConsoleLeveledOutput(TextWriter info, TextWriter warn, TextWriter error)
        {
            Info = info;
            Warn = warn;
            Error = error;
        }
    }
}
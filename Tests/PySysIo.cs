// sourced from: https://github.com/yagweb/pythonnetLab/blob/master/pynetLab/PySysIO.cs

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Python.Runtime;

namespace QuantConnect.Tests
{
    public static class PySysIo
    {
        public static void ToConsoleOut()
        {
            using (Py.GIL())
            {
                PythonEngine.RunSimpleString(@"
import sys
from System import Console
class output(object):
    def write(self, msg):
        Console.Out.Write(msg)
    def writelines(self, msgs):
        for msg in msgs:
            Console.Out.Write(msg)
    def flush(self):
        pass
    def close(self):
        pass
sys.stdout = sys.stderr = output()
");
            }
        }

        private static readonly SysIoWriter SysIoStream = new();

        public static TextWriter TextWriter => SysIoStream.TextWriter;

        public static TextWriter ToTextWriter(TextWriter writer = null)
        {
            using (Py.GIL())
            {
                SysIoStream.TextWriter = writer;
                dynamic sys = Py.Import("sys");
                sys.stdout = sys.stderr = SysIoStream;
            }
            return SysIoStream.TextWriter;
        }

        public static void Flush()
        {
            SysIoStream.flush();
        }
    }

    /// <summary>
    /// Implement the interface of the sys.stdout redirection
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class SysIoWriter
    {
        private TextWriter _textWriter;
        public TextWriter TextWriter
        {
            get => _textWriter ?? Console.Out;
            set => _textWriter = value;
        }

        public SysIoWriter(TextWriter writer = null)
        {
            _textWriter = writer;
        }

        public void write(string str) => TextWriter.Write(str);

        public void writelines(string[] str)
        {
            if (str == null)
            {
                return;
            }

            foreach (var line in str)
            {
                write(line);
            }
        }

        public void flush() => TextWriter.Flush();

        public void close() => _textWriter?.Close();
    }
}

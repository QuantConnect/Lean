using EnvDTE80;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Collections of util methods to work with directories
    /// </summary>
    class PathUtils
    {

        private PathUtils() { }

        public static string GetSolutionFolder(DTE2 dte2)
        {
            return Path.GetDirectoryName(dte2.Solution.FullName);
        }
    }
}

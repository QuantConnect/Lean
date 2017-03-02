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

using System.Collections.Generic;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Stores associations between a list of files and a QuantConnect project 
    /// that is associated with them.
    /// </summary>
    class ProjectFinder
    {
        private IDictionary<HashSet<string>, string> _projectForFiles 
            = new Dictionary<HashSet<string>, string>(HashSet<string>.CreateSetComparer());

        public ProjectFinder()
        {

        }

        /// <summary>
        /// Get a project name that is associated with the provided list of files
        /// </summary>
        /// <param name="files">List of files in a project</param>
        /// <returns>A name of a project if a list of files is associated with a project, empty string otherwise</returns>
        public string ProjectNameForFiles(List<string> files)
        {
            string projectName;
            if (_projectForFiles.TryGetValue(new HashSet<string>(files), out projectName))
            {
                return projectName;
            }
            return "";
        }

        /// <summary>
        /// Associate a project with a project name
        /// </summary>
        /// <param name="projectName">A project name to associate list of files with</param>
        /// <param name="files">A list of files to associate with a project name</param>
        public void AssociateProjectWith(string projectName, List<string> files)
        {
            _projectForFiles.Add(new HashSet<string>(files), projectName);
        }
    }
}

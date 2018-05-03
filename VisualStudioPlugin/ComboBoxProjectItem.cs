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
using System.Linq;
using System.Runtime.Serialization;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Item that represents project name and project id in a combo box
    /// </summary>
    internal class ComboboxProjectItem
    {
        public int Id { get; }
        public string Name { get; }
        public Language Language { get; }
        private readonly string _namePrefix;

        public ComboboxProjectItem(int id, string name, Language language)
        {
            Id = id;
            Name = name;
            Language = language;
            var memInfo = typeof(Language).GetMember(Language.ToString());
            var attr = memInfo[0].GetCustomAttributes(false).OfType<EnumMemberAttribute>().FirstOrDefault();
            if (attr != null)
            {
                _namePrefix = attr.Value + " - ";
            }
        }

        public override string ToString()
        {
            return _namePrefix + Name;
        }
    }
}

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
 *
*/

namespace QuantConnect.Packets
{
    /// <summary>
    /// Holds the permissions for the object store
    /// </summary>
    public class StoragePermissions
    {
        /// <summary>
        /// Whether the user has read permissions on the object store
        /// </summary>
        public bool Read { get; set; }

        /// <summary>
        /// Whether the user has write permissions on the object store
        /// </summary>
        public bool Write { get; set; }

        /// <summary>
        /// Whether the user has delete permissions on the object store
        /// </summary>
        public bool Delete { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StoragePermissions"/> struct with default permissions.
        /// </summary>
        public StoragePermissions()
        {
            // default permissions for controls storage
            Read = true;
            Write = true;
            Delete = true;
        }

        /// <summary>
        /// Returns a string representation of the storage permissions.
        /// </summary>
        public override string ToString()
        {
            return $"Read={Read} Write={Write} Delete={Delete}";
        }
    }
}

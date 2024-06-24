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

using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuantConnect
{
    /// <summary>
    /// Provides user-facing message construction methods and static messages for the <see cref="Notifications"/> namespace
    /// </summary>
    public static partial class Messages
    {
        /// <summary>
        /// Provides user-facing messages for the <see cref="Notifications.NotificationEmail"/> class and its consumers or related classes
        /// </summary>
        public static class NotificationEmail
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidEmailAddress(string email)
            {
                return $"Invalid email address: {email}";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Notifications.NotificationFtp"/> class and its consumers or related classes
        /// </summary>
        public static class NotificationFtp
        {
            public static string MissingSSHKeys = "FTP SSH keys missing for SFTP notification.";

            public static string MissingPassword = "FTP password is missing for unsecure FTP notification.";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Notifications.NotificationJsonConverter"/> class and its consumers or related classes
        /// </summary>
        public static class NotificationJsonConverter
        {
            public static string WriteNotImplemented = "Not implemented, should not be called";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnexpectedJsonObject(JObject jObject)
            {
                return $"Unexpected json object: '{jObject.ToString(Formatting.None)}'";
            }
        }
    }
}

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

using System;
using QuantConnect.Algorithm.Framework.Portfolio.SignalExports;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Interface to send positions holdings to different 3rd party API's
    /// </summary>
    public interface ISignalExportTarget : IDisposable
    {
        /// <summary>
        /// Sends user's positions to certain 3rd party API
        /// </summary>
        /// <param name="parameters">Holdings the user have defined to be sent to certain 3rd party API and the algorithm being ran</param>
        bool Send(SignalExportTargetParameters parameters);
    }
}

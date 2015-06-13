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

namespace QuantConnect 
{
    /// <summary>
    /// Helper method for a limited length queue which self-removes the extra elements.
    /// http://stackoverflow.com/questions/5852863/fixed-size-queue-which-automatically-dequeues-old-values-upon-new-enques
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FixedSizedQueue<T> : Queue<T>
    {
        private int _limit = -1;

        /// <summary>
        /// Max Length 
        /// </summary>
        public int Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }

        /// <summary>
        /// Create a new fixed length queue:
        /// </summary>
        public FixedSizedQueue(int limit)
            : base(limit)
        {
            Limit = limit;
        }

        /// <summary>
        /// Enqueue a new item int the generic fixed length queue:
        /// </summary>
        public new void Enqueue(T item)
        {
            while (Count >= Limit)
            {
                Dequeue();
            }
            base.Enqueue(item);
        }
    }


} // End QC Namespace

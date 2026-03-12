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
using System.Collections;
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    public interface IBacktestAnalysisContext
    {
    }

    public class BacktestAnalysysContext : IBacktestAnalysisContext
    {
        public object Sample { get; set; }

        public BacktestAnalysysContext(object sample)
        {
            Sample = sample;
        }
    }

    public class BacktestAnalysysRepeatedContext : BacktestAnalysysContext
    {
        public int Occurrences { get; set; }

        public BacktestAnalysysRepeatedContext(IReadOnlyList<object> samples) : base(samples.Count > 0 ? samples[0] : null)
        {
            Occurrences = samples.Count;
        }
    }

    public class BacktestAnalysysAggregateContext : IBacktestAnalysisContext, IEnumerable<IBacktestAnalysisContext>
    {
        private IReadOnlyList<IBacktestAnalysisContext> _contexts { get; set; }

        public BacktestAnalysysAggregateContext(IReadOnlyList<IBacktestAnalysisContext> contexts)
        {
            _contexts = contexts;
        }

        public IEnumerator<IBacktestAnalysisContext> GetEnumerator()
        {
            return _contexts.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class BacktestAnalysisResult
    {
        public string Name { get; set; }

        public IBacktestAnalysisContext Context { get; set; }
        
        public List<string> PotentialSolutions { get; set; }

        public BacktestAnalysisResult(string name, IBacktestAnalysisContext context, List<string> potentialSolutions)
        {
            Name = name;
            Context = context;
            PotentialSolutions = potentialSolutions;
        }
    }
}

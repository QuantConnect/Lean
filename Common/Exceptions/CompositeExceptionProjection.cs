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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using QuantConnect.Logging;

namespace QuantConnect.Exceptions
{
    /// <summary>
    /// Projects exceptions using the configured projections
    /// </summary>
    public class CompositeExceptionProjection : IExceptionProjection
    {
        private readonly List<IExceptionProjection> _projections;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeExceptionProjection"/> class
        /// </summary>
        /// <param name="projections">The projections to use</param>
        public CompositeExceptionProjection(IEnumerable<IExceptionProjection> projections)
        {
            _projections = projections.ToList();
        }

        /// <summary>
        /// Gets the projections loaded into this instance
        /// </summary>
        public IEnumerable<IExceptionProjection> Projections => _projections;

        /// <summary>
        /// Determines if this projection should be applied to the specified exception.
        /// </summary>
        /// <param name="exception">The exception to check</param>
        /// <returns>True if the exception can be projected, false otherwise</returns>
        public bool CanProject(Exception exception)
        {
            return _projections.Any(projection => projection.CanProject(exception));
        }

        /// <summary>
        /// Project the specified exception into a new exception.
        /// The innerProjection parameter is optional, specify null to enable the default recursive
        /// behavior of the composite projection.
        /// </summary>
        /// <param name="exception">The exception to be projected</param>
        /// <param name="innerProjection">A projection that should be applied to the inner exception.
        /// This provides a link back allowing the inner exception to be projected using the projections
        /// configured in the exception projector. Individual implementations *may* ignore this value if
        /// required.</param>
        /// <returns>The projected exception</returns>
        public Exception Project(Exception exception, IExceptionProjection innerProjection)
        {
            if (exception == null)
            {
                return null;
            }

            foreach (var projection in _projections)
            {
                try
                {
                    if (projection.CanProject(exception))
                    {
                        // use this composite projection to project inner exceptions as well, unless one was specified
                        return projection.Project(exception, innerProjection ?? this);
                    }
                }
                catch (Exception err)
                {
                    Log.Error(err);
                }
            }

            return exception;
        }

        /// <summary>
        /// Creates a new <see cref="CompositeExceptionProjection"/> by loading implementations with default constructors from the specified assemblies
        /// </summary>
        /// <param name="assemblies">The assemblies to scan</param>
        /// <returns>A new <see cref="CompositeExceptionProjection"/> containing projections from the specified assemblies</returns>
        public static CompositeExceptionProjection CreateFromAssemblies(IEnumerable<Assembly> assemblies)
        {
            var projections =
                from assembly in assemblies
                from type in assembly.GetTypes()
                // ignore non-public and non-instantiable abstract types
                where type.IsPublic && !type.IsAbstract
                // type implements IExceptionProjection
                where typeof(IExceptionProjection).IsAssignableFrom(type)
                // type has default parameterless ctor
                where type.GetConstructor(new Type[0]) != null
                // provide guarantee of deterministic ordering
                orderby type.FullName
                select (IExceptionProjection) Activator.CreateInstance(type);

            var projector = new CompositeExceptionProjection(projections);

            foreach (var projection in projector.Projections)
            {
                Log.Debug($"Loaded ExceptionProjection: {projection.GetType().Name}");
            }

            return projector;
        }
    }
}

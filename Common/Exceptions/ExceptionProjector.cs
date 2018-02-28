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
    public class ExceptionProjector
    {
        private readonly List<IExceptionProjection> _projections;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionProjector"/> class
        /// </summary>
        /// <param name="projections">The projections to use</param>
        public ExceptionProjector(IEnumerable<IExceptionProjection> projections)
        {
            _projections = projections.ToList();
        }

        /// <summary>
        /// Gets the projections loaded into this instance
        /// </summary>
        public IEnumerable<IExceptionProjection> Projections => _projections;

        /// <summary>
        /// Invokes the first matching projection on the specified exception
        /// </summary>
        /// <param name="exception">The exception to be projected</param>
        /// <returns>The projected exception, or the original exception if no projection matches</returns>
        public Exception Project(Exception exception)
        {
            foreach (var projection in _projections)
            {
                try
                {
                    if (projection.CanProject(exception))
                    {
                        return projection.Project(exception);
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
        /// Creates a new <see cref="ExceptionProjector"/> by loading implementations with default constructors from the specified assemblies
        /// </summary>
        /// <param name="assemblies">The assemblies to scan</param>
        /// <returns>A new <see cref="ExceptionProjector"/> containing projections from the specified assemblies</returns>
        public static ExceptionProjector CreateFromAssemblies(IEnumerable<Assembly> assemblies)
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

            var projector = new ExceptionProjector(projections);

            foreach (var projection in projector.Projections)
            {
                Log.Debug($"Loaded ExceptionProjection: {projection.GetType().Name}");
            }

            return projector;
        }
    }
}

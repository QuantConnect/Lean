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
using System.Linq.Expressions;
using System.Reflection;
using CloneExtensions;
using Fasterflect;
using QuantConnect.Logging;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides methods for creating new instances of objects
    /// </summary>
    public static class ObjectActivator
    {
        private static readonly object _lock = new object();
        private static readonly object[] _emptyObjectArray = new object[0];
        private static readonly Dictionary<Type, MethodInvoker> _cloneMethodsByType = new Dictionary<Type, MethodInvoker>();
        private static readonly Dictionary<Type, Func<object[], object>> _activatorsByType = new Dictionary<Type, Func<object[], object>>();

        static ObjectActivator()
        {
            // we can reuse the symbol instance in the clone since it's immutable
            ((HashSet<Type>) CloneFactory.KnownImmutableTypes).Add(typeof (Symbol));
            ((HashSet<Type>) CloneFactory.KnownImmutableTypes).Add(typeof (SecurityIdentifier));
        }

        /// <summary>
        /// Creates an activator using the provided parameter values and if a parameter value is not provided,
        /// then the default value for that parameter is used from the constructor info. These activators are
        /// NOT cached. If you need it to be cached, you can call <seealso cref="AddActivator"/>
        /// </summary>
        /// <param name="type">The type to create an activator for</param>
        /// <param name="parameterValues">Parameters to use when instantiating the type</param>
        /// <returns>An object activator or null if a usable constructor was found</returns>
        public static Func<object[], object> CreateCustomActivator(
            Type type,
            params Tuple<string, object>[] parameterValues
            )
        {
            var result = type.TryFindConstructor(parameterValues);
            var success = result.Item1;
            var ctor = result.Item2;
            if (!success)
            {
                // no parameterless ctor and no ctor w/ all default values
                return null;
            }

            var paramsInfo = ctor.GetParameters();

            // lambda input -- object[]
            var param = Expression.Parameter(typeof(object[]), "args");

            // ctor arguments -- param indexes containing default  values
            var argsExp = new Expression[paramsInfo.Length];

            for (var i = 0; i < paramsInfo.Length; i++)
            {
                var parameterInfo = paramsInfo[i];

                var parameterValue = parameterValues.FirstOrDefault(pv => pv.Item1 == parameterInfo.Name);

                // use the parameter value if provider, if not, use the default value
                argsExp[i] = parameterValue == null
                    ? Expression.Constant(parameterInfo.DefaultValue, parameterInfo.ParameterType)
                    : Expression.Constant(parameterValue, parameterInfo.ParameterType);
            }

            var newExp = Expression.New(ctor, argsExp);
            var lambda = Expression.Lambda(typeof(Func<object[], object>), newExp, param);

            return (Func<object[], object>)lambda.Compile();
        }

        /// <summary>
        /// Fast Object Creator from Generic Type:
        /// Modified from http://rogeralsing.com/2008/02/28/linq-expressions-creating-objects/
        /// </summary>
        /// <remarks>This assumes that the type has a parameterless, default constructor</remarks>
        /// <param name="dataType">Type of the object we wish to create</param>
        /// <returns>Method to return an instance of object</returns>
        public static Func<object[], object> GetActivator(Type dataType)
        {
            lock (_lock)
            {
                // if we already have it, just use it
                Func<object[], object> factory;
                if (_activatorsByType.TryGetValue(dataType, out factory))
                {
                    return factory;
                }

                // require a default, parameterless ctor or all optional parameters
                factory = CreateCustomActivator(dataType, parameterValues: null);

                // save it for later
                _activatorsByType.Add(dataType, factory);

                return factory;
            }
        }

        /// <summary>
        /// Clones the specified instance using reflection
        /// </summary>
        /// <param name="instanceToClone">The instance to be cloned</param>
        /// <returns>A field/property wise, non-recursive clone of the instance</returns>
        public static object Clone(object instanceToClone)
        {
            var type = instanceToClone.GetType();
            MethodInvoker func;
            if (_cloneMethodsByType.TryGetValue(type, out func))
            {
                return func(null, instanceToClone);
            }

            // public static T GetClone<T>(this T source, CloningFlags flags)
            var method = typeof (CloneFactory).GetMethods().FirstOrDefault(x => x.Name == "GetClone" && x.GetParameters().Length == 1);
            method = method.MakeGenericMethod(type);
            func = method.DelegateForCallMethod();
            _cloneMethodsByType[type] = func;
            return func(null, instanceToClone);
        }

        /// <summary>
        /// Clones the specified instance and then casts it to T before returning
        /// </summary>
        public static T Clone<T>(T instanceToClone) where T : class
        {
            var clone = Clone((object)instanceToClone) as T;
            if (clone == null)
            {
                throw new ArgumentException($"Unable to clone instance of type {instanceToClone.GetType().Name} to {typeof(T).Name}");
            }
            return clone;
        }

        /// <summary>
        /// Adds method to return an instance of object
        /// </summary>
        /// <param name="key">The key of the method to add</param>
        /// <param name="value">The value of the method to add</param>
        public static void AddActivator(Type key, Func<object[], object> value)
        {
            if (!_activatorsByType.ContainsKey(key))
            {
                _activatorsByType.Add(key, value);
            }
            else
            {
                throw new ArgumentException($"ObjectActivator.AddActivator(): a method to return an instance of {key.Name} has already been added");
            }
        }

        /// <summary>
        /// Reset the object activators
        /// </summary>
        public static void ResetActivators()
        {
            _activatorsByType.Clear();
        }
    }
}

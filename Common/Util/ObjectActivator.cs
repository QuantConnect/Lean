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
using System.Linq.Expressions;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides methods for creating new instances of objects
    /// </summary>
    public static class ObjectActivator
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<Type, Func<object[], object>> _activatorsByType = new Dictionary<Type, Func<object[], object>>(); 

        /// <summary>
        /// Fast Object Creator from Generic Type:
        /// Modified from http://rogeralsing.com/2008/02/28/linq-expressions-creating-objects/
        /// </summary>
        /// <remarks>This assumes that the type has a parameterless, default constructor</remarks>
        /// <param name="dataType">Type of the object we wish to create</param>
        /// <returns>Method to return an instance of object</returns>
        public static Func<object[], object> GetActivator(Type dataType)
        {
            Func<object[], object> factory;
            lock (_lock)
            {
                // if we already have it, just use it
                if (_activatorsByType.TryGetValue(dataType, out factory))
                {
                    return factory;
                }
            }

            var ctor = dataType.GetConstructor(new Type[] { });

            //User has forgotten to include a parameterless constructor:
            if (ctor == null) return null;

            var paramsInfo = ctor.GetParameters();

            //create a single param of type object[]
            var param = Expression.Parameter(typeof(object[]), "args");
            var argsExp = new Expression[paramsInfo.Length];

            for (var i = 0; i < paramsInfo.Length; i++)
            {
                var index = Expression.Constant(i);
                var paramType = paramsInfo[i].ParameterType;
                var paramAccessorExp = Expression.ArrayIndex(param, index);
                var paramCastExp = Expression.Convert(paramAccessorExp, paramType);
                argsExp[i] = paramCastExp;
            }

            var newExp = Expression.New(ctor, argsExp);
            var lambda = Expression.Lambda(typeof(Func<object[], object>), newExp, param);
            factory = (Func<object[], object>)lambda.Compile();

            lock (_lock)
            {
                // save it for later
                _activatorsByType.Add(dataType, factory);
            }

            return factory;
        }
    }
}

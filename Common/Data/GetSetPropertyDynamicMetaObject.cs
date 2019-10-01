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

using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace QuantConnect.Data
{
    /// <summary>
    /// Provides an implementation of <see cref="DynamicMetaObject"/> that uses get/set methods to update
    /// values in the dynamic object.
    /// </summary>
    public class GetSetPropertyDynamicMetaObject : DynamicMetaObject
    {
        private readonly MethodInfo _setPropertyMethodInfo;
        private readonly MethodInfo _getPropertyMethodInfo;

        public GetSetPropertyDynamicMetaObject(
            Expression expression,
            object value,
            MethodInfo setPropertyMethodInfo,
            MethodInfo getPropertyMethodInfo
            )
            : base(expression, BindingRestrictions.Empty, value)
        {
            _setPropertyMethodInfo = setPropertyMethodInfo;
            _getPropertyMethodInfo = getPropertyMethodInfo;
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            // we need to build up an expression tree that represents accessing our instance
            var restrictions = BindingRestrictions.GetTypeRestriction(Expression, LimitType);

            var args = new Expression[]
            {
                // this is the name of the property to set
                Expression.Constant(binder.Name),

                // this is the value
                Expression.Convert(value.Expression, typeof (object))
            };

            // set the 'this' reference
            var self = Expression.Convert(Expression, LimitType);

            var call = Expression.Call(self, _setPropertyMethodInfo, args);

            return new DynamicMetaObject(call, restrictions);
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            // we need to build up an expression tree that represents accessing our instance
            var restrictions = BindingRestrictions.GetTypeRestriction(Expression, LimitType);

            // arguments for 'call'
            var args = new Expression[]
            {
                // this is the name of the property to set
                Expression.Constant(binder.Name)
            };

            // set the 'this' reference
            var self = Expression.Convert(Expression, LimitType);

            var call = Expression.Call(self, _getPropertyMethodInfo, args);

            return new DynamicMetaObject(call, restrictions);
        }
    }
}
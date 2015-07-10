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

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides methods for constructing expressions at runtime
    /// </summary>
    public static class ExpressionBuilder
    {
        /// <summary>
        /// Constructs a selector of the form: x => x.propertyOrField where x is an instance of 'type'
        /// </summary>
        /// <param name="type">The type of the parameter in the expression</param>
        /// <param name="propertyOrField">The name of the property or field to bind to</param>
        /// <returns>A new lambda expression that represents accessing the property or field on 'type'</returns>
        public static LambdaExpression MakePropertyOrFieldSelector(Type type, string propertyOrField)
        {
            var parameter = Expression.Parameter(type);
            var property = Expression.PropertyOrField(parameter, propertyOrField);
            var lambda = Expression.Lambda(property, parameter);
            return lambda;
        }

        /// <summary>
        /// Constructs a selector of the form: x => x.propertyOrField where x is an instance of 'type'
        /// </summary>
        /// <typeparam name="T">The type of the parameter in the expression</typeparam>
        /// <typeparam name="TProperty">The type of the property or field being accessed in the expression</typeparam>
        /// <param name="propertyOrField">The name of the property or field to bind to</param>
        /// <returns>A new lambda expression that represents accessing the property or field on 'type'</returns>
        public static Expression<Func<T, TProperty>> MakePropertyOrFieldSelector<T, TProperty>(string propertyOrField)
        {
            return (Expression<Func<T, TProperty>>) MakePropertyOrFieldSelector(typeof (T), propertyOrField);
        }

        /// <summary>
        /// Converts the specified expression into an enumerable of expressions by walking the expression tree
        /// </summary>
        /// <param name="expression">The expression to enumerate</param>
        /// <returns>An enumerable containing all expressions in the input expression</returns>
        public static IEnumerable<Expression> AsEnumerable(this Expression expression)
        {
            var walker = new ExpressionWalker();
            walker.Visit(expression);
            return walker.Expressions;
        }

        /// <summary>
        /// Returns all the expressions of the specified type in the given expression tree
        /// </summary>
        /// <typeparam name="T">The type of expression to search for</typeparam>
        /// <param name="expression">The expression to search</param>
        /// <returns>All expressions of the given type in the specified expression</returns>
        public static IEnumerable<T> OfType<T>(this Expression expression)
            where T : Expression
        {
            return expression.AsEnumerable().OfType<T>();
        }

        private class ExpressionWalker : ExpressionVisitor
        {
            public readonly HashSet<Expression> Expressions = new HashSet<Expression>(); 
            public override Expression Visit(Expression node)
            {
                Expressions.Add(node);
                return base.Visit(node);
            }
        }
    }
}

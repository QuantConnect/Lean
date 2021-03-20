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
using System.Linq.Expressions;
using static QuantConnect.Util.ExpressionBuilder;

namespace QuantConnect
{
    /// <summary>
    /// Enumeration class defining binary comparisons and providing access to expressions and functions
    /// capable of evaluating a particular comparison for any type. If a particular type does not implement
    /// a binary comparison than an exception will be thrown.
    /// </summary>
    public class BinaryComparison
    {
        /// <summary>
        /// Gets the <see cref="BinaryComparison"/> equivalent of <see cref="ExpressionType.Equal"/>
        /// </summary>
        public static readonly BinaryComparison Equal = new BinaryComparison(ExpressionType.Equal);

        /// <summary>
        /// Gets the <see cref="BinaryComparison"/> equivalent of <see cref="ExpressionType.NotEqual"/>
        /// </summary>
        public static readonly BinaryComparison NotEqual = new BinaryComparison(ExpressionType.NotEqual);

        /// <summary>
        /// Gets the <see cref="BinaryComparison"/> equivalent of <see cref="ExpressionType.LessThan"/>
        /// </summary>
        public static readonly BinaryComparison LessThan = new BinaryComparison(ExpressionType.LessThan);

        /// <summary>
        /// Gets the <see cref="BinaryComparison"/> equivalent of <see cref="ExpressionType.GreaterThan"/>
        /// </summary>
        public static readonly BinaryComparison GreaterThan = new BinaryComparison(ExpressionType.GreaterThan);

        /// <summary>
        /// Gets the <see cref="BinaryComparison"/> equivalent of <see cref="ExpressionType.LessThanOrEqual"/>
        /// </summary>
        public static readonly BinaryComparison LessThanOrEqual = new BinaryComparison(ExpressionType.LessThanOrEqual);

        /// <summary>
        /// Gets the <see cref="BinaryComparison"/> equivalent of <see cref="ExpressionType.GreaterThanOrEqual"/>
        /// </summary>
        public static readonly BinaryComparison GreaterThanOrEqual = new BinaryComparison(ExpressionType.GreaterThanOrEqual);

        /// <summary>
        /// Gets the <see cref="BinaryComparison"/> matching the provided <paramref name="type"/>
        /// </summary>
        public static BinaryComparison FromExpressionType(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Equal:              return Equal;
                case ExpressionType.NotEqual:           return NotEqual;
                case ExpressionType.LessThan:           return LessThan;
                case ExpressionType.LessThanOrEqual:    return LessThanOrEqual;
                case ExpressionType.GreaterThan:        return GreaterThan;
                case ExpressionType.GreaterThanOrEqual: return GreaterThanOrEqual;
                default:
                    throw new InvalidOperationException($"The specified ExpressionType '{type}' is not a binary comparison.");
            }
        }

        /// <summary>
        /// Gets the expression type defining the binary comparison.
        /// </summary>
        public ExpressionType Type { get; }

        private BinaryComparison(ExpressionType type)
        {
            Type = type;
        }

        /// <summary>
        /// Evaluates the specified <paramref name="left"/> and <paramref name="right"/> according to this <see cref="BinaryComparison"/>
        /// </summary>
        public bool Evaluate<T>(T left, T right)
            => OfType<T>.GetFunc(Type)(left, right);

        /// <summary>
        /// Gets a function capable of performing this <see cref="BinaryComparison"/>
        /// </summary>
        public Func<T, T, bool> GetEvaluator<T>()
            => OfType<T>.GetFunc(Type);

        /// <summary>
        /// Gets an expression representing this <see cref="BinaryComparison"/>
        /// </summary>
        public Expression<Func<T, T, bool>> GetExpression<T>()
            => OfType<T>.GetExpression(Type);

        /// <summary>
        /// Flips the logic ordering of the comparison's operands. For example, <see cref="LessThan"/>
        /// is converted into <see cref="GreaterThan"/>
        /// </summary>
        public BinaryComparison FlipOperands()
        {
            switch (Type)
            {
                case ExpressionType.Equal:              return this;
                case ExpressionType.NotEqual:           return this;
                case ExpressionType.LessThan:           return GreaterThan;
                case ExpressionType.LessThanOrEqual:    return GreaterThanOrEqual;
                case ExpressionType.GreaterThan:        return LessThan;
                case ExpressionType.GreaterThanOrEqual: return LessThanOrEqual;
                default:
                    throw new Exception(
                        "The skies are falling and the oceans are rising! " +
                        "If you've made it here then this exception is the least of your worries! " +
                        $"ExpressionType: {Type}"
                    );
            }
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return Type.ToString();
        }

        /// <summary>
        /// Provides thread-safe lookups of expressions and functions for binary comparisons by type.
        /// MUCH faster than using a concurrency dictionary, for example, as it's expanded at runtime
        /// and hard-linked, no look-up is actually performed!
        /// </summary>
        private static class OfType<T>
        {
            private static readonly Expression<Func<T, T, bool>> EqualExpr = MakeBinaryComparisonLambdaOrNull(ExpressionType.Equal);
            private static readonly Expression<Func<T, T, bool>> NotEqualExpr = MakeBinaryComparisonLambdaOrNull(ExpressionType.NotEqual);
            private static readonly Expression<Func<T, T, bool>> LessThanExpr = MakeBinaryComparisonLambdaOrNull(ExpressionType.LessThan);
            private static readonly Expression<Func<T, T, bool>> LessThanOrEqualExpr = MakeBinaryComparisonLambdaOrNull(ExpressionType.LessThanOrEqual);
            private static readonly Expression<Func<T, T, bool>> GreaterThanExpr = MakeBinaryComparisonLambdaOrNull(ExpressionType.GreaterThan);
            private static readonly Expression<Func<T, T, bool>> GreaterThanOrEqualExpr = MakeBinaryComparisonLambdaOrNull(ExpressionType.GreaterThanOrEqual);

            public static Expression<Func<T, T, bool>> GetExpression(ExpressionType type)
            {
                switch (type)
                {
                    case ExpressionType.Equal:              return EqualExpr;
                    case ExpressionType.NotEqual:           return NotEqualExpr;
                    case ExpressionType.LessThan:           return LessThanExpr;
                    case ExpressionType.LessThanOrEqual:    return LessThanOrEqualExpr;
                    case ExpressionType.GreaterThan:        return GreaterThanExpr;
                    case ExpressionType.GreaterThanOrEqual: return GreaterThanOrEqualExpr;
                    default:
                        throw new InvalidOperationException($"The specified ExpressionType '{type}' is not a binary comparison.");
                }
            }

            private static readonly Func<T, T, bool> EqualFunc = EqualExpr?.Compile();
            private static readonly Func<T, T, bool> NotEqualFunc = NotEqualExpr?.Compile();
            private static readonly Func<T, T, bool> LessThanFunc = LessThanExpr?.Compile();
            private static readonly Func<T, T, bool> LessThanOrEqualFunc = LessThanOrEqualExpr?.Compile();
            private static readonly Func<T, T, bool> GreaterThanFunc = GreaterThanExpr?.Compile();
            private static readonly Func<T, T, bool> GreaterThanOrEqualFunc = GreaterThanOrEqualExpr?.Compile();

            public static Func<T, T, bool> GetFunc(ExpressionType type)
            {
                switch (type)
                {
                    case ExpressionType.Equal:              return EqualFunc;
                    case ExpressionType.NotEqual:           return NotEqualFunc;
                    case ExpressionType.LessThan:           return LessThanFunc;
                    case ExpressionType.LessThanOrEqual:    return LessThanOrEqualFunc;
                    case ExpressionType.GreaterThan:        return GreaterThanFunc;
                    case ExpressionType.GreaterThanOrEqual: return GreaterThanOrEqualFunc;
                    default:
                        throw new InvalidOperationException($"The specified ExpressionType '{type}' is not a binary comparison.");
                }
            }

            private static Expression<Func<T, T, bool>> MakeBinaryComparisonLambdaOrNull(ExpressionType type)
            {
                try
                {
                    return MakeBinaryComparisonLambda<T>(type);
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}

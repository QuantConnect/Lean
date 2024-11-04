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

using Python.Runtime;
using QuantConnect.Data.Market;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace QuantConnect.Python
{
    public partial class PandasData
    {
        private static DataTypeMember CreateDataTypeMember(MemberInfo member, DataTypeMember[] children = null)
        {
            return member switch
            {
                PropertyInfo property => new PropertyMember(property, children),
                FieldInfo field => new FieldMember(field, children),
                _ => throw new ArgumentException($"Member type {member.MemberType} is not supported")
            };
        }

        /// <summary>
        /// Represents a member of a data type, either a property or a field and it's children members in case it's a complex type.
        /// It contains logic to get the member name and the children names, taking into account the parent prefixes.
        /// </summary>
        private abstract class DataTypeMember
        {
            private static readonly StringBuilder _stringBuilder = new StringBuilder();

            private DataTypeMember _parent;
            private string _name;

            public MemberInfo Member { get; }

            public DataTypeMember[] Children { get; }

            public abstract bool IsProperty { get; }

            public abstract bool IsField { get; }

            /// <summary>
            /// The prefix to be used for the children members when a class being expanded has multiple properties/fields of the same type
            /// </summary>
            public string Prefix { get; private set; }

            public bool ShouldBeUnwrapped => Children != null && Children.Length > 0;

            /// <summary>
            /// Whether this member is Tick.LastPrice or OpenInterest.LastPrice.
            /// Saved to avoid MemberInfo comparisons in the future
            /// </summary>
            public bool IsTickLastPrice { get; }

            public bool IsTickProperty { get; }

            public DataTypeMember(MemberInfo member, DataTypeMember[] children = null)
            {
                Member = member;
                Children = children;

                IsTickLastPrice = member == _tickLastPriceMember || member == _openInterestLastPriceMember;
                IsTickProperty = IsProperty && member.DeclaringType == typeof(Tick);

                if (Children != null)
                {
                    foreach (var child in Children)
                    {
                        child._parent = this;
                    }
                }
            }

            public void SetPrefix()
            {
                Prefix = Member.Name.ToLowerInvariant();
            }

            /// <summary>
            /// Gets the member name, adding the parent prefixes if necessary.
            /// </summary>
            /// <param name="customName">If passed, it will be used instead of the <see cref="Member"/>'s name</param>
            public string GetMemberName(string customName = null)
            {
                if (ShouldBeUnwrapped)
                {
                    return string.Empty;
                }

                if (!string.IsNullOrEmpty(customName))
                {
                    return BuildMemberName(customName);
                }

                if (string.IsNullOrEmpty(_name))
                {
                    _name = BuildMemberName(GetBaseName());
                }

                return _name;
            }

            public IEnumerable<string> GetMemberNames()
            {
                return GetMemberNames(null);
            }

            public abstract object GetValue(object instance);

            public abstract Type GetMemberType();

            public override string ToString()
            {
                return $"{GetMemberType().Name} {Member.Name}";
            }

            private string BuildMemberName(string baseName)
            {
                _stringBuilder.Clear();
                while (_parent != null && _parent.ShouldBeUnwrapped)
                {
                    _stringBuilder.Insert(0, _parent.Prefix);
                    _parent = _parent._parent;
                }

                _stringBuilder.Append(baseName.ToLowerInvariant());
                return _stringBuilder.ToString();
            }

            private IEnumerable<string> GetMemberNames(string parentPrefix)
            {
                // If there are no children, return the name of the member. Else ignore the member and return the children names
                if (ShouldBeUnwrapped)
                {
                    var prefix = parentPrefix ?? string.Empty;
                    if (!string.IsNullOrEmpty(Prefix))
                    {
                        prefix += Prefix;
                    }

                    foreach (var child in Children)
                    {
                        foreach (var childName in child.GetMemberNames(prefix))
                        {
                            yield return childName;
                        }
                    }
                    yield break;
                }

                var memberName = GetBaseName();
                _name = string.IsNullOrEmpty(parentPrefix) ? memberName : $"{parentPrefix}{memberName}";
                yield return _name;
            }

            private string GetBaseName()
            {
                var baseName = Member.GetCustomAttribute<PandasColumnAttribute>()?.Name;
                if (string.IsNullOrEmpty(baseName))
                {
                    baseName = Member.Name;
                }

                return baseName.ToLowerInvariant();
            }
        }

        private class PropertyMember : DataTypeMember
        {
            private PropertyInfo _property;

            public override bool IsProperty => true;

            public override bool IsField => false;

            public PropertyMember(PropertyInfo property, DataTypeMember[] children = null)
                : base(property, children)
            {
                _property = property;
            }

            public override object GetValue(object instance)
            {
                return _property.GetValue(instance);
            }

            public override Type GetMemberType()
            {
                return _property.PropertyType;
            }
        }

        private class FieldMember : DataTypeMember
        {
            private FieldInfo _field;

            public override bool IsProperty => false;

            public override bool IsField => true;

            public FieldMember(FieldInfo field, DataTypeMember[] children = null)
                : base(field, children)
            {
                _field = field;
            }

            public override object GetValue(object instance)
            {
                return _field.GetValue(instance);
            }

            public override Type GetMemberType()
            {
                return _field.FieldType;
            }
        }
    }
}

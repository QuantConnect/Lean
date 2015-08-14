using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        /// <summary>
        /// Algorithm Parameter - Integer
        /// </summary>
        [AttributeUsage(AttributeTargets.Field)]
        public class IntParameter : Attribute
        {
            /// <summary>
            /// Minimum value for the parameter.
            /// </summary>
            public int Min { get; }
            /// <summary>
            /// Maximum value for the parameter.
            /// </summary>
            public int Max { get; }
            /// <summary>
            /// Incremental step for the parameter; useful only for optimization.
            /// </summary>
            public int Step { get; }

            /// <summary>
            /// Declare an algorithm parameter of type Integer.
            /// </summary>
            /// <param name="min">The minimum notional value for the parameter; this is only useful for optimization.</param>
            /// <param name="max">The maximum notional value for the parameter; thi is only useful for optimization.</param>
            /// <param name="step">The step increment for the parameter; useful only for optimization.</param>
            public IntParameter(int min, int max, int step = 1)
            {
                Min = min;
                Max = max;
                Step = step;
            }
        }

        /// <summary>
        /// Algorithm Parameter - Double.  Because Decimal is not a basic type and can't be accurately represented
        /// in parameter arguments.  It's a CLR restriction:
        /// http://stackoverflow.com/questions/3192833/why-decimal-is-not-a-valid-attribute-parameter-type
        /// </summary>
        [AttributeUsage(AttributeTargets.Field)]
        public class DecimalParameter : Attribute
        {
            /// <summary>
            /// Minimum value for the parameter.
            /// </summary>
            public decimal Min { get; }
            /// <summary>
            /// Maximum value for the parameter.
            /// </summary>
            public decimal Max { get; }
            /// <summary>
            /// Incremental step for the parameter; useful only for optimization.
            /// </summary>
            public decimal Step { get; }

            /// <summary>
            /// Declare an algorithm parameter of type Decimal.
            /// </summary>
            /// <param name="min">The minimum notional value for the parameter; this is only useful for optimization.</param>
            /// <param name="max">The maximum notional value for the parameter; this is only useful for optimization.</param>
            /// <param name="step">The step increment for the parameter; useful only for optimization.</param>
            public DecimalParameter(double min, double max, double step = 1.0d)
            {
                Min = Convert.ToDecimal(min);
                Max = Convert.ToDecimal(max);
                Step = Convert.ToDecimal(step);
            }
        }

        /// <summary>
        /// Algorithm Parameter - DateTime
        /// </summary>
        [AttributeUsage(AttributeTargets.Field)]
        public class DateTimeParameter : Attribute
        {
            /// <summary>
            /// Minimum value for the parameter.
            /// </summary>
            public DateTime Min { get; }
            /// <summary>
            /// Maximum value for the parameter.
            /// </summary>
            public DateTime Max { get; }
            /// <summary>
            /// Incremental step for the parameter; useful only for optimization.  Default is 1 day.
            /// </summary>
            public TimeSpan Step { get; }

            /// <summary>
            /// Declare an algorithm parameter of type DateTime.
            /// </summary>
            /// <param name="min">The minimum notional value for the parameter; will parse to a DateTime from passed string.</param>
            /// <param name="max">The maximum notional value for the parameter; will parse to a DateTime from passed string.</param>
            /// <param name="step">The step increment for the parameter; useful only for optimization.  Default is 1 day increments.</param>
            public DateTimeParameter(string min, string max, string step = "")
            {
                try
                {
                    Min = DateTime.Parse(min);
                }
                catch
                {
                    DateTime thisMin = new DateTime();
                    DateTime.TryParseExact(min, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out thisMin);
                    Min = thisMin;
                }

                try
                {
                    Max = DateTime.Parse(max);
                }
                catch
                {
                    DateTime thisMax = new DateTime();
                    DateTime.TryParseExact(min, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out thisMax);
                    Max = thisMax;
                }

                if (step == String.Empty)
                    Step = TimeSpan.FromDays(1);
                else
                    Step = TimeSpan.Parse(step);
            }
        }

        /// <summary>
        /// Algorithm Parameter - Boolean
        /// </summary>
        [AttributeUsage(AttributeTargets.Field)]
        public class BoolParameter : Attribute
        {
            /// <summary>
            /// Minimum value for the parameter.
            /// </summary>
            public bool Min { get { return false; } }
            /// <summary>
            /// Maximum value for the parameter.
            /// </summary>
            public bool Max { get { return true; } }

            /// <summary>
            /// Declare a parameter of type Boolean.
            /// </summary>
            public BoolParameter()
            {
            }
        }

        /// <summary>
        /// This helper function returns a list containing every possible permutation of parameters for an algorithm.
        /// </summary>
        /// <param name="algo">The algorithm to extract parameters from.</param>
        /// <returns></returns>
        public static List<Dictionary<string, Tuple<Type, object>>> ExtractPermutations(IAlgorithm algo)
        {
            // parameterDictionary stores our parameter possibilities.
            // name of field, data type, min, max, step
            var parameterDictionary = new Dictionary<string, Tuple<Type, object, object, object>>();

            #region Extract Parameters
            FieldInfo[] algoFields = algo.GetType().GetFields();
            foreach (FieldInfo field in algoFields)
            {
                object[] fieldAttributes = field.GetCustomAttributes(false);
                foreach (var attr in fieldAttributes)
                {
                    Tuple<Type, object, object, object> thisParameter = null;
                    foreach (PropertyInfo prop in attr.GetType().GetProperties())
                    {
                        if (thisParameter == null)
                            thisParameter = new Tuple<Type, object, object, object>(prop.PropertyType, null, null, null);

                        if (prop.Name == "Min")
                            thisParameter = new Tuple<Type, object, object, object>(
                                thisParameter.Item1, prop.GetValue(attr),
                                thisParameter.Item3, thisParameter.Item4);
                        else if (prop.Name == "Max")
                            thisParameter = new Tuple<Type, object, object, object>(
                                thisParameter.Item1, thisParameter.Item2,
                                prop.GetValue(attr), thisParameter.Item4);
                        else if (prop.Name == "Step")
                            thisParameter = new Tuple<Type, object, object, object>(
                                thisParameter.Item1, thisParameter.Item2,
                                thisParameter.Item3, prop.GetValue(attr));
                    }

                    if (thisParameter != null)
                    {
                        QuantConnect.Logging.Log.Trace(String.Format("-- Parameter: {0}|{1}, Min: {2}  Max: {3}  Step: {4}",
                            field.Name, thisParameter.Item1, thisParameter.Item2, thisParameter.Item3, thisParameter.Item4));

                        parameterDictionary.Add(field.Name, thisParameter);
                    }
                }
            }
            #endregion

            var permList = new List<Dictionary<string, Tuple<Type, object>>>();
            #region Generate Permutations
            foreach (var entry in parameterDictionary)
            {
                var newPerms = new List<Dictionary<string, Tuple<Type, object>>>();

                // can't figure out how to dynamically cast at-runtime for min/max/step,
                // so we're forced to use typeof-based IF-THENs to go from min to max properly.
                if (entry.Value.Item1 == typeof(bool))
                {
                    int min = Convert.ToInt32(false);
                    int max = Convert.ToInt32(true);
                    int step = 1;

                    // for each possible value of this parameter
                    for (var x = min; x <= max; x += step)
                    {
                        // if an empty list, just create the new permutation for x
                        if (permList.Count == 0)
                        {
                            var thisPerm = new Dictionary<string, Tuple<Type, object>>();
                            thisPerm.Add(entry.Key, new Tuple<Type, object>(entry.Value.Item1, x));
                            newPerms.Add(thisPerm);
                        }
                        // otherwise create a new permutation on this x for every prior permutation
                        else
                        {
                            foreach (var existingPerm in permList)
                            {
                                var thisPerm = new Dictionary<string, Tuple<Type, object>>(existingPerm);
                                thisPerm.Add(entry.Key, new Tuple<Type, object>(entry.Value.Item1, x));
                                newPerms.Add(thisPerm);
                            }
                        }
                    }
                }
                else if (entry.Value.Item1 == typeof(DateTime))
                {
                    DateTime min = (DateTime)entry.Value.Item2;
                    DateTime max = (DateTime)entry.Value.Item3;
                    TimeSpan step = (TimeSpan)entry.Value.Item4;

                    for (var x = min; x <= max; x += step)
                    {
                        if (permList.Count == 0)
                        {
                            var thisPerm = new Dictionary<string, Tuple<Type, object>>();
                            thisPerm.Add(entry.Key, new Tuple<Type, object>(entry.Value.Item1, x));
                            newPerms.Add(thisPerm);
                        }
                        // otherwise create a new permutation on this x for every prior permutation
                        else
                        {
                            foreach (var existingPerm in permList)
                            {
                                var thisPerm = new Dictionary<string, Tuple<Type, object>>(existingPerm);
                                thisPerm.Add(entry.Key, new Tuple<Type, object>(entry.Value.Item1, x));
                                newPerms.Add(thisPerm);
                            }
                        }
                    }
                }
                else if (entry.Value.Item1 == typeof(int))
                {
                    int min = (int)entry.Value.Item2;
                    int max = (int)entry.Value.Item3;
                    int step = (int)entry.Value.Item4;

                    for (var x = min; x <= max; x += step)
                    {
                        if (permList.Count == 0)
                        {
                            var thisPerm = new Dictionary<string, Tuple<Type, object>>();
                            thisPerm.Add(entry.Key, new Tuple<Type, object>(entry.Value.Item1, x));
                            newPerms.Add(thisPerm);
                        }
                        // otherwise create a new permutation on this x for every prior permutation
                        else
                        {
                            foreach (var existingPerm in permList)
                            {
                                var thisPerm = new Dictionary<string, Tuple<Type, object>>(existingPerm);
                                thisPerm[entry.Key] = new Tuple<Type, object>(entry.Value.Item1, x);
                                newPerms.Add(thisPerm);
                            }
                        }
                    }
                }
                else if (entry.Value.Item1 == typeof(decimal))
                {
                    decimal min = (decimal)entry.Value.Item2;
                    decimal max = (decimal)entry.Value.Item3;
                    decimal step = (decimal)entry.Value.Item4;

                    for (var x = min; x <= max; x += step)
                    {
                        if (permList.Count == 0)
                        {
                            var thisPerm = new Dictionary<string, Tuple<Type, object>>();
                            thisPerm.Add(entry.Key, new Tuple<Type, object>(entry.Value.Item1, x));
                            newPerms.Add(thisPerm);
                        }
                        // otherwise create a new permutation on this x for every prior permutation
                        else
                        {
                            foreach (var existingPerm in permList)
                            {
                                var thisPerm = new Dictionary<string, Tuple<Type, object>>(existingPerm);
                                thisPerm.Add(entry.Key, new Tuple<Type, object>(entry.Value.Item1, x));
                                newPerms.Add(thisPerm);
                            }
                        }
                    }
                }

                // replace the old permutations list now that we've generated the possibilities for
                // every combination of this current parameter.
                permList = newPerms;
            }
            #endregion

            return permList;
        }

        /// <summary>
        /// This helper function assigns a set of parameters to an algorithm.
        /// </summary>
        /// <param name="parameters">A dictionary containing the parameter names and values that will be assigned.</param>
        /// <param name="algorithm">A reference to the algorithm.  This function will modify the algorithm's values!</param>
        public static void AssignParameters(Dictionary<string, Tuple<Type, object>> parameters, IAlgorithm algorithm)
        {
            // each permutation is a dictionary (by parameter name), containing tuples indicating:
            //  Item1: param data type
            //  Item2: minimum parameter value
            //  Item3: maximum parameter value
            //  Item4: increment parameter value
            Type algoType = algorithm.GetType();
            foreach (var parameter in parameters)
            {
                //https://msdn.microsoft.com/en-us/library/6z33zd7h%28v=vs.110%29.aspx
                FieldInfo algoField = algoType.GetField(parameter.Key);

                if(parameter.Value.Item1 == typeof(bool))
                    algoField.SetValue(algorithm, Convert.ToBoolean(parameter.Value.Item2));
                else
                    algoField.SetValue(algorithm, parameter.Value.Item2);
            }
        }
    }
}

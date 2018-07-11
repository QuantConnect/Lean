using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Statistics;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Optimization methods for portfolios
    /// </summary>
    public class Optimization
    {
        /// <summary>
        /// Fill vector with specified values
        /// </summary>
        /// <typeparam name="T">Vector element type</typeparam>
        /// <param name="size">Vector size</param>
        /// <param name="value">Fill value</param>
        /// <returns></returns>
        public static T[] FillVector<T>(int size, T value)
        {
            var vec = new T[size];
            for (int i = 0; i < size; i++)
            {
                vec[i] = value;
            }
            return vec;
        }

        public static void GetCovariance(IEnumerable<IEnumerable<double>> returns, out double[,] cov, out double[] mean)
        {
            mean = returns.Select(r => Measures.Mean(r.ToArray())).ToArray();
            var data = Accord.Math.Matrix.Create(returns.Select(r => r.ToArray()).ToArray());
            cov = Measures.Covariance(Accord.Math.Matrix.Transpose(data), mean);
            return;
        }

        /// <summary>
        /// Perform mean variance optimization given the returns
        /// </summary>
        /// <param name="symbols">Collection of symbols</param>
        /// <param name="returns">Collections of returns by symbols</param>
        /// <param name="minimumWeight">Lower weight bound</param>
        /// <param name="maximumWeight">Upper weight bound</param>
        /// <param name="mu">Target return value</param>
        /// <returns></returns>
        public static Dictionary<Symbol, double> MinimumVariance(
            Dictionary<Symbol, List<double>> returns,
            double minimumWeight,
            double maximumWeight,
            double mu)
        {

            var symbols = returns.Keys;

            double[] mean;
            double[,] cov;
            GetCovariance(symbols.Select(sym => returns[sym]), out cov, out mean);
            var size = mean.Length;
            
            // initial point
            var x0 = FillVector(size, 1.0 / size);
            // lower boundaries
            var bndl = FillVector(size, minimumWeight);
            // upper boundaries
            var bndu = FillVector(size, maximumWeight);
            // scale
            var s = FillVector(size, 1.0);
            // covariance
            double[,] a = cov;
            //double[] b = new double[size];

            alglib.minqpstate state;
            alglib.minqpreport rep;

            // create solver, set quadratic/linear terms
            alglib.minqpcreate(size, out state);
            alglib.minqpsetquadraticterm(state, a);
            //alglib.minqpsetlinearterm(state, b);
            alglib.minqpsetstartingpoint(state, x0);
            // set scale
            alglib.minqpsetscale(state, s);
            // upper and lower bounds
            alglib.minqpsetbc(state, bndl, bndu);

            // c1: sum(x) = 1
            var c1 = FillVector(mean.Length + 1, 1.0);
            // c2: R^T * x = mu
            var c2 = FillVector(mean.Length + 1, mu); 
            mean.CopyTo(c2, 0);

            // set constraints
            var C = Accord.Math.Matrix.Create(new double[][] { c1, c2 });
            int[] ct = new int[] { 0, 0 };
            alglib.minqpsetlc(state, C, ct);

            // Solve problem
            //if (size > 50)
            //{
            //    alglib.minqpsetalgodenseaul(state, 1.0e-9, 1.0e+4, 5);
            //}
            //else
            {
                alglib.minqpsetalgobleic(state, 0.0, 0.0, 0.0, 0);
            }
            alglib.minqpoptimize(state);

            // Get results
            double[] x;
            alglib.minqpresults(state, out x, out rep);

            var weights = new Dictionary<Symbol, double>();
            // Solved succesfully
            if (rep.terminationtype > 0)
            {
                foreach (var kv in symbols.Zip(x, (sym, w) => Tuple.Create(sym, w)))
                {
                    weights[kv.Item1] = kv.Item2;
                }
            }
            return weights;
        }
    }
}

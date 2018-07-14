using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Math;
using Accord.Statistics;

namespace QuantConnect.Algorithm.Framework.Portfolio.Optimization
{
    public enum ConstraintType { Equal = 0, Less = -1, More = 1 };

    /// <summary>
    /// Mean-Variance Portfolio Optimization
    /// </summary>
    public class MeanVariancePortfolio
    {
        public double[,] _cov;
        public double[] _x0;
        public double[] _scale;
        public List<double[]> _constraints;
        public List<int> _constraintTypes;
        public double _lower;
        public double _upper;

        public int Size => _cov.GetLength(0);

        public MeanVariancePortfolio(double[,] cov)
        {
            _constraints = new List<double[]>();
            _constraintTypes = new List<int>();
            _cov = cov;
            _x0 = null;
            _scale = null;
            _lower = Double.NaN;
            _upper = Double.NaN;
        }

        public void SetInitialValue(double[] init = null)
        {
            if (init == null || init.Length != Size)
            {
                if (_x0 == null)
                {
                    _x0 = Vector.Create(Size, 1.0 / Size);
                }
            }
            else
            {
                _x0 = init;
            }
        }

        public void SetScale(double[] scale = null)
        {
            if (scale == null || scale.Length != Size)
            {
                if (_scale == null)
                {
                    _scale = Vector.Create(Size, 1.0);
                }
            }
            else
            {
                _scale = scale;
            }
        }

        public void SetBounds(double lower, double upper)
        {
            _lower = lower;
            _upper = upper;
        }

        public void SetConstraints(double[] lc, ConstraintType ct, double rc)
        {
            if (lc.Length != Size)
            {
                throw new ArgumentOutOfRangeException(String.Format("Incorrect number of constraints: {0}", lc));
            }
            var c = Vector.Create(Size + 1, rc);
            lc.CopyTo(c, 0);
            _constraints.Add(c);
            _constraintTypes.Add((int)ct);
        }

        /// <summary>
        /// Solve QP problem
        /// </summary>
        /// <param name="x">Portfolio weights</param>
        /// <returns></returns>
        protected virtual int Optimize(out double[] x)
        {
            alglib.minqpstate state;

            SetInitialValue();

            // set quadratic/linear terms
            alglib.minqpcreate(Size, out state);
            alglib.minqpsetquadraticterm(state, _cov.Multiply(2.0));
            //alglib.minqpsetlinearterm(state, b);

            alglib.minqpsetstartingpoint(state, _x0);            
            alglib.minqpsetbc(state, Vector.Create(Size, _lower), Vector.Create(Size, _upper));

            // wire all constraints            
            var C = Matrix.Create(_constraints.ToArray());
            alglib.minqpsetlc(state, C, _constraintTypes.ToArray());

            int ret = 0;
            x = Vector.Create(Size, 0.0);
            alglib.minqpreport rep;
            bool autoscale = true;
            while (autoscale)
            {
                //if (autoscale) // For version 3.14
                //{
                //    alglib.minqpsetscaleautodiag(_state);
                //}
                //else
                {
                    SetScale();
                    alglib.minqpsetscale(state, _scale);
                }
                autoscale = false;

                // Solve problem                
                //alglib.minqpsetalgodenseaul(_state, 0, 1.0e+4, 0); // For version 3.14
                alglib.minqpsetalgobleic(state, 0.0, 0.0, 0.0, 0);
                alglib.minqpoptimize(state);

                // Get results
                double[] res;
                alglib.minqpresults(state, out res, out rep);
                ret = rep.terminationtype;
                x = res;

                // Restart with different scale
                if (ret == -9)
                    autoscale = true;
            }
            return ret;
        }

        /// <summary>
        /// Perform mean variance optimization given the returns
        /// </summary>
        /// <param name="W">Portfolio weights</param>       
        /// <param name="minimumWeight">Lower weight bound</param>
        /// <param name="maximumWeight">Upper weight bound</param>
        /// <param name="expectedReturns">Vector of expected returns</param>
        /// <param name="targetReturn">Target return value</param>
        /// <returns>error code</returns>
        public virtual int Optimize(out double[] W, double minimumWeight, double maximumWeight, double[] expectedReturns, double targetReturn = 0.0)
        {
            SetBounds(minimumWeight, maximumWeight);

            // sum(x) = 1
            SetConstraints(Vector.Create(Size, 1.0), ConstraintType.Equal, 1.0);
            // mu^T x = R  or mu^T x >= 0
            SetConstraints(expectedReturns, targetReturn == 0.0 ? ConstraintType.More : ConstraintType.Equal, targetReturn);

            return Optimize(out W);
        }
    }

    /// <summary>
    /// Maximum Sharpe Ratio Portfolio Optimization
    /// </summary>
    public class MaxSharpeRatioPortfolio : MeanVariancePortfolio
    {
        double _riskFreeRate;
        double[] _expectedReturns;

        public MaxSharpeRatioPortfolio(double[,] cov, double riskFreeRate) : base(cov)
        {
            _riskFreeRate = riskFreeRate;
        }

        public override int Optimize(out double[] x, double minimumWeight, double maximumWeight, double[] expectedReturns, double targetReturn = 0.0)
        {
            _expectedReturns = expectedReturns;

            SetBounds(minimumWeight, maximumWeight);
            SetConstraints(Vector.Create(Size, 1.0), ConstraintType.Equal, 1.0);

            var ret = Optimize(out x); // use NLP solver

            return ret;
        }

        public static void SharpeRatio(double[] x, ref double func, object obj)
        {
            var opt = (MaxSharpeRatioPortfolio)obj;
            var annual_return = opt._expectedReturns.Dot(x);
            var annual_volatility = Math.Sqrt(x.Dot(opt._cov).Dot(x));
            func = (annual_return - opt._riskFreeRate) / annual_volatility;
            func = Double.IsInfinity(func) || Double.IsNaN(func)  ? 1.0E+300 : -func;
        }

        protected override int Optimize(out double[] x)
        {
            alglib.minbleicstate state;

            SetInitialValue();

            //
            // This variable contains differentiation step
            //
            double diffstep = 1.0e-6;
            alglib.minbleiccreatef(_x0, diffstep, out state);
            alglib.minbleicsetbc(state, Vector.Create(Size, _lower), Vector.Create(Size, _upper));

            // wire all constraints            
            var C = Matrix.Create(_constraints.ToArray());
            alglib.minbleicsetlc(state, C, _constraintTypes.ToArray());

            // Stopping conditions for the optimizer. 
            alglib.minbleicsetcond(state, 0, 0, 0, 0);
            alglib.minbleicoptimize(state, SharpeRatio, null, this);

            alglib.minbleicreport rep;
            alglib.minbleicresults(state, out x, out rep);
            return rep.terminationtype;
        }
    }

}

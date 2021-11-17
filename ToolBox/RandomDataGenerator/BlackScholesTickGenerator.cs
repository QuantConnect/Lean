using System;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// https://cseweb.ucsd.edu/~goguen/courses/130/SayBlackScholes.html
    /// </summary>
    public class BlackScholesTickGenerator : TickGenerator
    {
        public BlackScholesTickGenerator(RandomDataGeneratorSettings settings)
            : base(settings)
        {
        }

        public BlackScholesTickGenerator(RandomDataGeneratorSettings settings, IRandomValueGenerator random)
            : base(settings, random)
        {
        }

        /// <summary>
        /// The Black and Scholes (1973) Stock option formula
        /// C# Implementation
        /// uses the C# Math.PI field rather than a constant as in the C++ implementaion
        /// the value of Pi is 3.14159265358979323846
        /// </summary>
        /// <param name="CallPutFlag"></param>
        /// <param name="S">Stock price</param>
        /// <param name="X">Strike price</param>
        /// <param name="T">Years to maturity</param>
        /// <param name="r">Risk-free rate</param>
        /// <param name="v">Volatility</param>
        /// <returns></returns>
        public double BlackScholes(OptionRight CallPutFlag, double S, double X,
            double T, double r, double v)
        {
            double d1 = (Math.Log(S / X) + (r + v * v / 2.0) * T) / (v * Math.Sqrt(T));
            double d2 = d1 - v * Math.Sqrt(T);
            double dBlackScholes;
            if (CallPutFlag == OptionRight.Call)
            {
                dBlackScholes = S * CND(d1) - X * Math.Exp(-r * T) * CND(d2);
            }
            else
            {
                dBlackScholes = X * Math.Exp(-r * T) * CND(-d2) - S * CND(-d1);
            }
            return dBlackScholes;
        }

        /// <summary>
        /// cumulative normal distribution
        /// </summary>
        /// <param name="X"></param>
        /// <returns></returns>
        public double CND(double X)
        {
            const double a1 = 0.31938153;
            const double a2 = -0.356563782;
            const double a3 = 1.781477937;
            const double a4 = -1.821255978;
            const double a5 = 1.330274429;
            double L = Math.Abs(X);
            double K = 1.0 / (1.0 + 0.2316419 * L);
            double dCND = 1.0 - 1.0 / Math.Sqrt(2 * Convert.ToDouble(Math.PI.ToString())) *
                Math.Exp(-L * L / 2.0) * (a1 * K + a2 * K * K + a3 * Math.Pow(K, 3.0) + a4 * Math.Pow(K, 4.0) + a5 * Math.Pow(K, 5.0));

            if (X < 0)
            {
                return 1.0 - dCND;
            }

            return dCND;
        }
    }
}

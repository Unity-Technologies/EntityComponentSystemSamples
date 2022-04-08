using System;
using System.Collections.Generic;
using System.Linq;

namespace Editor.PerformanceTestViewer
{
    static class StatisticsUtility
    {
        static double NormP(double z)
        {
            // input = z-value (-inf to +inf)
            // output = p under Standard Normal curve from -inf to z
            // e.g., if z = 0.0, function returns 0.5000
            // ACM Algorithm #209
            if (z == 0.0)
                return 0.5;
            var y = Math.Abs(z) / 2; // 209 scratch variable
            double p; // result. called 'z' in 209
            if (y >= 3.0)
            {
                p = 1.0;
            }
            else if (y < 1.0)
            {
                var w = y * y; // 209 scratch variable
                p = ((((((((0.000124818987 * w
                            - 0.001075204047) * w + 0.005198775019) * w
                          - 0.019198292004) * w + 0.059054035642) * w
                        - 0.151968751364) * w + 0.319152932694) * w
                      - 0.531923007300) * w + 0.797884560593) * y * 2.0;
            }
            else
            {
                y = y - 2.0;
                p = (((((((((((((-0.000045255659 * y
                                 + 0.000152529290) * y - 0.000019538132) * y
                               - 0.000676904986) * y + 0.001390604284) * y
                             - 0.000794620820) * y - 0.002034254874) * y
                           + 0.006549791214) * y - 0.010557625006) * y
                         + 0.011630447319) * y - 0.009279453341) * y
                       + 0.005353579108) * y - 0.002141268741) * y
                     + 0.000535310849) * y + 0.999936657524;
            }

            if (z < 0.0)
                p = -p;
            return (p + 1.0) / 2;
        }

        /*
         *     Compute the quantile function for the normal distribution.
         *
         *     For small to moderate probabilities, algorithm referenced
         *     below is used to obtain an initial approximation which is
         *     polished with a final Newton step.
         *
         *     For very large arguments, an algorithm of Wichura is used.
         *
         *  REFERENCE
         *
         *     Beasley, J. D. and S. G. Springer (1977).
         *     Algorithm AS 111: The percentage points of the normal distribution,
         *     Applied Statistics, 26, 118-121.
         *
         *      Wichura, M.J. (1988).
         *      Algorithm AS 241: The Percentage Points of the Normal Distribution.
         *      Applied Statistics, 37, 477-484.
         */
        static double NormQ(double p, double mu, double sigma)
        {
            // from https://gist.github.com/kmpm/1211922/6b7fcd0155b23c3dc71e6f4969f2c48785371292
            // see https://stackedboxes.org/2017/05/01/acklams-normal-quantile-function/
            if (sigma < 0)
                throw new Exception("The standard deviation sigma must be positive");

            if (p <= 0)
                return -double.NegativeInfinity;
            if (p >= 1)
                return double.PositiveInfinity;
            if (sigma == 0)
                return mu;

            double val;

            var q = p - 0.5;

            /*-- use AS 241 --- */
            /* double ppnd16_(double *p, long *ifault)*/
            /*      ALGORITHM AS241  APPL. STATIST. (1988) VOL. 37, NO. 3
                    Produces the normal deviate Z corresponding to a given lower
                    tail area of P; Z is accurate to about 1 part in 10**16.
            */
            if (Math.Abs(q) <= .425)
            {
                /* 0.075 <= p <= 0.925 */
                double r = .180625 - q * q;
                val =
                    q * (((((((r * 2509.0809287301226727 +
                               33430.575583588128105) * r + 67265.770927008700853) * r +
                             45921.953931549871457) * r + 13731.693765509461125) * r +
                           1971.5909503065514427) * r + 133.14166789178437745) * r +
                         3.387132872796366608)
                    / (((((((r * 5226.495278852854561 +
                             28729.085735721942674) * r + 39307.89580009271061) * r +
                           21213.794301586595867) * r + 5394.1960214247511077) * r +
                         687.1870074920579083) * r + 42.313330701600911252) * r + 1);
            }
            else
            {
                /* closer than 0.075 from {0,1} boundary */

                /* r = min(p, 1-p) < 0.075 */
                double r;
                if (q > 0)
                    r = 1 - p;
                else
                    r = p;

                r = Math.Sqrt(-Math.Log(r));
                /* r = sqrt(-log(r))  <==>  min(p, 1-p) = exp( - r^2 ) */

                if (r <= 5)
                {
                    /* <==> min(p,1-p) >= exp(-25) ~= 1.3888e-11 */
                    r += -1.6;
                    val = (((((((r * 7.7454501427834140764e-4 +
                                 .0227238449892691845833) * r + .24178072517745061177) *
                                  r + 1.27045825245236838258) * r +
                              3.64784832476320460504) * r + 5.7694972214606914055) *
                               r + 4.6303378461565452959) * r +
                           1.42343711074968357734)
                          / (((((((r *
                                         1.05075007164441684324e-9 + 5.475938084995344946e-4) *
                                     r + .0151986665636164571966) * r +
                                 .14810397642748007459) * r + .68976733498510000455) *
                                  r + 1.6763848301838038494) * r +
                              2.05319162663775882187) * r + 1);
                }
                else
                {
                    /* very close to  0 or 1 */
                    r += -5;
                    val = (((((((r * 2.01033439929228813265e-7 +
                                 2.71155556874348757815e-5) * r +
                                .0012426609473880784386) * r + .026532189526576123093) *
                                 r + .29656057182850489123) * r +
                             1.7848265399172913358) * r + 5.4637849111641143699) *
                              r + 6.6579046435011037772)
                          / (((((((r *
                                         2.04426310338993978564e-15 + 1.4215117583164458887e-7) *
                                     r + 1.8463183175100546818e-5) * r +
                                 7.868691311456132591e-4) * r + .0148753612908506148525)
                                  * r + .13692988092273580531) * r +
                              .59983220655588793769) * r + 1);
                }

                if (q < 0.0)
                {
                    val = -val;
                }
            }

            return mu + sigma * val;
        }

        static double StudentP(double t, double df)
        {
            // for large integer df or double df
            // adapted from ACM algorithm 395
            // returns 2-tail p-value
            double n = df; // to sync with ACM parameter name
            t = t * t;
            var y = t / n;
            var b = y + 1.0;
            if (y > 1.0E-6) y = Math.Log(b);
            var a = n - 0.5;
            b = 48.0 * a * a;
            y = a * y;
            y = (((((-0.4 * y - 3.3) * y - 24.0) * y - 85.5) /
                    (0.8 * y * y + 100.0 + b) + y + 3.0) / b + 1.0) *
                Math.Sqrt(y);
            return 2.0 * NormP(-y); // ACM algorithm 209
        }

        // Critical T-values are so annoying to calculate, so lets just tabulate them for 95% confidence.
        private static readonly ValueTuple<int, double>[] CriticalTValueTwoSided95 =
        {
            (1, 12.71),
            (2, 4.303),
            (3, 3.182),
            (4, 2.776),
            (5, 2.571),
            (6, 2.447),
            (7, 2.365),
            (8, 2.306),
            (9, 2.262),
            (10, 2.228),
            (11, 2.201),
            (12, 2.179),
            (13, 2.160),
            (14, 2.145),
            (15, 2.131),
            (16, 2.120),
            (17, 2.110),
            (18, 2.101),
            (19, 2.093),
            (20, 2.086),
            (21, 2.080),
            (22, 2.074),
            (23, 2.069),
            (24, 2.064),
            (25, 2.060),
            (26, 2.056),
            (27, 2.052),
            (28, 2.048),
            (29, 2.045),
            (30, 2.042),
            (35, 2.03),
            (40, 2.021),
            (45, 2.01),
            (50, 2.01),
            (60, 2.0),
            (80, 1.990),
            (100, 1.984),
            (120, 1.98),
            (1000, 1.962),
            // converges towards the Z-value:
            (int.MaxValue, 1.96),
        };

        private static readonly ValueTuple<int, double>[] CriticalTValueTwoSided99 =
        {
            (1, 63.657),
            (2, 9.925),
            (3, 5.841),
            (4, 4.602),
            (5, 4.032),
            (6, 3.707),
            (7, 3.500),
            (8, 3.355),
            (9, 3.250),
            (10, 3.169),
            (11, 3.106),
            (12, 3.055),
            (13, 3.012),
            (14, 2.977),
            (15, 2.947),
            (16, 2.921),
            (17, 2.898),
            (18, 2.878),
            (19, 2.861),
            (20, 2.845),
            (21, 2.831),
            (22, 2.818),
            (23, 2.807),
            (24, 2.797),
            (25, 2.787),
            (26, 2.779),
            (27, 2.771),
            (28, 2.763),
            (29, 2.756),
            (30, 2.750),
            (35, 2.724),
            (40, 2.704),
            (45, 2.690),
            (50, 2.678),
            (60, 2.660),
            (80, 2.639),
            (100, 2.626),
            (120, 2.617),
            (1000, 2.58),
            // converges towards the Z-value:
            (int.MaxValue, 2.58),
        };

        static double FindCritialTValue(double df, ValueTuple<int, double>[] table)
        {
            int idx = 0;
            for (; idx < table.Length; idx++)
            {
                if (table[idx].Item1 > df)
                    break;
            }

            // unlikely, but...
            if (idx >= table.Length - 1)
                return table[table.Length - 1].Item2;

            // just err a little bit on the conservative side and take whatever is just before us
            return table[idx - 1].Item2;
        }

        /// <summary>
        /// Returns a conservative estimation of the critical t value for a 95 percent confidence interval.
        /// </summary>
        /// <param name="df"></param>
        /// <returns></returns>
        static double FindCriticalTValueTwoSided95(double df) => FindCritialTValue(df, CriticalTValueTwoSided95);
        static double FindCriticalTValueTwoSided99(double df) => FindCritialTValue(df, CriticalTValueTwoSided99);

        public struct WelchTestResult
        {
            public double p;
            public double MeanX;
            public double MeanY;
            public double IntervalCenter;
            public double IntervalHalfWidth;
            public double IntervalLower => IntervalCenter - IntervalHalfWidth;
            public double IntervalUpper => IntervalCenter + IntervalHalfWidth;

            public override string ToString() =>
                $"{MeanX} - {MeanY} = {MeanX - MeanY}, [{IntervalLower}, {IntervalUpper}], p = {p}";
        }

        public enum ConfidenceLevel {
            Alpha5, Alpha1
        }

        public static WelchTestResult WelchTTest(IList<double> x, IList<double> y, ConfidenceLevel c)
        {
            // Following the implementation here:
            // https://docs.microsoft.com/en-us/archive/msdn-magazine/2015/november/test-run-the-t-test-using-csharp
            var sumX = x.Sum();
            var sumY = y.Sum();
            int nX = x.Count;
            int nY = y.Count;
            double meanX = sumX / nX;
            double meanY = sumY / nY;
            double top = meanX - meanY;
            double varX;
            double varY;
            {
                double sumXminusMeanSquared = 0.0; // Calculate variances
                double sumYminusMeanSquared = 0.0;
                for (int i = 0; i < nX; ++i)
                    sumXminusMeanSquared += (x[i] - meanX) * (x[i] - meanX);
                for (int i = 0; i < nY; ++i)
                    sumYminusMeanSquared += (y[i] - meanY) * (y[i] - meanY);
                varX = sumXminusMeanSquared / (nX - 1);
                varY = sumYminusMeanSquared / (nY - 1);
            }
            double bot = Math.Sqrt(varX / nX + varY / nY);
            double num = (varX / nX + varY / nY) * (varX / nX + varY / nY);
            double denomLeft = varX / nX * (varX / nX) / (nX - 1);
            double denomRight = varY / nY * (varY / nY) / (nY - 1);
            double denom = denomLeft + denomRight;
            double df = num / denom;
            double t = top / bot;
            double p = StudentP(t, df); // Cumulative two-tail density

            double tCrit = c == ConfidenceLevel.Alpha1
                ? FindCriticalTValueTwoSided99(df)
                : FindCriticalTValueTwoSided95(df);
            return new WelchTestResult {
                p = p,
                MeanX = meanX,
                MeanY = meanY,
                IntervalCenter = top,
                IntervalHalfWidth = Math.Abs(bot) * tCrit
            };
        }

        public static void RemoveTopOutliers(List<double> values, float p = 0.05f)
        {
            values.Sort();
            int toRemove = (int) (values.Count * p);
            if (toRemove > 0)
                values.RemoveRange(values.Count - toRemove, toRemove);
        }
    }
}

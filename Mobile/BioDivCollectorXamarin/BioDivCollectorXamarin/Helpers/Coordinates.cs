using System;
using System.Collections.Generic;
using System.Text;

namespace BioDivCollectorXamarin.Helpers
{
    public class Coordinates
    {

        public Coordinates() { }

        /// <summary>
        /// Rough offline conversion between WGS84 and Swiss coordinates
        /// </summary>
        /// <param name="wgs84_Lat"></param>
        /// <param name="wgs84_Long"></param>
        /// <returns></returns>
        public static double[] TransformCoordFromWGSToSwissOffline(double wgs84_Lat, double wgs84_Long)
        {
            var phi_p = (wgs84_Lat * 3600 - 169028.66) / 10000;
            var lambda_p = (wgs84_Long * 3600 - 26782.5) / 10000;

            double ch1903East = 2600072.37
            + 211455.93 * lambda_p
            - 10938.51 * lambda_p * phi_p
            - 0.36 * lambda_p * (phi_p * phi_p)
            - 44.54 * (lambda_p * lambda_p * lambda_p);
            var east = Convert.ToInt32(Math.Round(ch1903East));

            double ch1903North = 1200147.07
            + 308807.95 * phi_p
            + 3745.25 * (lambda_p * lambda_p)
            + 76.63 * (phi_p * phi_p)
            - 194.56 * (lambda_p * lambda_p) * phi_p
            + 119.79 * (phi_p * phi_p * phi_p);
            var north = Convert.ToInt32(Math.Round(ch1903North));
            return new double[] { east, north };
        }
    }
}

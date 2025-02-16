using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib
{
    internal class MathsF
    {
        /// <summary>
        /// Scale x to the range of yMin to yMax
        /// </summary>
        /// <param name="x"></param>
        /// <param name="xMin"></param>
        /// <param name="xMax"></param>
        /// <param name="yMin"></param>
        /// <param name="yMax"></param>
        /// <returns></returns>
        public static float MapRange(float x, float xMin, float xMax, float yMin, float yMax)
        {
            return yMin + (yMax - yMin) * (x - xMin) / (xMax - xMin);
        }

        /// <summary>
        /// Scale x to the range of yMin to yMax, but ensure it stays within the range of yMin and yMax
        /// </summary>
        /// <param name="x"></param>
        /// <param name="xMin"></param>
        /// <param name="xMax"></param>
        /// <param name="yMin"></param>
        /// <param name="yMax"></param>
        /// <returns></returns>
        public static float EnsureMapRange(float x, float xMin, float xMax, float yMin, float yMax)
        {
            return MathF.Max(MathF.Min(MapRange(x, xMin, xMax, yMin, yMax), MathF.Max(yMin, yMax)), MathF.Min(yMin, yMax));
        }

        /// <summary>
        /// Copy the sign of the second parameter to the first
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static float CopySign(float x, float y)
        {
            return MathF.Abs(x) * (y < 0f ? -1f : 1f);
        }

        /// <summary>
        /// Limits the actual pitch and roll values to a certain range
        /// </summary>
        /// <param name="x"></param>
        /// <param name="actualMin">expected minimum input</param>
        /// <param name="actualMax">expected maximum input</param>
        /// <param name="fwMax">maximum forward degrees (should probably be negative)</param>
        /// <param name="bkMax">maximum backward degrees</param>
        /// <returns></returns>
        public static float ScalePitchRoll(float x, float actualMin, float actualMax, float fwMax, float bkMax)
        {
            
            if (x < 0)
            {
                return EnsureMapRange(x, 0, actualMin, 0, -MathF.Abs(fwMax));
            }
            else
            {
                return EnsureMapRange(x, 0, actualMax, 0, MathF.Abs(bkMax));
            }

                
        }
    }
}

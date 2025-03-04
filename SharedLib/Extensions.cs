using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib
{
    internal static class Extensions
    {
        //public static IEnumerable<(int index, T value)> WithIndex<T>(this IEnumerable<T> source)
        //{
        //    int index = 0;
        //    foreach (var item in source)
        //    {
        //        yield return (index, item);
        //        index++;
        //    }
        //}

        public static IEnumerable<(int index, string key, float value)> WithIndex(this IEnumerable<(string key, float value)> source)
        {
            int index = 0;
            foreach (var (k,v) in source)
            {
                yield return (index, k, v);
                index++;
            }
        }
    }
}

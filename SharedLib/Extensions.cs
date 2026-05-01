using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();

            using (cancellationToken.Register(() => tcs.TrySetResult(true)))
            {
                if (task != await Task.WhenAny(task, tcs.Task))
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            return await task;
        }
    }
}

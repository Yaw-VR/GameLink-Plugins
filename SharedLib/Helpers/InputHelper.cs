using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharedLib
{
    internal class InputHelper
    {
        public static IEnumerable<(string key, float value)> GetValues<T>(T data)
        {
            foreach (var field in (data?.GetType() ?? typeof(T)).GetFields())
            {
                if (field.FieldType.IsPrimitive)
                    yield return (field.Name, GetFloat(field, data));
                else
                    foreach (var (k, v) in GetValues(field.GetValue(data)))
                        yield return (field.Name + "." + k, v);
            }
#nullable enable
            float GetFloat(FieldInfo f, object? data = null)
            {
                var retval = data == null ? 0 : (float) (Convert.ChangeType(f.GetValue(data), typeof(float)) ?? 0);
                return retval;
            }
#nullable disable
        }



    }

    internal static class InputHelperExtensions
    {
        public static string[] Keys(this IEnumerable<(string key, float value)> inputs)
        {
            return inputs.Select(i => i.key).ToArray();
        }
    }
}

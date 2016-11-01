using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LeanMapper
{
    public static class TypePolyfill
    {
        public static bool IsPublic(this Type t)
        {
#if NET452
            return t.IsPublic;
#else
            return t.GetTypeInfo().IsPublic;
#endif
        }

        public static bool IsValueType(this Type t)
        {
#if NET452
            return t.IsValueType;
#else
            return t.GetTypeInfo().IsValueType;
#endif
        }

        public static Type BaseType(this Type t)
        {
#if NET452
            return t.BaseType;
#else
            return t.GetTypeInfo().BaseType;
#endif
        }
    }
}

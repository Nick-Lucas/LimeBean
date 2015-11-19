using System;
using System.Data.Common;
using System.Reflection;

namespace LimeBean {

#if DOTNET5_1

    partial class Extensions {
        internal static bool IsEnum(this Type type) {
            return type.GetTypeInfo().IsEnum;
        }
        internal static bool IsGenericType(this Type type) {
            return type.GetTypeInfo().IsGenericType;
        }
    }

#else

    [Serializable]
    partial class Bean {
    }

    partial class Extensions {
        internal static bool IsEnum(this Type type) {
            return type.IsEnum;
        }
        internal static bool IsGenericType(this Type type) {
            return type.IsGenericType;
        }
    }

#endif

#if !DOTNET5_1

    partial class BeanApi {
        public BeanApi(string connectionString, string providerName)
            : this(connectionString, DbProviderFactories.GetFactory(providerName)) {
        }
    }

#endif

}



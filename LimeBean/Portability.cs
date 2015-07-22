using System;

namespace LimeBean {

#if DOTNET

    using System.Reflection;

    static partial class Extensions {
        internal static bool IsEnum(this Type type) {
            return type.GetTypeInfo().IsEnum;
        }
    }

#else

    using System.Data.Common;

    [Serializable]
    public partial class Bean {
    }

    public partial class BeanApi {
        public BeanApi(string connectionString, string providerName)
            : this(connectionString, DbProviderFactories.GetFactory(providerName)) {
        }
    }

    static partial class Extensions {
        internal static bool IsEnum(this Type type) {
            return type.IsEnum;
        }
    }

#endif

}



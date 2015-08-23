using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {
    
    static partial class Extensions {

        internal static V GetSafe<K, V>(this IDictionary<K, V> dict, K key, V defaultValue = default(V)) {
            V existingValue;
            if(dict.TryGetValue(key, out existingValue))
                return existingValue;

            return defaultValue;
        }

        internal static string GetAutoIncrementName(this IKeyAccess keyAccess, string kind) {
            if(!keyAccess.IsAutoIncrement(kind))
                return null;

            return keyAccess.GetKeyNames(kind).First();
        }

        internal static bool IsSignedByteRange(this Int64 value) {
            return -128L <= value && value <= 127L;
        }

        internal static bool IsUnsignedByteRange(this Int64 value) {
            return 0L <= value && value <= 255L;
        }

        internal static bool IsInt32Range(this Int64 value) {
            return -0x80000000L <= value && value <= 0x7FFFFFFFL;
        }

    }

}

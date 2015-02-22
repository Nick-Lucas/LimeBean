using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {
    
    static class Extensions {

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

    }

}

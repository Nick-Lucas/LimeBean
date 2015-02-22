using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {
    interface IKeyAccess {
        bool IsAutoIncrement(string kind);
        ICollection<string> GetKeyNames(string kind);
        IConvertible GetKey(string kind, IDictionary<string, IConvertible> data);
        void SetKey(string kind, IDictionary<string, IConvertible> data, IConvertible key);
    }
}

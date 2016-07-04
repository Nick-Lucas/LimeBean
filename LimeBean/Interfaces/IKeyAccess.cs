using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean.Interfaces {

    interface IKeyAccess {
        bool IsAutoIncrement(string kind);
        ICollection<string> GetKeyNames(string kind);
        object GetKey(string kind, IDictionary<string, object> data);
        void SetKey(string kind, IDictionary<string, object> data, object key);
    }

}

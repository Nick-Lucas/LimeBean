using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {

    interface IStorage {
        IConvertible Store(string kind, IDictionary<string, IConvertible> data, ICollection<string> dirtyNames);
        IDictionary<string, IConvertible> Load(string kind, IConvertible key);
        void Trash(string kind, IConvertible key);
    }

}

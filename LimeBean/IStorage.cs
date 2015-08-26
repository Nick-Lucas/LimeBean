using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {

    interface IStorage {
        object Store(string kind, IDictionary<string, object> data, ICollection<string> dirtyNames);
        IDictionary<string, object> Load(string kind, object key);
        void Trash(string kind, object key);
    }

}

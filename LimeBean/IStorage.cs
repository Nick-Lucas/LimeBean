using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {

    interface IStorage {
        long Store(string kind, IDictionary<string, IConvertible> data);
        IDictionary<string, IConvertible> Load(string kind, long id);
        void Trash(string kind, long id);
    }

}

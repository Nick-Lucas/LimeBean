using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {

    class InMemoryStorage : IStorage {
        IDictionary<long, IDictionary<string, IConvertible>> _storage = new Dictionary<long, IDictionary<string, IConvertible>>();
        long _autoId = 0;

        long IStorage.Store(string kind, IDictionary<string, IConvertible> data) {
            if(!data.ContainsKey(Bean.ID_PROP_NAME))
                data[Bean.ID_PROP_NAME] = _autoId++;

            var id = data[Bean.ID_PROP_NAME].ToInt64(CultureInfo.InvariantCulture);

            _storage[id] = data;
            return id;

        }

        IDictionary<string, IConvertible> IStorage.Load(string kind, long id) {
            if(!_storage.ContainsKey(id))
                return null;

            return _storage[id];
        }

        void IStorage.Trash(string kind, long id) {
            _storage.Remove(id);
        }
    }

}

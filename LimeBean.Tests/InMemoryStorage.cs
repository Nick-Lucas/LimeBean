using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {

    class InMemoryStorage : IStorage {
        IList<IDictionary<string, IConvertible>> _storage = new List<IDictionary<string, IConvertible>>();        
        long _autoId = 0;
        IKeyAccess _keyAccess = new KeyUtil();

        public IConvertible Store(string kind, IDictionary<string, IConvertible> data) {
            var key = _keyAccess.GetKey(kind, data);

            if(key == null) {
                key = _autoId++;
                _keyAccess.SetKey(kind, data, key);
            } else {
                Trash(kind, key);
            }

            _storage.Add(data);
            return key;
        }

        public IDictionary<string, IConvertible> Load(string kind, IConvertible key) {
            return _storage.FirstOrDefault(item => _keyAccess.GetKey(kind, item) == key);
        }

        public void Trash(string kind, IConvertible key) {
            var item = Load(kind, key);
            if(item != null)
                _storage.Remove(item);
        }
    }

}

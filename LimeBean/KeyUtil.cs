using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {
    class KeyUtil : IKeyAccess {
        IDictionary<string, ICollection<string>> _names = new Dictionary<string, ICollection<string>>();
        IDictionary<string, bool> _autoIncrements = new Dictionary<string, bool>();

        public string DefaultName = "id";
        public bool DefaultAutoIncrement = true;        

        public bool IsAutoIncrement(string kind) {
            return _autoIncrements.GetSafe(kind, GetKeyNames(kind).Count > 1 ? false : DefaultAutoIncrement);
        }

        public ICollection<string> GetKeyNames(string kind) {
            return _names.GetSafe(kind, new[] { DefaultName });
        }

        public object GetKey(string kind, IDictionary<string, object> data) {
            var keyNames = GetKeyNames(kind);

            if(keyNames.Count  > 1) {
                var key = new CompoundKey();
                foreach(var name in keyNames)
                    key[name] = data.GetSafe(name);
                return key;
            }

            return data.GetSafe(keyNames.First());
        }

        public void SetKey(string kind, IDictionary<string, object> data, object key) {
            if(key is CompoundKey)
                throw new NotSupportedException();

            var name = GetKeyNames(kind).First();
            if(key == null)
                data.Remove(name);
            else
                data[name] = key;
        }

        public void RegisterKey(string kind, ICollection<string> names, bool? autoIncrement) {
            if(names.Count < 1)
                throw new ArgumentException();

            _names[kind] = names;

            if(autoIncrement != null)
                _autoIncrements[kind] = autoIncrement.Value;
        }

        public object PackCompoundKey(string kind, IEnumerable<object> components) {
            var result = new CompoundKey();
            foreach(var tuple in Enumerable.Zip(GetKeyNames(kind), components, Tuple.Create))
                result[tuple.Item1] = tuple.Item2;

            return result;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {
    class KeyUtil : IKeyAccess {
        static readonly string[] DEFAULT_NAMES = new[] { "id" };

        IDictionary<string, ICollection<string>> _names = new Dictionary<string, ICollection<string>>();
        IDictionary<string, bool> _autoIncrements = new Dictionary<string, bool>();

        public bool AutoIncrementByDefault = true;

        public bool IsAutoIncrement(string kind) {
            return _autoIncrements.GetSafe(kind, GetKeyNames(kind).Count > 1 ? false : AutoIncrementByDefault);
        }

        public ICollection<string> GetKeyNames(string kind) {
            return _names.GetSafe(kind, DEFAULT_NAMES);
        }

        public IConvertible GetKey(string kind, IDictionary<string, IConvertible> data) {
            var keyNames = GetKeyNames(kind);

            if(keyNames.Count  > 1) {
                var key = new CompoundKey();
                foreach(var name in keyNames)
                    key[name] = data.GetSafe(name);
                return key;
            }

            return data.GetSafe(keyNames.First());
        }

        public void SetKey(string kind, IDictionary<string, IConvertible> data, IConvertible key) {
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

        public IConvertible PackCompoundKey(string kind, IEnumerable<IConvertible> components) {
            var result = new CompoundKey();
            foreach(var tuple in Enumerable.Zip(GetKeyNames(kind), components, Tuple.Create))
                result[tuple.Item1] = tuple.Item2;

            return result;
        }

    }
}

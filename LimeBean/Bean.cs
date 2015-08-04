using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LimeBean {

    public partial class Bean {
        static readonly ConcurrentDictionary<Type, string> _kindCache = new ConcurrentDictionary<Type, string>();

        internal static string GetKind<T>() where T : Bean, new() {
            return _kindCache.GetOrAdd(typeof(T), type => new T().GetKind());
        }

        IDictionary<string, IConvertible> _props = new Dictionary<string, IConvertible>();
        IDictionary<string, IConvertible> _dirtyBackup;
        string _kind;

        internal bool Dispensed;

        internal Bean() {
        }

        protected internal Bean(string kind) {
            _kind = kind;
        }

        public string GetKind() {
            return _kind;
        }

        internal IConvertible GetKey(IKeyAccess access) {
            return access.GetKey(_kind, _props);
        }

        internal void SetKey(IKeyAccess access, IConvertible key) {
            access.SetKey(_kind, _props, key);
        }

        public override string ToString() {
            return _kind ?? base.ToString();
        }

        // Accessors

        public IConvertible this[string name] {
            get { return _props.GetSafe(name); }
            set {
                SaveDirtyBackup(name, value);
                _props[name] = value;
            }
        }

        public T Get<T>(string name) where T : IConvertible {
            var value = GetCore(name, typeof(T));
            if(value == null)
                return default(T);

            return (T)value;
        }

        public T? GetNullable<T>(string name) where T : struct, IConvertible {
            return (T?)GetCore(name, typeof(T));
        }

        object GetCore(string name, Type convertTo) {
            var value = this[name];

            if(value == null || value.GetType() == convertTo)
                return value;

            try {
                if(convertTo.IsEnum())
                    return Enum.Parse(convertTo, value.ToString(CultureInfo.InvariantCulture), true);

                return value.ToType(convertTo, CultureInfo.InvariantCulture);
            } catch {
                return null;
            }
        }

        public Bean Put(string name, IConvertible value) { 
            this[name] = value;
            return this;
        }

        public Bean Put<T>(string name, T? value) where T : struct, IConvertible {
            if(value != null)
                Put(name, value.Value);
            else
                this[name] = null;
            return this;
        }

        // Import / Export

        internal IDictionary<string, IConvertible> Export() {
            return new Dictionary<string, IConvertible>(_props);
        }

        internal void Import(IDictionary<string, IConvertible> data) {
            foreach(var entry in data)
                this[entry.Key] = entry.Value;
        }

        // Dirty tracking

        void SaveDirtyBackup(string name, IConvertible newValue) {
            var currentValue = this[name];
            if(Equals(newValue, currentValue))
                return;

            var initialValue = currentValue;
            if(_dirtyBackup != null && _dirtyBackup.ContainsKey(name))
                initialValue = _dirtyBackup[name];

            if(Equals(newValue, initialValue)) {
                if(_dirtyBackup != null)
                    _dirtyBackup.Remove(name);
            } else {
                if(_dirtyBackup == null)
                    _dirtyBackup = new Dictionary<string, IConvertible>();
                _dirtyBackup[name] = currentValue;
            }
        }

        internal void ForgetDirtyBackup() {
            _dirtyBackup = null;
        }

        internal ICollection<string> GetDirtyNames() {
            if(_dirtyBackup == null)
                return new string[0];

            return new HashSet<string>(_dirtyBackup.Keys);
        }

        // Hooks

        protected internal virtual void AfterDispense() {
        }

        protected internal virtual void BeforeLoad() {
        }

        protected internal virtual void AfterLoad() {
        }

        protected internal virtual void BeforeStore() {
        }

        protected internal virtual void AfterStore() {
        }

        protected internal virtual void BeforeTrash() {
        }

        protected internal virtual void AfterTrash() {
        }

    }

}

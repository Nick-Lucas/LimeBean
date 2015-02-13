using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LimeBean {

    [Serializable]
    public class Bean {
        public const string ID_PROP_NAME = "id";

        static readonly ConcurrentDictionary<Type, string> _kindCache = new ConcurrentDictionary<Type, string>();

        internal static string GetKind<T>() where T : Bean, new() {
            return _kindCache.GetOrAdd(typeof(T), type => new T().GetKind());
        }

        IDictionary<string, IConvertible> _props = new Dictionary<string, IConvertible>();
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

        public long? ID {
            get { return GetNullable<long>(ID_PROP_NAME); }
            internal set { Put(ID_PROP_NAME, value); }
        }

        public override string ToString() {
            if(_kind == null)
                return base.ToString();

            var result = _kind;
            if(ID != null)
                result += " #" + ID;

            return result;
        }

        // Accessors

        public IConvertible this[string name] {
            get {
                if(_props.ContainsKey(name))
                    return _props[name];
                return null;
            }
            set {
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

            if(value == null || value.GetTypeCode() == Type.GetTypeCode(convertTo))
                return value;

            try {
                if(convertTo.IsEnum)
                    return Enum.Parse(convertTo, value.ToString(CultureInfo.InvariantCulture), true);

                return value.ToType(convertTo, CultureInfo.InvariantCulture);
            } catch {
                return null;
            }
        }

        public Bean Put<T>(string name, T value) where T : IConvertible { 
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

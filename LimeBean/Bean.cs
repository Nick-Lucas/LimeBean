using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using LimeBean.Interfaces;

namespace LimeBean {

    public partial class Bean : IBean {
        static readonly ConcurrentDictionary<Type, string> _kindCache = new ConcurrentDictionary<Type, string>();

        internal static string GetKind<T>() where T : Bean, new() {
            return _kindCache.GetOrAdd(typeof(T), type => new T().GetKind());
        }

        IDictionary<string, object> _props = new Dictionary<string, object>();
        IDictionary<string, object> _dirtyBackup;
        string _kind;

        internal bool Dispensed;
        internal BeanApi Api;

        internal Bean() {
        }

        protected internal Bean(string kind) {
            _kind = kind;
        }

        /// <summary>
        /// Get the Kind (Table Name) of the Bean
        /// </summary>
        /// <returns>Table name</returns>
        public string GetKind() {
            return _kind;
        }

        internal object GetKey(IKeyAccess access) {
            return access.GetKey(_kind, _props);
        }

        internal void SetKey(IKeyAccess access, object key) {
            access.SetKey(_kind, _props, key);
        }

        public override string ToString() {
            return _kind ?? base.ToString();
        }


        // Accessors

        /// <summary>
        /// Get or Set the value of a Column
        /// </summary>
        /// <param name="name">Name of the Column to Get or Set</param>
        public object this[string name] {
            get {
                if (ValidateGetColumns)
                    ValidateColumnExists(name);

                return _props.GetSafe(name); }
            set {
                SaveDirtyBackup(name, value);
                _props[name] = value;
            }
        }

        /// <summary>
        /// Get the value of a Column in a given Type
        /// </summary>
        /// <typeparam name="T">Type of the return value</typeparam>
        /// <param name="name">Name of the Column to Get</param>
        /// <returns>Value of the requested Column as type T</returns>
        public T Get<T>(string name) {
            return this[name].ConvertSafe<T>();
        }

        /// <summary>
        /// Set the value of a Column
        /// </summary>
        /// <param name="name">Name of the Column to Set</param>
        /// <param name="value">Value to Set the Column to</param>
        public Bean Put(string name, object value) {
            this[name] = value;
            return this;
        }

        /// <summary>
        /// Retrieve the name of each Column held in this Bean
        /// </summary>
        public IEnumerable<string> Columns {
            get { return _props.Keys; } 
        }


        // Bean Options

        internal bool ValidateGetColumns {
            get;
            set;
        }

        private void ValidateColumnExists(string name) {
            if (_props.ContainsKey(name) == false)
                throw Exceptions.ColumnNotFoundException.New(this, name);
        }


        // Import / Export

        internal IDictionary<string, object> Export() {
            return new Dictionary<string, object>(_props);
        }

        internal void Import(IDictionary<string, object> data) {
            foreach(var entry in data)
                this[entry.Key] = entry.Value;
        }


        // Dirty tracking

        void SaveDirtyBackup(string name, object newValue) {
            var currentValue = _props.GetSafe(name);
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
                    _dirtyBackup = new Dictionary<string, object>();
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

        protected BeanApi GetApi() {
            return Api;
        }

        protected virtual void AfterDispense() {
        }

        protected virtual void BeforeLoad() {
        }

        protected virtual void AfterLoad() {
        }

        protected virtual void BeforeStore() {
        }

        protected virtual void AfterStore() {
        }

        protected virtual void BeforeTrash() {
        }

        protected virtual void AfterTrash() {
        }

    }

}

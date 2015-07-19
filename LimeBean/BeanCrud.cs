using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {

    class BeanCrud : IBeanCrud {
        IStorage _storage;
        ITransactionSupport _transactionSupport;
        IKeyAccess _keyAccess;
        ICollection<BeanObserver> _observers;

        public BeanCrud(IStorage storage, ITransactionSupport transactionSupport, IKeyAccess keys) {
            _storage = storage;
            _transactionSupport = transactionSupport;
            _keyAccess = keys;
            _observers = new HashSet<BeanObserver>();
            DirtyTracking = true;
        }

        public bool DirtyTracking { get; set; }

        public void AddObserver(BeanObserver observer) {
            _observers.Add(observer);
        }

        public void RemoveObserver(BeanObserver observer) {
            _observers.Remove(observer);
        }

        public Bean Dispense(string kind) {
            return ContinueDispense(new Bean(kind));
        }

        public T Dispense<T>() where T : Bean, new() {
            return ContinueDispense(new T());
        }

        public Bean RowToBean(string kind, IDictionary<string, IConvertible> row) {
            if(row == null)
                return null;
            
            return ContinueLoad(Dispense(kind), row);
        }

        public T RowToBean<T>(IDictionary<string, IConvertible> row) where T : Bean, new() {
            if(row == null)
                return null;

            return ContinueLoad(Dispense<T>(), row);
        }

        public Bean Load(string kind, IConvertible key) {
            return RowToBean(kind, _storage.Load(kind, key));
        }

        public T Load<T>(IConvertible key) where T : Bean, new() {
            return RowToBean<T>(_storage.Load(Bean.GetKind<T>(), key));
        }

        public IConvertible Store(Bean bean) {
            EnsureDispensed(bean);

            ImplicitTransaction(delegate() {
                bean.BeforeStore();
                foreach(var observer in _observers)
                    observer.BeforeStore(bean);

                var key = _storage.Store(bean.GetKind(), bean.Export(), DirtyTracking ? bean.GetDirtyNames() : null);
                if(key is CompoundKey) {
                    // compound keys must not change during insert/update
                } else {
                    bean.SetKey(_keyAccess, key);
                }

                bean.AfterStore();
                foreach(var observer in _observers)
                    observer.AfterStore(bean);

                return true;
            });

            bean.ForgetDirtyBackup();
            return bean.GetKey(_keyAccess);
        }

        public void Trash(Bean bean) {
            EnsureDispensed(bean);

            if(bean.GetKey(_keyAccess) == null)
                return;

            ImplicitTransaction(delegate() {
                bean.BeforeTrash();
                foreach(var observer in _observers)
                    observer.BeforeTrash(bean);

                _storage.Trash(bean.GetKind(), bean.GetKey(_keyAccess));

                bean.AfterTrash();
                foreach(var observer in _observers)
                    observer.AfterTrash(bean);

                return true;
            });
        }

        T ContinueDispense<T>(T bean) where T : Bean {
            bean.Dispensed = true;

            bean.AfterDispense();
            foreach(var observer in _observers)
                observer.AfterDispense(bean);

            return bean;
        }

        T ContinueLoad<T>(T bean, IDictionary<string, IConvertible> row) where T : Bean {
            bean.BeforeLoad();
            foreach(var observer in _observers)
                observer.BeforeLoad(bean);

            bean.Import(row);
            bean.ForgetDirtyBackup();

            bean.AfterLoad();
            foreach(var observer in _observers)
                observer.AfterLoad(bean);

            return bean;
        }

        void ImplicitTransaction(Func<bool> action) {
            if(_transactionSupport == null || !_transactionSupport.ImplicitTransactions || _transactionSupport.InTransaction)
                action();
            else
                _transactionSupport.Transaction(action);
        }

        void EnsureDispensed(Bean bean) {
            if(!bean.Dispensed)
                throw new InvalidOperationException("Do not instantiate beans directly, use BeanApi.Dispense method instead.");
        }
    }

}

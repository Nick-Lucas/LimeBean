using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {

    class BeanCrud : IBeanCrud {
        IStorage _storage;
        ITransactionSupport _transactionSupport;
        ICollection<BeanObserver> _observers;

        public BeanCrud(IStorage storage, ITransactionSupport transactionSupport) {
            _storage = storage;
            _transactionSupport = transactionSupport;
            _observers = new HashSet<BeanObserver>();
        }

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

        public Bean Load(string kind, IDictionary<string, IConvertible> data) {
            if(data == null)
                return null;
            
            return ContinueLoad(Dispense(kind), data);
        }

        public T Load<T>(IDictionary<string, IConvertible> data) where T : Bean, new() {
            if(data == null)
                return null;

            return ContinueLoad(Dispense<T>(), data);
        }

        public Bean Load(string kind, long id) {
            return Load(kind, _storage.Load(kind, id));
        }

        public T Load<T>(long id) where T : Bean, new() {
            return Load<T>(_storage.Load(Bean.GetKind<T>(), id));
        }

        public long Store(Bean bean) {
            EnsureDispensed(bean);

            ImplicitTransaction(delegate() {
                bean.BeforeStore();
                foreach(var observer in _observers)
                    observer.BeforeStore(bean);

                bean.ID = _storage.Store(bean.GetKind(), bean.Export());

                bean.AfterStore();
                foreach(var observer in _observers)
                    observer.AfterStore(bean);

                return true;
            });

            return bean.ID.Value;
        }

        public void Trash(Bean bean) {
            EnsureDispensed(bean);

            if(bean.ID == null)
                return;

            ImplicitTransaction(delegate() {
                bean.BeforeTrash();
                foreach(var observer in _observers)
                    observer.BeforeTrash(bean);

                _storage.Trash(bean.GetKind(), bean.ID.Value);

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

            bean.AfterLoad();
            foreach(var observer in _observers)
                observer.AfterLoad(bean);

            return bean;
        }

        void ImplicitTransaction(Func<bool> action) {
            if(_transactionSupport == null || _transactionSupport.InTransaction)
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

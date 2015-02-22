using System;
using System.Collections.Generic;

namespace LimeBean {

    interface IBeanCrud {
        void AddObserver(BeanObserver observer);
        void RemoveObserver(BeanObserver observer);

        Bean Dispense(string kind);
        T Dispense<T>() where T : Bean, new();

        Bean RowToBean(string kind, IDictionary<string, IConvertible> row);
        T RowToBean<T>(IDictionary<string, IConvertible> row) where T : Bean, new();

        Bean Load(string kind, IConvertible key);
        T Load<T>(IConvertible key) where T : Bean, new();

        IConvertible Store(Bean bean);

        void Trash(Bean bean);
    }

}

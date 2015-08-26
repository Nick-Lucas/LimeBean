using System;
using System.Collections.Generic;

namespace LimeBean {

    interface IBeanCrud {
        bool DirtyTracking { get; set; }

        void AddObserver(BeanObserver observer);
        void RemoveObserver(BeanObserver observer);

        Bean Dispense(string kind);
        T Dispense<T>() where T : Bean, new();

        Bean RowToBean(string kind, IDictionary<string, object> row);
        T RowToBean<T>(IDictionary<string, object> row) where T : Bean, new();

        Bean Load(string kind, object key);
        T Load<T>(object key) where T : Bean, new();

        object Store(Bean bean);

        void Trash(Bean bean);
    }

}

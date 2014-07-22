using System;
using System.Collections.Generic;

namespace LimeBean {

    interface IBeanCrud {
        void AddObserver(BeanObserver observer);
        void RemoveObserver(BeanObserver observer);

        Bean Dispense(string kind);
        T Dispense<T>() where T : Bean, new();

        Bean Load(string kind, IDictionary<string, IConvertible> data);
        T Load<T>(IDictionary<string, IConvertible> data) where T : Bean, new();

        Bean Load(string kind, long id);
        T Load<T>(long id) where T : Bean, new();

        long Store(Bean bean);

        void Trash(Bean bean);
    }

}

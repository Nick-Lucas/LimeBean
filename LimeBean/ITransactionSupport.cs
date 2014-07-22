using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {

    interface ITransactionSupport {
        bool InTransaction { get; }
        void Transaction(Func<bool> action);
    }

}

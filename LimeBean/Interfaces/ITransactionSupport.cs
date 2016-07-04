using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LimeBean.Interfaces {

    interface ITransactionSupport {
        bool ImplicitTransactions { get; set; }
        bool InTransaction { get; }
        IsolationLevel TransactionIsolation { get; set; }
        void Transaction(Func<bool> action);
    }

}

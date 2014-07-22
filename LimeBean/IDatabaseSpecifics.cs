using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {
    
    interface IDatabaseSpecifics {
        string QuoteName(string name);
        long GetLastInsertID();
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean.Interfaces {

    public interface IBeanDispenser {
        Bean Dispense(string kind);
        T Dispense<T>() where T : Bean, new();
    }

}

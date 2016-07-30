using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean.Interfaces {

    public interface IBeanFactory : IBeanDispenser {
        IBeanOptions Options { get; }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {

    class BeanApiLinker : BeanObserver {
        BeanApi _api;

        public BeanApiLinker(BeanApi api) {
            _api = api;
        }

        public override void AfterDispense(Bean bean) {
            bean.Api = _api;
        }
    }

}

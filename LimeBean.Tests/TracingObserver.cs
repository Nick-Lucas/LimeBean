using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {

    class TracingObserver : BeanObserver {
        public string TraceLog = "";
        public Bean LastBean;

        void Trace(Bean bean, string subject) {
            LastBean = bean;
            if(TraceLog.Length > 0)
                TraceLog += " ";
            TraceLog += subject + ":" + bean[Bean.ID_PROP_NAME];
        }

        public override void AfterDispense(Bean bean) {
            Trace(bean, "ad");
        }

        public override void BeforeStore(Bean bean) {
            Trace(bean, "bs");
        }

        public override void AfterStore(Bean bean) {
            Trace(bean, "as");
        }

        public override void BeforeLoad(Bean bean) {
            Trace(bean, "bl");
        }

        public override void AfterLoad(Bean bean) {
            Trace(bean, "al");
        }

        public override void BeforeTrash(Bean bean) {
            Trace(bean, "bt");
        }

        public override void AfterTrash(Bean bean) {
            Trace(bean, "at");
        }

    }

}

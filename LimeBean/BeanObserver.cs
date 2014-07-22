using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {

    public abstract class BeanObserver {
        public virtual void AfterDispense(Bean bean) {
        }

        public virtual void BeforeLoad(Bean bean) {
        }

        public virtual void AfterLoad(Bean bean) {
        }

        public virtual void BeforeStore(Bean bean) {
        }

        public virtual void AfterStore(Bean bean) {
        }

        public virtual void BeforeTrash(Bean bean) {
        }

        public virtual void AfterTrash(Bean bean) {
        }
    }

}

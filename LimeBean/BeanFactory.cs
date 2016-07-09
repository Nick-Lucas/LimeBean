using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LimeBean.Interfaces;

namespace LimeBean {

    internal class BeanFactory : IBeanFactory {
        internal BeanFactory() { }

        IBeanConfiguration _config;
        public IBeanConfiguration Config {
            get {
                if (_config == null) 
                    _config = new BeanConfiguration();
                return _config;
            }
        }
        
        public Bean Dispense(string kind) {
            Bean bean = new Bean(kind);
            return ConfigureBean(bean);
        }

        public T Dispense<T>() where T : Bean, new() {
            T bean = new T();
            return ConfigureBean(bean);
        }

        private T ConfigureBean<T>(T bean) where T : Bean {
            bean.ValidateGetColumns = Config.ValidateGetColumns;
            return bean;
        }

    }

}

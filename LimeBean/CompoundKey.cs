using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LimeBean {

    class CompoundKey {
        IDictionary<string, object> _components = new Dictionary<string, object>();

        public object this[string component] {
            get { return _components.ContainsKey(component) ? _components[component] : null; }
            set {
                if(value == null)
                    throw new ArgumentNullException(component);

                _components[component] = value; 
            }
        }

        public override string ToString() {            
            return String.Join(", ", _components.OrderBy(e => e.Key).Select(c => c.Key + "=" + Convert.ToString(c.Value, CultureInfo.InvariantCulture)));
        }
    
    }

}
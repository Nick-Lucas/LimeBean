using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LimeBean.Interfaces;

namespace LimeBean {
    class BeanOptions : IBeanOptions {

        /// <summary>
        /// Specifies whether each Bean[column] or Bean.Get<T>(column) call 
        /// will throw ColumnNotFoundException if the column does not exist. Default True
        /// </summary>
        public bool ValidateGetColumns {
            get { return _ValidateGetColumns; }
            set { _ValidateGetColumns = value; }
        }
        private bool _ValidateGetColumns = true;
    }
}

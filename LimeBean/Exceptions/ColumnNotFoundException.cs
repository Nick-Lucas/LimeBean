using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean.Exceptions {
    class ColumnNotFoundException : Exception {
        public ColumnNotFoundException() { }

        public ColumnNotFoundException(string message) : base(message) { }

        public static ColumnNotFoundException New(Bean bean, string column) {
            string message = String.Format(
                @"The requested column '{0}' for Bean '{1}' was not found. 
                You can assign a value to the column to create it", 
                column, bean.GetKind());
            return new ColumnNotFoundException(message);
        }
    }
}

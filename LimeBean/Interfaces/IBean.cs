using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean.Interfaces {

    public interface IBean {
        string GetKind();
        

        // Accessors

        object this[string name] { get; set; }

        T Get<T>(string name);

        Bean Put(string name, object value);

        IEnumerable<string> Columns { get; }


        // Flags

        bool ValidateGetColumns { get; set; }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean.Interfaces {

    public interface IValueRelaxations {
        bool TrimStrings { get; set; }
        bool ConvertEmptyStringToNull { get; set; }
        bool RecognizeIntegers { get; set; }
    }

}

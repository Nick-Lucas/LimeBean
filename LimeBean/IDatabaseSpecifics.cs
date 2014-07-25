using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {
    
    interface IDatabaseSpecifics {
        bool TrimStrings { get; set; }
        bool ConvertEmptyStringToNull { get; set; }
        bool RecognizeIntegers { get; set; }

        string QuoteName(string name);
        long GetLastInsertID();
    }

}

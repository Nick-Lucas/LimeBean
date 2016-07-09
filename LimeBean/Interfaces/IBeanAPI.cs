using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace LimeBean.Interfaces {

    public interface IBeanApi : IDisposable, IBeanCrud, IBeanFinder, IDatabaseAccess, IValueRelaxations {
        // Properties

        DbConnection Connection { get; }

        void EnterFluidMode();

        // Custom keys
        
        void Key(string kind, string name, bool autoIncrement);
        void Key(string kind, params string[] names);
        void Key<T>(string name, bool autoIncrement) where T : Bean, new();
        void Key<T>(params string[] names) where T : Bean, new();
        void DefaultKey(bool autoIncrement);
        void DefaultKey(string name, bool autoIncrement = true);
    }

}

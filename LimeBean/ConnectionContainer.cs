using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace LimeBean {

    abstract class ConnectionContainer  {
        public abstract DbConnection Connection { get; }

        public virtual void Dispose() {         
        }

        internal class SimpleImpl : ConnectionContainer {
            DbConnection _conn;

            public SimpleImpl(DbConnection conn) {
                _conn = conn;
            }

            public override DbConnection Connection { 
                get { return _conn; } 
            }
        }

        internal class LazyImpl : ConnectionContainer {
            DbConnection _conn;
            string _connectionString;
            Func<DbConnection> _factory;

            public LazyImpl(string connectionString, Func<DbConnection> factory) {
                _connectionString = connectionString;
                _factory = factory;
            }

            public override DbConnection Connection {
                get {
                    if(_conn == null) {
                        _conn = _factory();                        
                        _conn.ConnectionString = _connectionString;
                        _conn.Open();
                        _factory = null;
                        _connectionString = null;
                    }
                    return _conn; 
                }
            }

            public sealed override void Dispose() {
                if(_conn != null)
                    _conn.Dispose();
            }
        }

    }

}

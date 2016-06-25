using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LimeBean.Tests {

#if SQLITE_MODE_MS

    using Microsoft.Data.Sqlite;

    static partial class SQLitePortability {
        public static readonly Type ExceptionType = typeof(SqliteException);

        public static BeanApi CreateApi(string connectionString = IN_MEMORY_CONNECTION_STRING) {
            return new BeanApi(connectionString, typeof(SqliteConnection));
        }

        public static DbConnection CreateConnection(string connectionString = IN_MEMORY_CONNECTION_STRING) {
            return new SqliteConnection(connectionString);
        }
    }

#elif SQLITE_MODE_MONO

    using Mono.Data.Sqlite;

    static partial class SQLitePortability {
        public static readonly Type ExceptionType = typeof(SqliteException);

        public static BeanApi CreateApi(string connectionString = IN_MEMORY_CONNECTION_STRING) {
            return new BeanApi(connectionString, SqliteFactory.Instance);
        }

        public static DbConnection CreateConnection(string connectionString = IN_MEMORY_CONNECTION_STRING) {
            return new SqliteConnection(connectionString);
        }
    }

#else

    using System.Data.SQLite;

    static partial class SQLitePortability {
        public static readonly Type ExceptionType = typeof(SQLiteException);

        public static BeanApi CreateApi(string connectionString = IN_MEMORY_CONNECTION_STRING) {
            return new BeanApi(connectionString, SQLiteFactory.Instance);
        }

        public static DbConnection CreateConnection(string connectionString = IN_MEMORY_CONNECTION_STRING) {
            return new SQLiteConnection(connectionString);
        }
    }

#endif

    static partial class SQLitePortability {
        const string IN_MEMORY_CONNECTION_STRING = "data source=:memory:";
    }

}

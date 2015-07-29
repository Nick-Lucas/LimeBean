#if !NO_MSSQL
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

namespace LimeBean.Tests.Fixtures {

    public class MsSqlConnectionFixture : IDisposable {
        public readonly ICollection<string> DropList = new List<string>();
        public readonly DbConnection Connection;

        public MsSqlConnectionFixture() {
            Connection = new SqlConnection(TestEnv.MsSqlConnectionString);
            Connection.Open();
        }

        public void Dispose() {
            TestEnv.MsSqlTearDown(new DatabaseAccess(Connection, null), DropList);
            Connection.Dispose();        
        }
    }

}
#endif
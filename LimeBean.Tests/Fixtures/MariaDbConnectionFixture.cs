#if !DNX
using System;
using System.Data.Common;
using MySql.Data.MySqlClient;

namespace LimeBean.Tests.Fixtures {

    public class MariaDbConnectionFixture {
        public readonly DbConnection Connection;

        public MariaDbConnectionFixture() {
            Connection = new MySqlConnection(TestEnv.MariaConnectionString);
            Connection.Open();
        }

        public void Dispose() {
            Connection.Close();
        }
    }

}
#endif
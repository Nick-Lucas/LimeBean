#if !NO_MSSQL
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

namespace LimeBean.Tests.Fixtures {

    public class MsSqlConnectionFixture : ConnectionFixture {
        ICollection<string> _dropList = new List<string>();

        static string ConnectionString {
            get { return "server=" + ServerName + "; user instance=true; integrated security=true; connection timeout=90"; }
        }

        static string ServerName {
            get { return GetEnvVar("MSSQL_NAME", ".\\SQLEXPRESS"); }
        }

        public MsSqlConnectionFixture() {
            Connection = new SqlConnection(ConnectionString);
            Connection.Open();
        }

        public override void Dispose() {
            Exec(Connection, "use master");
            foreach(var name in _dropList)
                Exec(Connection, "drop database " + name);

            Connection.Close();
        }

        public override void SetUpDatabase() {
            var name = GenerateTempDbName();
            _dropList.Add(name);

            Exec(Connection, "create database " + name);
            Exec(Connection, "use " + name);  
        }

        public override void TearDownDatabase() {
        }
    }

}
#endif
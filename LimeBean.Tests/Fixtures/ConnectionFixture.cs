using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LimeBean.Tests.Fixtures {

    public abstract class ConnectionFixture : IDisposable {
        public DbConnection Connection { get; protected set; }

        public abstract void Dispose();
        public abstract void SetUpDatabase();
        public abstract void TearDownDatabase();

        protected static string GenerateTempDbName() {
            return "lime_bean_" + Guid.NewGuid().ToString("N");
        }

        protected static string GetEnvVar(string key, string defaultValue) {
            return Environment.GetEnvironmentVariable("LIME_TEST_" + key) ?? defaultValue;
        }

        protected static void Exec(DbConnection conn, string sql) {
            using(var cmd = conn.CreateCommand()) {
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }
    }

}

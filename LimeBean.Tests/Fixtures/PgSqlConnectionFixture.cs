#if !NO_PGSQL
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LimeBean.Tests.Fixtures {

    public class PgSqlConnectionFixture : ConnectionFixture {
        DbConnection _serviceConnection;
        string _dbName;

        static string Host {
            get { return GetEnvVar("PGSQL_HOST", "localhost"); }
        }

        static string User {
            get { return GetEnvVar("PGSQL_USER", "postgres"); }
        }

        static string Password {
            get { return GetEnvVar("PGSQL_PWD", "qwerty"); }
        }

        public PgSqlConnectionFixture() {
            _serviceConnection = new NpgsqlConnection(FormatConnectionString("postgres"));
            _serviceConnection.Open();
        }

        public override void Dispose() {
            _serviceConnection.Close();
        }

        public override void SetUpDatabase() {
            _dbName = GenerateTempDbName();
            Exec(_serviceConnection, "create database " + _dbName);

            Connection = new NpgsqlConnection(FormatConnectionString(_dbName));
            Connection.Open();
        }

        public override void TearDownDatabase() {
            Connection.Close();

            Exec(_serviceConnection, "select pg_terminate_backend(pid) from pg_stat_activity where datname='" + _dbName + "'");
            Exec(_serviceConnection, "drop database if exists " + _dbName);
        }

        static string FormatConnectionString(string db) {
            return "host=" + Host + "; username=" + User + "; password='" + Password + "'" + "; database=" + db;
        }

    }

}
#endif
#if !NO_MARIADB
using System;
using System.Data.Common;
using MySql.Data.MySqlClient;

namespace LimeBean.Tests.Fixtures {

    public class MariaDbConnectionFixture : ConnectionFixture {
        string _dbName;

        static string ConnectionString {
            get { return "server=" + Host + "; uid=" + User + "; pwd=" + Password; }
        }

        static string Host {
            get { return GetEnvVar("MARIA_HOST", "localhost"); }
        }

        static string User {
            get { return GetEnvVar("MARIA_USER", "root"); }
        }

        static string Password {
            get { return GetEnvVar("MARIA_PWD", "qwerty"); }
        }

        public MariaDbConnectionFixture() {
            Connection = new MySqlConnection(ConnectionString);
            Connection.Open();
        }

        public override void Dispose() {
            Connection.Close();
        }

        public override void SetUpDatabase() {
            _dbName = GenerateTempDbName();

            Exec(Connection, "set sql_mode=STRICT_TRANS_TABLES");
            Exec(Connection, "create database " + _dbName);
            Exec(Connection, "use " + _dbName);            
        }

        public override void TearDownDatabase() {
            Exec(Connection, "drop database if exists " + _dbName);
        }

    }

}
#endif
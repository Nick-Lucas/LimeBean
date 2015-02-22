using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {

    static class TestEnv {
        static readonly string MariaDbName = GenerateTempDbName();

        public static string MariaConnectionString {
            get { return "server=" + MariaHost + "; uid=" + MariaUser + "; pwd=" + MariaPassword; }
        }

        public static string MsSqlConnectionString {
            get { return "server=" + MsSqlName + "; user instance=true; integrated security=true; connection timeout=90"; }
        }

        static string MsSqlName {
            get { return GetEnvVar("MSSQL_NAME", ".\\SQLEXPRESS");  }
        }

        static string MariaHost {
            get { return GetEnvVar("MARIA_HOST", "localhost");  }
        }

        static string MariaUser {
            get { return GetEnvVar("MARIA_USER", "root"); }
        }

        static string MariaPassword {
            get { return GetEnvVar("MARIA_PWD", "qwerty"); }
        }

        public static void MariaSetUp(IDatabaseAccess db) {
            db.Exec("set sql_mode=STRICT_TRANS_TABLES");
            db.Exec("create database " + MariaDbName);
            db.Exec("use " + MariaDbName);        
        }

        public static void MariaTearDown(IDatabaseAccess db) {
            db.Exec("drop database if exists " + MariaDbName);
        }

        public static void MsSqlSetUp(IDatabaseAccess db, ICollection<string> dropList) {
            var name = GenerateTempDbName();
            dropList.Add(name);

            db.Exec("create database " + name);
            db.Exec("use " + name);        
        }

        public static void MsSqlTearDown(IDatabaseAccess db, ICollection<string> dropList) {
            db.Exec("use master");
            foreach(var name in dropList)
                db.Exec("drop database " + name);            
        }

        static string GetEnvVar(string key, string defaultValue) {
            return Environment.GetEnvironmentVariable("LIME_TEST_" + key) ?? defaultValue;
        }

        static string GenerateTempDbName() {
            return "lime_bean_" + Guid.NewGuid().ToString("N");
        }

    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {

    static class TestEnv {

        public static string MsSqlName {
            get { return Get("MSSQL_NAME", ".\\SQLEXPRESS");  }
        }

        public static string MariaHost {
            get { return Get("MARIA_HOST", "localhost");  }
        }

        public static string MariaUser {
            get { return Get("MARIA_USER", "root"); }
        }

        public static string MariaPassword {
            get { return Get("MARIA_PWD", "qwerty"); }
        }


        static string Get(string key, string defaultValue) {
            return Environment.GetEnvironmentVariable("LIME_TEST_" + key) ?? defaultValue;
        }

    }

}

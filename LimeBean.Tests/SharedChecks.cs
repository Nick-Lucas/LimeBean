using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {

    static class SharedChecks {

        public static void CheckSchemaReadingKeepsCache(IDatabaseAccess db, DatabaseStorage storage) {
            db.Exec("create table foo(bar int)");
            db.Exec("insert into foo(bar) values(1)");

            var queryCount = 0;
            db.QueryExecuting += cmd => queryCount++;

            db.Cell<int>(true, "Select * from foo");
            storage.GetSchema();

            var savedQueryCount = queryCount;
            db.Cell<int>(true, "Select * from foo");

            Assert.AreEqual(savedQueryCount, queryCount);        
        }

        public static void CheckRoundtripOfExtremalValues(RoundtripChecker checker) {
            checker.Check(Int64.MinValue, Int64.MinValue);
            checker.Check(Int64.MaxValue, Int64.MaxValue);
            checker.Check(Double.Epsilon, Double.Epsilon);
            checker.Check(Double.MinValue, Double.MinValue);
            checker.Check(Double.MaxValue, Double.MaxValue);

            var text = String.Empty.PadRight(84 * 1000, 'x');
            checker.Check(text, text);
        }

        public static void CheckRoundtripForcesString(RoundtripChecker checker) {
            checker.Check(9223372036854775808, "9223372036854775808");
            checker.Check(9223372036854775808M, "9223372036854775808");
            checker.Check(new DateTime(1984, 6, 14, 13, 14, 15), "06/14/1984 13:14:15");        
        }

    }

}

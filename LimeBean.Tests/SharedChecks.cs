using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

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

            Assert.Equal(savedQueryCount, queryCount);        
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
            checker.Check(new DateTime(1984, 6, 14, 13, 14, 15), "1984-06-14 13:14:15");        
        }

        public static void CheckDateTimeQueries(IDatabaseAccess db, DatabaseStorage storage) {
            storage.EnterFluidMode();

            var date = new DateTime(2015, 1, 1);
            var dateTime = date.AddHours(1).AddMinutes(2).AddSeconds(3);

            Func<DateTime, IDictionary<string, IConvertible>> makeRow = d => new Dictionary<string, IConvertible> { 
                { "d", d }
            };

            storage.Store("foo", makeRow(date));
            storage.Store("foo", makeRow(dateTime));
            Assert.Equal(2, db.Cell<int>(false, "select count(*) from foo where d = {0} or d = {1}", date, dateTime));

            db.Exec("delete from foo");

            for(var i = -2; i <= 2; i++)
                storage.Store("foo", makeRow(date.AddDays(i)));
            Assert.Equal(3, db.Cell<int>(false, "select count(*) from foo where d between {0} and {1}", date.AddDays(-1), date.AddDays(1)));

            Assert.Equal(date, db.Cell<DateTime>(false, "select d from foo where d = {0}", date));        
        }

    }

}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Xunit;

namespace LimeBean.Tests {

    static class SharedChecks {
        public static readonly Guid SAMPLE_GUID = Guid.NewGuid();
        public static readonly DateTime SAMPLE_DATETIME = new DateTime(1984, 6, 14, 11, 22, 33);
        public static readonly DateTimeOffset SAMPLE_DATETIME_OFFSET = new DateTimeOffset(SAMPLE_DATETIME, TimeSpan.FromHours(6));
        public static readonly byte[] SAMPLE_BLOB = new byte[] { 1, 2, 3 };

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

        public static void CheckRoundtripOfExtremalValues(RoundtripChecker checker, bool checkDecimal = false, bool checkDateTime = false, bool checkDateTimeOffset = false) {
            checker.Check(Int64.MinValue, Int64.MinValue);
            checker.Check(Int64.MaxValue, Int64.MaxValue);
            checker.Check(Double.Epsilon, Double.Epsilon);
            checker.Check(Double.MinValue, Double.MinValue);
            checker.Check(Double.MaxValue, Double.MaxValue);

            if(checkDecimal) {
                checker.Check(Decimal.MinValue, decimal.MinValue);
                checker.Check(Decimal.MaxValue, decimal.MaxValue);
            }

            if(checkDateTime) {
                checker.Check(DateTime.MinValue, DateTime.MinValue);
                checker.Check(DateTime.MaxValue.Date, DateTime.MaxValue.Date);            
            }

            if(checkDateTimeOffset) {
                checker.Check(DateTimeOffset.MinValue, DateTimeOffset.MinValue);
                checker.Check(DateTimeOffset.MaxValue.Date, DateTimeOffset.MaxValue.Date);
            }

            var text = String.Empty.PadRight(84 * 1000, 'x');
            checker.Check(text, text);
        }

        public static void CheckBigNumberRoundtripForcesString(RoundtripChecker checker) {
            checker.Check(9223372036854775808, "9223372036854775808");
            checker.Check(9223372036854775808M, "9223372036854775808");
        }

        public static void CheckLongToDouble(IDatabaseAccess db, DatabaseStorage storage) {
            storage.EnterFluidMode();

            var bigLong = Int64.MinValue + 12345;
            var longID = storage.Store("foo", MakeRow("p", bigLong));
            
            storage.Store("foo", MakeRow("p", Math.PI));
            Assert.Equal(bigLong, db.Cell<long>(false, "select p from foo where id = {0}", longID));
        } 

        public static void CheckDateTimeQueries(IDatabaseAccess db, DatabaseStorage storage) {
            storage.EnterFluidMode();
            
            var dateTime = SAMPLE_DATETIME;
            var date = SAMPLE_DATETIME.Date;

            storage.Store("foo", MakeRow("d", date));
            storage.Store("foo", MakeRow("d", dateTime));
            Assert.Equal(2, db.Cell<int>(false, "select count(*) from foo where d = {0} or d = {1}", date, dateTime));

            db.Exec("delete from foo");

            for(var i = -2; i <= 2; i++)
                storage.Store("foo", MakeRow("d", date.AddDays(i)));
            Assert.Equal(3, db.Cell<int>(false, "select count(*) from foo where d between {0} and {1}", date.AddDays(-1), date.AddDays(1)));

            Assert.Equal(date, db.Cell<DateTime>(false, "select d from foo where d = {0}", date));        
        }

        public static void CheckGuidQuery(IDatabaseAccess db, DatabaseStorage storage) {
            storage.EnterFluidMode();
            storage.Store("foo", MakeRow("g", SAMPLE_GUID));
            Assert.Equal(SAMPLE_GUID, db.Cell<Guid>(false, "select g from foo where g = {0}", SAMPLE_GUID));
        }

        public static void CheckReadUncommitted(IDatabaseAccess db1, IDatabaseAccess db2) {
            db1.Exec("create table foo(f text)");
            db1.Exec("insert into foo(f) values('initial')");

            db1.Transaction(delegate() {
                db1.Exec("update foo set f='dirty'");

                db2.TransactionIsolation = IsolationLevel.ReadUncommitted;
                db2.Transaction(delegate() {
                    Assert.Equal("dirty", db2.Cell<string>(false, "select f from foo"));
                    return true;
                });

                return true;
            });
        }

        public static void CheckCustomRank_MissingColumn(IDatabaseAccess db, DatabaseStorage storage) {
            storage.EnterFluidMode();

            var row = MakeRow("a", new object());

            // Try create table
            AssertCannotAddColumn(Record.Exception(delegate() {
                storage.Store("foo1", row);
            }));

            storage.Store("foo2", MakeRow("b", 1));

            // Try add column
            AssertCannotAddColumn(Record.Exception(delegate() {
                storage.Store("foo2", row);
            }));
        }

        public static IDictionary<string, object> MakeRow(params object[] data) {
            var row = new Dictionary<string, object>();
            for(var i = 0; i < data.Length; i += 2) {
                row[(string)data[i]] = data[1 + i];
            }
            return row;
        }

        static void AssertCannotAddColumn(Exception x) {
            Assert.IsType<InvalidOperationException>(x);
            Assert.StartsWith("Cannot automatically add ", x.Message);
        }

    }

}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Xunit;

namespace LimeBean.Tests {

    static class SharedChecks {
        public static Guid SAMPLE_GUID = Guid.NewGuid();
        public static DateTime SAMPLE_DATETIME = new DateTime(1984, 6, 14, 11, 22, 33);
        public static DateTimeOffset SAMPLE_DATETIME_OFFSET = new DateTimeOffset(SAMPLE_DATETIME, TimeSpan.FromHours(6));

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

        public static void CheckDateTimeQueries(IDatabaseAccess db, DatabaseStorage storage) {
            storage.EnterFluidMode();
            
            var dateTime = SharedChecks.SAMPLE_DATETIME;
            var date = SharedChecks.SAMPLE_DATETIME.Date;

            storage.Store("foo", MakeRow("d", date));
            storage.Store("foo", MakeRow("d", dateTime));
            Assert.Equal(2, db.Cell<int>(false, "select count(*) from foo where d = {0} or d = {1}", date, dateTime));

            db.Exec("delete from foo");

            for(var i = -2; i <= 2; i++)
                storage.Store("foo", MakeRow("d", date.AddDays(i)));
            Assert.Equal(3, db.Cell<int>(false, "select count(*) from foo where d between {0} and {1}", date.AddDays(-1), date.AddDays(1)));

            Assert.Equal(date, db.Cell<DateTime>(false, "select d from foo where d = {0}", date));        
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

        public static void CheckCustomRankInFluidMode(IDatabaseAccess db, DatabaseStorage storage, bool expectSuccess) {
            storage.EnterFluidMode();

            var row = MakeRow("a", new byte[] { 1, 2, 3 });

            // Try create table

            var x = Record.Exception(delegate() {
                storage.Store("foo1", row);
            });
            
            if(expectSuccess) {
                Assert.Null(x);
            } else {
                Assert.IsType<InvalidOperationException>(x);
                Assert.EndsWith("custom SQL type", x.Message);
            }

            // Try add column

            storage.Store("foo2", MakeRow("b", 1));

            x = Record.Exception(delegate() {
                storage.Store("foo2", row);
            });

            if(expectSuccess) {
                Assert.Null(x);
            } else {
                Assert.IsType<InvalidOperationException>(x);
                Assert.EndsWith("custom SQL type", x.Message);
            }
            
            // Try to upgrade rank

            storage.Store("foo3", MakeRow("a", 1));

            x = Record.Exception(delegate() {
                storage.Store("foo3", row);
            });

            if(expectSuccess) {
                Assert.Null(x);
            } else {
                Assert.IsType<InvalidOperationException>(x);
                Assert.EndsWith("to '(custom)'", x.Message);
            }
        }

        public static void CheckStaticRankInFluidMode(IDatabaseAccess db, DatabaseStorage storage, object staticRankValue) {
            storage.EnterFluidMode();
            storage.Store("foo", MakeRow("dynamic", 1, "static", staticRankValue));   
         
            // dynamic ->  static
            
            var x = Record.Exception(delegate() {
                storage.Store("foo", MakeRow("dynamic", staticRankValue));    
            });
            Assert.IsType<InvalidOperationException>(x);
            Assert.StartsWith("Cannot automatically", x.Message);

            // static -> dynamic

            x = Record.Exception(delegate() {
                storage.Store("foo", MakeRow("static", 1));
            });

            Assert.IsType<InvalidOperationException>(x);
            Assert.StartsWith("Cannot automatically", x.Message);

        }

        public static void CheckCustomRankWithExistingTable(IDatabaseAccess db, DatabaseStorage storage, string blobType) {
            db.Exec("create table foo(id integer, b " + blobType + ")");
            storage.Store("foo", MakeRow("b", new byte[] { 123 }));
            var readBack = db.Cell<byte[]>(false, "select b from foo");
            Assert.Equal(123, readBack[0]);
        }

        static IDictionary<string, object> MakeRow(params object[] data) {
            var row = new Dictionary<string, object>();
            for(var i = 0; i < data.Length; i += 2) {
                row[(string)data[i]] = data[1 + i];
            }
            return row;
        }


    }

}

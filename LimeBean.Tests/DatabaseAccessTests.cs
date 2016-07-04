using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using Xunit;

using LimeBean.Interfaces;

namespace LimeBean.Tests {

    public class DatabaseAccessTests : IDisposable {
        DbConnection _conn;
        IDatabaseAccess _db;

        const string SELECT_RANDOM = "select hex(randomblob(16)) as r";

        public DatabaseAccessTests() {
            _conn = SQLitePortability.CreateConnection();
            _conn.Open();

            _db = new DatabaseAccess(_conn, new SQLiteDetails());
        }

        public void Dispose() {
            _conn.Dispose();        
        }

        [Fact]
        public void TypedReadsUseConvertSafe() {
            Assert.Equal(DayOfWeek.Thursday, _db.Cell<DayOfWeek?>(false, "select 4"));
        }

        [Fact]
        public void Caching_EnableDisable() {
            var readers = new Func<string, object>[] { 
                sql => _db.Cell<string>(true, sql),
                sql => _db.Col<string>(true, sql)[0],
                sql => _db.Row(true, sql)["r"],
                sql => _db.Rows(true, sql)[0]["r"],
            };

            foreach(var reader in readers) {
                _db.CacheCapacity = 1;
                var value = reader(SELECT_RANDOM);
                Assert.Equal(value, reader(SELECT_RANDOM));

                _db.CacheCapacity = 0;
                Assert.NotEqual(value, reader(SELECT_RANDOM));
            }
        }

        [Fact]
        public void Caching_SameQueriesDifferByFetchType() {
            var sql = "select 1";

            Assert.Null(Record.Exception(delegate() {
                _db.Col<int>(true, sql);
                _db.Cell<int>(true, sql);
                _db.Row(true, sql);
                _db.Rows(true, sql);
                _db.Exec(sql);
                _db.ColIterator<int>(sql).Last();
                _db.RowsIterator(sql).Last();            
            }));
        }

        [Fact]
        public void Caching_InvalidateByUpdate() {            
            _db.Exec("create table t1(a)");
            _db.Exec("insert into t1(a) values(42)");
            _db.Cell<int>(true, "select a from t1");
            _db.Exec("update t1 set a=82");
            Assert.Equal(82, _db.Cell<int>(true, "select a from t1"));
        }

        [Fact]
        public void UncachedRead() {
            var uuid = _db.Cell<string>(true, SELECT_RANDOM);
            Assert.Equal(uuid, _db.Cell<string>(true, SELECT_RANDOM));
            Assert.NotEqual(uuid, _db.Cell<string>(false, SELECT_RANDOM));

            Assert.NotEqual(uuid, _db.Cell<string>(true, SELECT_RANDOM)); // "No stale cache"
        }

        [Fact]
        public void CacheTrimming() {
            _db.Exec("create table foo(x)");

            var queryCount = 0;
            _db.QueryExecuting += cmd => queryCount++;
            _db.CacheCapacity = 3;

            var sql = "select * from foo where x = {0}";

            // fill up: 0, 1, 2
            for(var i = 0; i < 3; i++)
                _db.Row(true, sql, i);
            Assert.Equal(3, queryCount);

            // renew oldest entry: 1, 2, 0
            _db.Row(true, sql, 0);
            Assert.Equal(3, queryCount);
        
            // trim: 2, 0, 9
            _db.Row(true, sql, 9);
            Assert.Equal(4, queryCount);

            _db.Row(true, sql, 2);
            _db.Row(true, sql, 0);
            Assert.Equal(4, queryCount);

            _db.Row(true, sql, 1);
            Assert.Equal(5, queryCount);
        }

        [Fact]
        public void Transactions() {
            _db.Exec("create table t(c)");

            _db.Transaction(delegate() {
                _db.Exec("insert into t(c) values(1)");
                return false;
            });

            Assert.Equal(0, _db.Cell<int>(true, "select count(*) from t"));

            Assert.Throws<Exception>(delegate() {
                _db.Transaction(delegate() {
                    _db.Exec("insert into t(c) values(1)");
                    throw new Exception();
                });
            });

            Assert.Equal(0, _db.Cell<int>(true, "select count(*) from t"));

            _db.Transaction(delegate() {
                _db.Exec("insert into t(c) values(1)");
                return true;
            });

            Assert.Equal(1, _db.Cell<int>(true, "select count(*) from t"));
        }

        [Fact]
        public void InTransaction() {
            Assert.False(_db.InTransaction);

            _db.Transaction(delegate() {
                Assert.True(_db.InTransaction);
                return true;
            });

            _db.Transaction(delegate() {
                return false;
            });

            Assert.False(_db.InTransaction);

            try {
                _db.Transaction(delegate() {
                    throw new Exception();
                });
            } catch { 
            }

            Assert.False(_db.InTransaction);
        }

        [Fact]
        public void RolledBackTransactionClearsCache() {
            _db.Exec("create table foo(x)");
            _db.Exec("insert into foo(x) values(1)");

            _db.Transaction(delegate() {
                _db.Exec("update foo set x=2");
                _db.Cell<int>(true, "select x from foo");
                return false;
            });

            Assert.Equal(1, _db.Cell<int>(true, "select x from foo"));
        }

        [Fact]
        public void TransactionAssignedToCommand() {
            var trace = new List<DbTransaction>();

            _db.QueryExecuting += cmd => trace.Add(cmd.Transaction);

            _db.Transaction(delegate() {
                _db.Exec("select 1");
                return true;
            });

            Assert.NotNull(trace[0]);
        }
    }
}

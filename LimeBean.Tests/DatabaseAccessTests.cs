using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {

    [TestFixture]
    public class DatabaseAccessTests {
        IDbConnection _conn;
        IDatabaseAccess _db;

        [SetUp]
        public void SetUp() {
            _conn = new SQLiteConnection("data source=:memory:");
            _conn.Open();
            _db = new DatabaseAccess(_conn);
        }

        [TearDown]
        public void TearDown() {
            _conn.Dispose();
        }

        const string SELECT_RANDOM = "select hex(randomblob(16)) as r";

        [Test]
        public void Caching_EnableDisable() {
            var readers = new Func<string, IConvertible>[] { 
                sql => _db.Cell<string>(true, sql),
                sql => _db.Col<string>(true, sql)[0],
                sql => _db.Row(true, sql)["r"],
                sql => _db.Rows(true, sql)[0]["r"],
            };

            foreach(var reader in readers) {
                _db.CacheCapacity = 1;
                var value = reader(SELECT_RANDOM);
                Assert.AreEqual(value, reader(SELECT_RANDOM));

                _db.CacheCapacity = 0;
                Assert.AreNotEqual(value, reader(SELECT_RANDOM));
            }
        }

        [Test]
        public void Caching_SameQueriesDifferByFetchType() {
            var sql = "select 1";

            Assert.DoesNotThrow(delegate() {
                _db.Col<int>(true, sql);
                _db.Cell<int>(true, sql);
                _db.Row(true, sql);
                _db.Rows(true, sql);
                _db.Exec(sql);
                _db.ColIterator<int>(sql).Last();
                _db.RowsIterator(sql).Last();
            });        
        }

        [Test]
        public void Caching_InvalidateByUpdate() {            
            _db.Exec("create table t1(a)");
            _db.Exec("insert into t1(a) values(42)");
            _db.Cell<int>(true, "select a from t1");
            _db.Exec("update t1 set a=82");
            Assert.AreEqual(82, _db.Cell<int>(true, "select a from t1"));
        }

        [Test]
        public void UncachedRead() {
            var uuid = _db.Cell<string>(true, SELECT_RANDOM);
            Assert.AreEqual(uuid, _db.Cell<string>(true, SELECT_RANDOM));
            Assert.AreNotEqual(uuid, _db.Cell<string>(false, SELECT_RANDOM));

            Assert.AreNotEqual(uuid, _db.Cell<string>(true, SELECT_RANDOM), "No stale cache");
        }

        [Test]
        public void CacheTrimming() {
            _db.Exec("create table foo(x)");

            var queryCount = 0;
            _db.QueryExecuting += cmd => queryCount++;
            _db.CacheCapacity = 3;

            var sql = "select * from foo where x = ?";

            // fill up: 0, 1, 2
            for(var i = 0; i < 3; i++)
                _db.Row(true, sql, i);
            Assert.AreEqual(3, queryCount);

            // renew oldest entry: 1, 2, 0
            _db.Row(true, sql, 0);
            Assert.AreEqual(3, queryCount);
        
            // trim: 2, 0, 9
            _db.Row(true, sql, 9);
            Assert.AreEqual(4, queryCount);

            _db.Row(true, sql, 2);
            _db.Row(true, sql, 0);
            Assert.AreEqual(4, queryCount);

            _db.Row(true, sql, 1);
            Assert.AreEqual(5, queryCount);
        }

        [Test]
        public void Transactions() {
            _db.Exec("create table t(c)");

            _db.Transaction(delegate() {
                _db.Exec("insert into t(c) values(1)");
                return false;
            });

            Assert.AreEqual(0, _db.Cell<int>(true, "select count(*) from t"));

            Assert.Throws<Exception>(delegate() {
                _db.Transaction(delegate() {
                    _db.Exec("insert into t(c) values(1)");
                    throw new Exception();
                });
            });

            Assert.AreEqual(0, _db.Cell<int>(true, "select count(*) from t"));

            _db.Transaction(delegate() {
                _db.Exec("insert into t(c) values(1)");
                return true;
            });

            Assert.AreEqual(1, _db.Cell<int>(true, "select count(*) from t"));
        }

        [Test]
        public void InTransaction() {
            Assert.IsFalse(_db.InTransaction);

            _db.Transaction(delegate() {
                Assert.IsTrue(_db.InTransaction);

                _db.Transaction(delegate() {
                    Assert.IsTrue(_db.InTransaction);
                    return true;
                });

                Assert.IsTrue(_db.InTransaction);

                return true;
            });

            _db.Transaction(delegate() {
                return false;
            });

            Assert.IsFalse(_db.InTransaction);

            try {
                _db.Transaction(delegate() {
                    throw new Exception();
                });
            } catch { 
            }

            Assert.IsFalse(_db.InTransaction);
        }

        [Test]
        public void RolledBackTransactionClearsCache() {
            _db.Exec("create table foo(x)");
            _db.Exec("insert into foo(x) values(1)");

            _db.Transaction(delegate() {
                _db.Exec("update foo set x=2");
                _db.Cell<int>(true, "select x from foo");
                return false;
            });

            Assert.AreEqual(1, _db.Cell<int>(true, "select x from foo"));
        }
    }
}

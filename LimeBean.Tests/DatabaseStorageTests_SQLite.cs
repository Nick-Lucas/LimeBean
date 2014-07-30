using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {

    [TestFixture]
    public class DatabaseStorageTests_SQLite {
        IDbConnection _conn;
        IDatabaseAccess _db;
        DatabaseStorage _storage;

        [SetUp]
        public void SetUp() {
            _conn = new SQLiteConnection("data source=:memory:");
            _conn.Open();

            IDatabaseDetails details = new SQLiteDetails();

            _db = new DatabaseAccess(_conn, details);
            _storage = new DatabaseStorage(details, _db);
        }

        [TearDown]
        public void TearDown() {
            _conn.Dispose();
        }

        [Test]
        public void Schema() {
            _db.Exec(@"create table t (
                id int,

                a1,
                a2 LONGBLOB,

                t1 mediumtext,
                t2 CLOB,
                t3 VarChar(123),
                
                x1 charint,
                x2 numeric,
                x3 real,
                x4 eprst,
                
                x5 text not null,
                x6 text default 'a'
            )");

            var schema = _storage.GetSchema();
            Assert.AreEqual(1, schema.Count);

            var t = schema["t"];
            Assert.IsFalse(t.ContainsKey("id"));

            Assert.AreEqual(SQLiteDetails.RANK_ANY, t["a1"]);
            Assert.AreEqual(SQLiteDetails.RANK_ANY, t["a2"]);

            Assert.AreEqual(SQLiteDetails.RANK_TEXT, t["t1"]);
            Assert.AreEqual(SQLiteDetails.RANK_TEXT, t["t2"]);
            Assert.AreEqual(SQLiteDetails.RANK_TEXT, t["t3"]);

            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, t["x1"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, t["x2"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, t["x3"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, t["x4"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, t["x5"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, t["x6"]);
        }

        [Test]
        public void StoreToGoodTable() {
            _db.Exec("create table kind1 (" + Bean.ID_PROP_NAME + " integer primary key, p1 numeric, p2 text)");
            var id = _storage.Store("kind1", new Dictionary<string, IConvertible> { 
                { "p1", 123 },
                { "p2", "hello" }
            });

            var row = _db.Row(true, "select * from kind1");
            Assert.AreEqual(123, row["p1"]);
            Assert.AreEqual("hello", row["p2"]);
            Assert.AreEqual(id, row[Bean.ID_PROP_NAME]);

            Assert.AreEqual(id, _storage.Store("kind1", new Dictionary<string, IConvertible> { 
                { Bean.ID_PROP_NAME, id },
                { "p1", -1 },
                { "p2", "see you" }            
            }));

            row = _db.Row(true, "select * from kind1");
            Assert.AreEqual(-1, row["p1"]);
            Assert.AreEqual("see you", row["p2"]);
        }

        [Test]
        public void StoreToMissingTable() {
            _storage.EnterFluidMode();
            _storage.Store("kind1", new Dictionary<string, IConvertible> { 
                { "p1", 123 },
                { "p2", 3.14 },
                { "p3", "hello" },
                { "p4", null }
            });

            var row = _db.Row(true, "select * from kind1");
            Assert.AreEqual(123, row["p1"]);
            Assert.AreEqual(3.14, row["p2"]);
            Assert.AreEqual("hello", row["p3"]);
            Assert.AreEqual(null, row["p4"]);

            var table = _storage.GetSchema()["kind1"];
            Assert.AreEqual(SQLiteDetails.RANK_ANY, table["p1"]);
            Assert.AreEqual(SQLiteDetails.RANK_ANY, table["p2"]);
            Assert.AreEqual(SQLiteDetails.RANK_TEXT, table["p3"]);
            Assert.AreEqual(SQLiteDetails.RANK_ANY, table["p4"]);
        }

        [Test]
        public void ChangeSchemaOnStore() {
            _db.Exec("create table kind1 (" + Bean.ID_PROP_NAME + " integer primary key)");
            _storage.EnterFluidMode();

            _storage.Store("kind1", new Dictionary<string, IConvertible> { 
                { "x", 1 } 
            });

            var schema = _storage.GetSchema();
            Assert.AreEqual(SQLiteDetails.RANK_ANY, schema["kind1"]["x"]);

            _storage.Store("kind1", new Dictionary<string, IConvertible> { 
                { "x", "hello" },
                { "y", null }
            });

            schema = _storage.GetSchema();
            Assert.AreEqual(SQLiteDetails.RANK_TEXT, schema["kind1"]["x"]);
            Assert.AreEqual(SQLiteDetails.RANK_ANY, schema["kind1"]["y"]);

            var rows = _db.Rows(true, "select * from kind1 order by " + Bean.ID_PROP_NAME);
            Assert.AreEqual("1", rows[0]["x"]);
            Assert.AreEqual("hello", rows[1]["x"]);
        }

        [Test]
        public void ChangeSchemaWhenFrozen() {
            Assert.Throws<SQLiteException>(delegate() {
                _storage.Store("unlucky", new Dictionary<string, IConvertible> { { "a", 1 } });
            });
        }

        [Test]
        public void InsertUpdateWithoutValues() {
            _storage.EnterFluidMode();

            var id = _storage.Store("kind1", new Dictionary<string, IConvertible>());
            Assert.AreEqual(1, _db.Cell<int>(true, "select count(*) from kind1"));

            Assert.DoesNotThrow(delegate() {
                _storage.Store("kind1", new Dictionary<string, IConvertible>() { { Bean.ID_PROP_NAME, id } });
            });
        }

        [Test, ExpectedException(ExpectedMessage = "Row not found")]
        public void StoreWithKeyMissingFromDb() {
            _storage.EnterFluidMode();
            _storage.Store("foo", new Dictionary<string, IConvertible> { { Bean.ID_PROP_NAME, 123 }, { "a", 1 } });
        }

        [Test]
        public void LoadFromMissingTable() {
            Assert.Throws<SQLiteException>(delegate() {
                _storage.Load("phantom", 1);
            });

            _storage.EnterFluidMode();
            Assert.IsNull(_storage.Load("phantom", 1));
        }

        [Test]
        public void LoadMissingRow() {
            _db.Exec("create table kind1 (" + Bean.ID_PROP_NAME + " integer primary key)");
            Assert.IsNull(_storage.Load("kind1", 1));
        }

        [Test]
        public void Load() {
            _db.Exec("create table kind1 (" + Bean.ID_PROP_NAME + " integer primary key, p1 numeric, p2 text)");
            _db.Exec("insert into kind1 (" + Bean.ID_PROP_NAME + ", p1, p2) values (5, 123, 'hello')");

            var data = _storage.Load("kind1", 5);
            Assert.AreEqual(5, data[Bean.ID_PROP_NAME]);
            Assert.AreEqual(123, data["p1"]);
            Assert.AreEqual("hello", data["p2"]);
        }

        [Test, SetCulture("ru")]
        public void Roundtrip() {
            _storage.EnterFluidMode();
            var checker = new RoundtripChecker(_db, _storage);

            // native SQLite types
            checker.Check(null, null);
            checker.Check("hello", "hello");
            checker.Check(123L, 123L);
            checker.Check(3.14, 3.14);

            // extremal vaues
            checker.Check(Int64.MinValue, Int64.MinValue);
            checker.Check(Int64.MaxValue, Int64.MaxValue);
            checker.Check(Double.Epsilon, Double.Epsilon);
            checker.Check(Double.MinValue, Double.MinValue);
            checker.Check(Double.MaxValue, Double.MaxValue);

            // conversion to string
            checker.Check(9223372036854775808, "9223372036854775808");
            checker.Check(9223372036854775808M, "9223372036854775808");
            checker.Check(new DateTime(1984, 6, 14, 13, 14, 15), "06/14/1984 13:14:15");

            // upscale to long
            checker.Check(0, 0L);
            checker.Check(1, 1L);
            checker.Check(true, 1L);
            checker.Check(false, 0L);
            checker.Check(TypeCode.DateTime, 16L);
        }

        [Test]
        public void StringRelaxations() {
            _storage.EnterFluidMode();
            var checker = new RoundtripChecker(_db, _storage);

            checker.Check("", null);
            checker.Check(" \t", null);

            _storage.ConvertEmptyStringToNull = false;
            checker.Check(" \t", "");

            _storage.TrimStrings = false;
            checker.Check(" \t", " \t");
        }

        [Test]
        public void RegognizeIntegers() {
            _storage.EnterFluidMode();
            var checker = new RoundtripChecker(_db, _storage);

            checker.Check(123.0, 123L);
            checker.Check(123M, 123L);
            checker.Check(" 123 ", 123L);

            checker.Check("-0", "-0");
            checker.Check("+0", "+0");
            checker.Check("+1", "+1");
            checker.Check("00", "00");
            checker.Check("0123", "0123");
            checker.Check("0x123", "0x123");
            checker.Check(-1.0M, "-1.0");
            checker.Check("3.0e+5", "3.0e+5");

            _storage.TrimStrings = false;
            checker.Check(" 123 ", " 123 ");

            _storage.RecognizeIntegers = false;
            checker.Check("123", "123");
        }

        [Test]
        public void TrashFromMissingTable() {
            Assert.Throws<SQLiteException>(delegate() {
                _storage.Trash("kind1", 1);
            });

            Assert.DoesNotThrow(delegate() {
                _storage.EnterFluidMode();
                _storage.Trash("kind1", 1);
            });
        }

        [Test]
        public void Trash() {
            var emptiness = new Dictionary<string, IConvertible>();

            _storage.EnterFluidMode();

            var ids = new[] {
                _storage.Store("kind1", emptiness),
                _storage.Store("kind1", emptiness)
            };

            _storage.Trash("kind1", ids[0]);
            Assert.AreEqual(1, _db.Cell<int>(true, "select count(*) from kind1"));
        }

        [Test]
        public void Booleans() {
            _storage.EnterFluidMode();

            var trueKeys = new[] { 
                _storage.Store("foo", new Dictionary<string, IConvertible> { { "x", true } }),
                _storage.Store("foo", new Dictionary<string, IConvertible> { { "x", 1 } }),
                _storage.Store("foo", new Dictionary<string, IConvertible> { { "x", "1" } })
            };

            var falseKeys = new[] {
                _storage.Store("foo", new Dictionary<string, IConvertible> { { "x", false } }),
                _storage.Store("foo", new Dictionary<string, IConvertible> { { "x", 0 } }),
                _storage.Store("foo", new Dictionary<string, IConvertible> { { "x", "0" } })
            };

            var nullKeys = new[] { 
                _storage.Store("foo", new Dictionary<string, IConvertible> { { "x", null } }),
                _storage.Store("foo", new Dictionary<string, IConvertible> { { "x", "" } })
            };

            CollectionAssert.AreEquivalent(trueKeys, _db.Col<IConvertible>(true, "select " + Bean.ID_PROP_NAME + " from foo where x"));
            CollectionAssert.AreEquivalent(falseKeys, _db.Col<IConvertible>(true, "select " + Bean.ID_PROP_NAME + " from foo where not x"));
            CollectionAssert.AreEquivalent(nullKeys, _db.Col<IConvertible>(true, "select " + Bean.ID_PROP_NAME + " from foo where x is null"));
        }

        [Test]
        public void Numbers() {
            _storage.EnterFluidMode();

            var id = _storage.Store("foo", new Dictionary<string, IConvertible> { { "long", 42 }, { "double", 3.14 } });

            var row = _storage.Load("foo", id);
            Assert.IsInstanceOf<long>(row["long"]);
            Assert.IsInstanceOf<double>(row["double"]);

            // Expand long to real

            row["long"] = 3.14;
            _storage.Store("foo", row);

            row = _storage.Load("foo", id);
            Assert.IsInstanceOf<double>(row["long"]);

            // Store long in real column, type rank will not go back

            row["long"] = Int64.MaxValue;
            _storage.Store("foo", row);

            row = _storage.Load("foo", id);
            Assert.IsInstanceOf<long>(row["long"]);
            Assert.AreEqual(Int64.MaxValue, row["long"]);
        }

        [Test]
        public void BacktickInName() {
            Assert.Throws<ArgumentException>(delegate() {
                new SQLiteDetails().QuoteName("`");
            });
        }

        [Test]
        public void SchemaReadingKeepsCache() {
            _db.Exec("create table foo(bar)");
            _db.Exec("insert into foo(bar) values(1)");

            var queryCount = 0;
            _db.QueryExecuting += cmd => queryCount++;

            _db.Cell<int>(true, "SELECT * from foo");
            _storage.GetSchema();

            var savedQueryCount = queryCount;
            _db.Cell<int>(true, "SELECT * from foo");

            Assert.AreEqual(savedQueryCount, queryCount);
        }
    }

}

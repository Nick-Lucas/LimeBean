using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {

    [TestFixture]
    public class SQLiteStorageTests {
        IDbConnection _conn;
        IDatabaseAccess _db;
        DatabaseStorage _storage;

        [SetUp]
        public void SetUp() {
            _conn = new SQLiteConnection("data source=:memory:");
            _conn.Open();
            _db = new DatabaseAccess(_conn);
            _storage = new SQLiteStorage(_db);
        }

        [TearDown]
        public void TearDown() {
            _conn.Dispose();
        }

        [Test]
        public void Schema() {
            _db.Exec(@"create table t (
                " + Bean.ID_PROP_NAME + @" integer primary key, 
                n None, 
                t TEXT, 
                o            
            )");

            var schema = _storage.GetSchema();
            Assert.AreEqual(1, schema.Count);
            Assert.IsFalse(schema["t"].ContainsKey(Bean.ID_PROP_NAME));
            Assert.AreEqual(SQLiteStorage.RANK_NONE, schema["t"]["n"]);
            Assert.AreEqual(SQLiteStorage.RANK_TEXT, schema["t"]["t"]);
            Assert.AreEqual(SQLiteStorage.RANK_MAX, schema["t"]["o"]);
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
                { "p4", null },
                { "p5", "" }
            });

            var row = _db.Row(true, "select * from kind1");
            Assert.AreEqual(123, row["p1"]);
            Assert.AreEqual(3.14, row["p2"]);
            Assert.AreEqual("hello", row["p3"]);
            Assert.AreEqual(null, row["p4"]);
            Assert.AreEqual("", row["p5"]);

            var table = _storage.GetSchema()["kind1"];
            Assert.AreEqual(SQLiteStorage.RANK_NONE, table["p1"]);
            Assert.AreEqual(SQLiteStorage.RANK_NONE, table["p2"]);
            Assert.AreEqual(SQLiteStorage.RANK_TEXT, table["p3"]);
            Assert.AreEqual(SQLiteStorage.RANK_NONE, table["p4"]);
            Assert.AreEqual(SQLiteStorage.RANK_NONE, table["p5"]);
        }

        [Test]
        public void ChangeSchemaOnStore() {
            _db.Exec("create table kind1 (" + Bean.ID_PROP_NAME + " integer primary key)");
            _storage.EnterFluidMode();

            _storage.Store("kind1", new Dictionary<string, IConvertible> { 
                { "x", 1 } 
            });

            var schema = _storage.GetSchema();
            Assert.AreEqual(SQLiteStorage.RANK_NONE, schema["kind1"]["x"]);

            _storage.Store("kind1", new Dictionary<string, IConvertible> { 
                { "x", "hello" },
                { "y", null }
            });

            schema = _storage.GetSchema();
            Assert.AreEqual(SQLiteStorage.RANK_TEXT, schema["kind1"]["x"]);
            Assert.AreEqual(SQLiteStorage.RANK_NONE, schema["kind1"]["y"]);

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

        [Test, ExpectedException(ExpectedMessage="Row not found")]
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

            Action<IConvertible, IConvertible> check = (before, after) => {
                _db.Exec("drop table if exists kind1");
                _storage.InvalidateSchema();                

                var id = _storage.Store("kind1", new Dictionary<string, IConvertible> { 
                    { "p", before }
                });

                var loaded = _storage.Load("kind1", id);
                Assert.AreEqual(after, loaded["p"]);

                if(after != null)
                    Assert.AreEqual(after.GetType(), loaded["p"].GetType());
            };

            // native SQLite types
            check(null, null);
            check("hello", "hello");            
            check(Int64.MinValue, Int64.MinValue);
            check(Int64.MaxValue, Int64.MaxValue);
            check(Double.Epsilon, Double.Epsilon);
            check(Double.MinValue, Double.MinValue);
            check(Double.MaxValue, Double.MaxValue);

            // conversion to string
            check(9223372036854775808, "9223372036854775808");
            check(9223372036854775808M, "9223372036854775808");
            check(new DateTime(1984, 6, 14, 13, 14, 15), "06/14/1984 13:14:15");

            // upscale to long
            check(0, 0L);
            check(1, 1L);
            check(true, 1L);
            check(false, 0L);

            // downscale to long
            check("123", 123L);
            check(123.0, 123L);
            check(123uL, 123L);
            check(123M, 123L);
            check(TypeCode.DateTime, 16L);

            // safety
            check("", "");
            check(" ", " ");
            check("-0", "-0");
            check("00", "00");
            check("+123", "+123");
            check("123 ", "123 ");
            check(" 123", " 123");
            check("0123", "0123");
            check("0x123", "0x123");            
            check(-1.00M, "-1.00");
            check("3.0e+5", "3.0e+5");            
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
            var id1 = _storage.Store("kind1", emptiness);
            var id2 = _storage.Store("kind1", emptiness);

            _storage.Trash("kind1", id1);
            Assert.AreEqual(1, _db.Cell<int>(true, "select count(*) from kind1"));
        }

        [Test]
        public void Booleans() {
            _storage.EnterFluidMode();

            var trueKeys = new[] { 
                _storage.Store("foo", new Dictionary<string, IConvertible> { { "x", true } }),
                _storage.Store("foo", new Dictionary<string, IConvertible> { { "x", "1" } })
            };

            var falseKeys = new[] {
                _storage.Store("foo", new Dictionary<string, IConvertible> { { "x", false } }),
                _storage.Store("foo", new Dictionary<string, IConvertible> { { "x", "" } }),
                _storage.Store("foo", new Dictionary<string, IConvertible> { { "x", "0" } })
            };

            var nullKeys = new[] { 
                _storage.Store("foo", new Dictionary<string, IConvertible> { { "x", null } })
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
                _storage.QuoteName("`"); 
            });
        }
    }

}

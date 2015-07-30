﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using Xunit;

namespace LimeBean.Tests {

    public class DatabaseStorageTests_SQLite : IDisposable {
        DbConnection _conn;
        IDatabaseAccess _db;
        DatabaseStorage _storage;

        public DatabaseStorageTests_SQLite() {
            _conn = SQLitePortability.CreateConnection();
            _conn.Open();

            IDatabaseDetails details = new SQLiteDetails();

            _db = new DatabaseAccess(_conn, details);
            _storage = new DatabaseStorage(details, _db, new KeyUtil());
        }

        public void Dispose() {
            _conn.Dispose();
        }

        [Fact]
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
            Assert.Equal(1, schema.Count);

            var t = schema["t"];
            Assert.False(t.ContainsKey(Bean.ID_PROP_NAME));

            Assert.Equal(SQLiteDetails.RANK_ANY, t["a1"]);
            Assert.Equal(SQLiteDetails.RANK_ANY, t["a2"]);

            Assert.Equal(SQLiteDetails.RANK_TEXT, t["t1"]);
            Assert.Equal(SQLiteDetails.RANK_TEXT, t["t2"]);
            Assert.Equal(SQLiteDetails.RANK_TEXT, t["t3"]);

            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x1"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x2"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x3"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x4"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x5"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x6"]);
        }

        [Fact]
        public void StoreToGoodTable() {
            _db.Exec("create table kind1 (" + Bean.ID_PROP_NAME + " integer primary key, p1 numeric, p2 text)");
            var id = _storage.Store("kind1", new Dictionary<string, IConvertible> { 
                { "p1", 123 },
                { "p2", "hello" }
            });

            var row = _db.Row(true, "select * from kind1");
            Assert.Equal(123, row["p1"].ToInt32(null));
            Assert.Equal("hello", row["p2"]);
            Assert.Equal(id, row[Bean.ID_PROP_NAME]);

            Assert.Equal(id, _storage.Store("kind1", new Dictionary<string, IConvertible> { 
                { Bean.ID_PROP_NAME, id },
                { "p1", -1 },
                { "p2", "see you" }            
            }));

            row = _db.Row(true, "select * from kind1");
            Assert.Equal(-1, row["p1"].ToInt32(null));
            Assert.Equal("see you", row["p2"]);
        }

        [Fact]
        public void StoreToMissingTable() {
            _storage.EnterFluidMode();
            _storage.Store("kind1", new Dictionary<string, IConvertible> { 
                { "p1", 123 },
                { "p2", 3.14 },
                { "p3", "hello" },
                { "p4", null }
            });

            var row = _db.Row(true, "select * from kind1");
            Assert.Equal(123L, row["p1"]);
            Assert.Equal(3.14, row["p2"]);
            Assert.Equal("hello", row["p3"]);
            Assert.DoesNotContain("p4", row.Keys);

            var table = _storage.GetSchema()["kind1"];
            Assert.Equal(SQLiteDetails.RANK_ANY, table["p1"]);
            Assert.Equal(SQLiteDetails.RANK_ANY, table["p2"]);
            Assert.Equal(SQLiteDetails.RANK_TEXT, table["p3"]);
            Assert.DoesNotContain("p4", table.Keys);
        }

        [Fact]
        public void ChangeSchemaOnStore() {
            _db.Exec("create table kind1 (" + Bean.ID_PROP_NAME + " integer primary key)");
            _storage.EnterFluidMode();

            _storage.Store("kind1", new Dictionary<string, IConvertible> { 
                { "x", 1 } 
            });

            var schema = _storage.GetSchema();
            Assert.Equal(SQLiteDetails.RANK_ANY, schema["kind1"]["x"]);

            _storage.Store("kind1", new Dictionary<string, IConvertible> { 
                { "x", "hello" },
                { "y", null }
            });

            schema = _storage.GetSchema();
            Assert.Equal(SQLiteDetails.RANK_TEXT, schema["kind1"]["x"]);
            Assert.DoesNotContain("y", schema["kind1"].Keys);

            var rows = _db.Rows(true, "select * from kind1 order by " + Bean.ID_PROP_NAME);
            Assert.Equal("1", rows[0]["x"]);
            Assert.Equal("hello", rows[1]["x"]);
        }

        [Fact]
        public void ChangeSchemaWhenFrozen() {
            Assert.Throws(SQLitePortability.ExceptionType, delegate() {
                _storage.Store("unlucky", new Dictionary<string, IConvertible> { { "a", 1 } });
            });
        }

        [Fact]
        public void InsertUpdateWithoutValues() {
            _storage.EnterFluidMode();

            var id = _storage.Store("kind1", new Dictionary<string, IConvertible>());
            Assert.Equal(1, _db.Cell<int>(true, "select count(*) from kind1"));

            Assert.Null(Record.Exception(delegate() {
                _storage.Store("kind1", new Dictionary<string, IConvertible>() { { Bean.ID_PROP_NAME, id } });
            }));
        }

        [Fact]
        public void StoreWithKeyMissingFromDb() {
            _storage.EnterFluidMode();

            var error = Record.Exception(delegate() {
                _storage.Store("foo", new Dictionary<string, IConvertible> { { Bean.ID_PROP_NAME, 123 }, { "a", 1 } });
            });
            Assert.Equal("Row not found", error.Message);
        }

        [Fact]
        public void LoadFromMissingTable() {
            Assert.Throws(SQLitePortability.ExceptionType, delegate() {
                _storage.Load("phantom", 1);
            });

            _storage.EnterFluidMode();
            Assert.Null(_storage.Load("phantom", 1));
        }

        [Fact]
        public void LoadMissingRow() {
            _db.Exec("create table kind1 (" + Bean.ID_PROP_NAME + " integer primary key)");
            Assert.Null(_storage.Load("kind1", 1));
        }

        [Fact]
        public void Load() {
            _db.Exec("create table kind1 (" + Bean.ID_PROP_NAME + " integer primary key, p1 numeric, p2 text)");
            _db.Exec("insert into kind1 (" + Bean.ID_PROP_NAME + ", p1, p2) values (5, 123, 'hello')");

            var data = _storage.Load("kind1", 5);
            Assert.Equal(5L, data[Bean.ID_PROP_NAME]);
            Assert.Equal(123, data["p1"].ToInt32(null));
            Assert.Equal("hello", data["p2"]);
        }

        [Fact]
        public void Roundtrip() {
            AssertExtensions.WithCulture("ru", delegate() {
                _storage.EnterFluidMode();
                var checker = new RoundtripChecker(_db, _storage);

                // native SQLite types
                checker.Check(null, null);
                checker.Check("hello", "hello");
                checker.Check(123L, 123L);
                checker.Check(3.14, 3.14);

                // extremal vaues
                SharedChecks.CheckRoundtripOfExtremalValues(checker);

                // conversion to string
                SharedChecks.CheckRoundtripForcesString(checker);

                // upscale to long
                checker.Check(0, 0L);
                checker.Check(1, 1L);
                checker.Check(true, 1L);
                checker.Check(false, 0L);
                checker.Check(TypeCode.DateTime, 16L);
            });
        }

        [Fact]
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

        [Fact]
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

        [Fact]
        public void TrashFromMissingTable() {
            Assert.Throws(SQLitePortability.ExceptionType, delegate() {
                _storage.Trash("kind1", 1);
            });

            Assert.Null(Record.Exception(delegate() {
                _storage.EnterFluidMode();
                _storage.Trash("kind1", 1);            
            }));
        }

        [Fact]
        public void Trash() {
            var emptiness = new Dictionary<string, IConvertible>();

            _storage.EnterFluidMode();

            var ids = new[] {
                _storage.Store("kind1", emptiness),
                _storage.Store("kind1", emptiness)
            };

            _storage.Trash("kind1", ids[0]);
            Assert.Equal(1, _db.Cell<int>(true, "select count(*) from kind1"));
        }

        [Fact]
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

            AssertExtensions.Equivalent(trueKeys, _db.Col<IConvertible>(true, "select " + Bean.ID_PROP_NAME + " from foo where x"));
            AssertExtensions.Equivalent(falseKeys, _db.Col<IConvertible>(true, "select " + Bean.ID_PROP_NAME + " from foo where not x"));
            AssertExtensions.Equivalent(nullKeys, _db.Col<IConvertible>(true, "select " + Bean.ID_PROP_NAME + " from foo where x is null"));
        }

        [Fact]
        public void Numbers() {
            _storage.EnterFluidMode();

            var id = _storage.Store("foo", new Dictionary<string, IConvertible> { { "long", 42 }, { "double", 3.14 } });

            var row = _storage.Load("foo", id);
            Assert.IsType<long>(row["long"]);
            Assert.IsType<double>(row["double"]);

            // Expand long to real

            row["long"] = 3.14;
            _storage.Store("foo", row);

            row = _storage.Load("foo", id);
            Assert.IsType<double>(row["long"]);

            // Store long in real column, type rank will not go back

            row["long"] = Int64.MaxValue;
            _storage.Store("foo", row);

            row = _storage.Load("foo", id);
            Assert.IsType<long>(row["long"]);
            Assert.Equal(Int64.MaxValue, row["long"]);
        }

        [Fact]
        public void BacktickInName() {
            Assert.Throws<ArgumentException>(delegate() {
                new SQLiteDetails().QuoteName("`");
            });
        }

        [Fact]
        public void SchemaReadingKeepsCache() {
            SharedChecks.CheckSchemaReadingKeepsCache(_db, _storage);
        }

        [Fact]
        public void DateTimeQueries() {
            SharedChecks.CheckDateTimeQueries(_db, _storage);
        }
    }

}

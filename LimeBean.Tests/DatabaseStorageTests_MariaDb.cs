using MySql.Data.MySqlClient;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {

    [TestFixture]
    public class DatabaseStorageTests_MariaDb {
        static string TEMP_DB_NAME = "lime_bean_" + Guid.NewGuid().ToString("N");

        IDbConnection _conn;
        IDatabaseAccess _db;
        DatabaseStorage _storage;

        [TestFixtureSetUp]
        public void TestFixtureSetUp() {
            _conn = new MySqlConnection("server=localhost; uid=root; pwd=qwerty");
            _conn.Open();
        }

        [SetUp]
        public void SetUp() {
            IDatabaseDetails details = new MariaDbDetails();
            IDatabaseAccess db = new DatabaseAccess(_conn, details);            
            DatabaseStorage storage = new DatabaseStorage(details, db);

            db.Exec("set sql_mode=STRICT_TRANS_TABLES");
            db.Exec("create database " + TEMP_DB_NAME);
            db.Exec("use " + TEMP_DB_NAME);

            _db = db;
            _storage = storage;
        }

        [TearDown]
        public void TearDown() {
            _db.Exec("drop database if exists " + TEMP_DB_NAME);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown() {
            _conn.Dispose();
        }
        
        [Test]
        public void Schema() {
            _db.Exec(@"create table t(
                pk integer primary key,

                ti1 TinyInt(123),
                ti2 TINYINT,
                ti3 bool,
                ti4 Boolean, 

                i1  integer(123),
                i2  Integer,
                i3  INT,

                bi1 bigint(123),
                bi2 BIGINT,

                d1  Double,
                d2  DOUBLE PRECISION,

                t1  varchar(32),
                t2  VarChar(255),
                t3  TinyText,
                t4  Text,
                t5  MEDIUMTEXT,

                x1  smallint,
                x2  mediumint,
                x3  double(3,2),
                x4  float,
                x6  decimal,
                x7  date,
                x8  timestamp,
                x9  char(32),
                x10 varchar(123),
                x11 binary,
                x12 blob,
                x13 longtext,

                x14 int unsigned,
                x15 int not null,
                x16 int default '123'
            )");

            var schema = _storage.GetSchema();
            Assert.AreEqual(1, schema.Count);

            var t = schema["t"];
            Assert.IsFalse(t.ContainsKey("pk"));

            Assert.AreEqual(MariaDbDetails.RANK_INT8, t["ti1"]);
            Assert.AreEqual(MariaDbDetails.RANK_INT8, t["ti2"]);
            Assert.AreEqual(MariaDbDetails.RANK_INT8, t["ti3"]);
            Assert.AreEqual(MariaDbDetails.RANK_INT8, t["ti4"]);

            Assert.AreEqual(MariaDbDetails.RANK_INT32, t["i1"]);
            Assert.AreEqual(MariaDbDetails.RANK_INT32, t["i2"]);
            Assert.AreEqual(MariaDbDetails.RANK_INT32, t["i3"]);

            Assert.AreEqual(MariaDbDetails.RANK_INT64, t["bi1"]);
            Assert.AreEqual(MariaDbDetails.RANK_INT64, t["bi2"]);

            Assert.AreEqual(MariaDbDetails.RANK_DOUBLE, t["d1"]);
            Assert.AreEqual(MariaDbDetails.RANK_DOUBLE, t["d2"]);

            Assert.AreEqual(MariaDbDetails.RANK_TEXT5, t["t1"]);
            Assert.AreEqual(MariaDbDetails.RANK_TEXT8, t["t2"]);
            Assert.AreEqual(MariaDbDetails.RANK_TEXT8, t["t3"]);
            Assert.AreEqual(MariaDbDetails.RANK_TEXT16, t["t4"]);
            Assert.AreEqual(MariaDbDetails.RANK_TEXT24, t["t5"]);

            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, t["x1"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, t["x2"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, t["x3"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, t["x4"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, t["x6"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, t["x7"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, t["x8"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, t["x9"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, t["x10"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, t["x11"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, t["x12"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, t["x13"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, t["x14"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, t["x15"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, t["x16"]);
        }

        [Test]
        public void CreateTable() {
            _storage.EnterFluidMode();

            var data = new Dictionary<string, IConvertible> {
                { "p1", null },
                { "p2", 1 },
                { "p3", 1000 },
                { "p4", Int64.MaxValue },
                { "p5", 3.14 },
                { "p6", "abc" },
                { "p7", "".PadRight(33, 'a') },
                { "p8", "".PadRight(256, 'a') },
                { "p9", "".PadRight(65536, 'a') }
            };

            _storage.Store("foo", data);

            var cols = _storage.GetSchema()["foo"];
            Assert.AreEqual(MariaDbDetails.RANK_INT8, cols["p1"]);
            Assert.AreEqual(MariaDbDetails.RANK_INT8, cols["p2"]);
            Assert.AreEqual(MariaDbDetails.RANK_INT32, cols["p3"]);
            Assert.AreEqual(MariaDbDetails.RANK_INT64, cols["p4"]);
            Assert.AreEqual(MariaDbDetails.RANK_DOUBLE, cols["p5"]);
            Assert.AreEqual(MariaDbDetails.RANK_TEXT5, cols["p6"]);
            Assert.AreEqual(MariaDbDetails.RANK_TEXT8, cols["p7"]);
            Assert.AreEqual(MariaDbDetails.RANK_TEXT16, cols["p8"]);
            Assert.AreEqual(MariaDbDetails.RANK_TEXT24, cols["p9"]);
        }

        [Test]
        public void AlterTable() {
            _storage.EnterFluidMode();

            var data = new Dictionary<string, IConvertible> {
                { "p1", 1 },
                { "p2", 1000 },
                { "p3", Int64.MaxValue },
                { "p4", 3.14 },
                { "p5", "abc" },
                { "p6", "".PadRight(33, 'a') },
                { "p7", "".PadRight(256, 'a') },
                { "p8", "".PadRight(65536, 'a') }
            };

            _storage.Store("foo", data);

            for(var i = 1; i < 8; i++)
                data["p" + i] = data["p" + (i + 1)];

            data["p8"] = 123;
            data["p9"] = 123;

            _storage.Store("foo", data);

            var cols = _storage.GetSchema()["foo"];

            Assert.AreEqual(MariaDbDetails.RANK_INT32, cols["p1"]);
            Assert.AreEqual(MariaDbDetails.RANK_INT64, cols["p2"]);
            Assert.AreEqual(MariaDbDetails.RANK_DOUBLE, cols["p3"]);
            Assert.AreEqual(MariaDbDetails.RANK_TEXT5, cols["p4"]);
            Assert.AreEqual(MariaDbDetails.RANK_TEXT8, cols["p5"]);
            Assert.AreEqual(MariaDbDetails.RANK_TEXT16, cols["p6"]);
            Assert.AreEqual(MariaDbDetails.RANK_TEXT24, cols["p7"]);
            Assert.AreEqual(MariaDbDetails.RANK_TEXT24, cols["p8"]);
            Assert.AreEqual(MariaDbDetails.RANK_INT8, cols["p9"]);
        }

        [Test, SetCulture("ru")]
        public void Roundtrip() {
            _storage.EnterFluidMode();
            var checker = new RoundtripChecker(_db, _storage);

            // supported ranks
            checker.Check(null, null);
            checker.Check(1000, 1000);
            checker.Check((sbyte)123, (sbyte)123);
            checker.Check(0x80000000L, 0x80000000L);            
            checker.Check(3.14, 3.14);
            checker.Check("hello", "hello");

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

            // bool            
            checker.Check(true, (sbyte)1);
            checker.Check(false, (sbyte)0);

            // enum
            checker.Check(TypeCode.DateTime, (sbyte)16);
        }

        [Test]
        public void SchemaReadingKeepsCache() {
            _db.Exec("create table foo(bar int)");
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

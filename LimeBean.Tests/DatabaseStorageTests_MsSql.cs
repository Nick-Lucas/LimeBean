using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {

    [TestFixture]
    public class DatabaseStorageTests_MsSql {
        IDbConnection _conn;        
        IDatabaseAccess _db;
        DatabaseStorage _storage;

        List<string> _dropList = new List<string>();

        [TestFixtureSetUp]
        public void TestFixtureSetUp() {
            var server = Environment.GetEnvironmentVariable("LIME_TEST_SQLSERVER") ?? ".\\SQLEXPRESS";
            _conn = new SqlConnection("server=" + server + "; user instance=true; integrated security=true; connection timeout=90");
            _conn.Open();
        }

        [SetUp]
        public void SetUp() {
            IDatabaseDetails details = new MsSqlDetails();
            IDatabaseAccess db = new DatabaseAccess(_conn, details);
            DatabaseStorage storage = new DatabaseStorage(details, db);

            var name = "lime_bean_" + Guid.NewGuid().ToString("N");
            _dropList.Add(name);

            db.Exec("create database " + name);
            db.Exec("use " + name);

            _db = db;
            _storage = storage;
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown() {
            _db.Exec("use master");
            foreach(var name in _dropList)
                _db.Exec("drop database " + name);            

            _conn.Dispose();
        }


        [Test]
        public void Schema() {
            _db.Exec(@"create table foo(
                id  int,

                b   tinyint,
                i   int,
                l   bigint,
                d   float(53),
                t1  nvarchar(32),
                t2  nvarchar(4000),
                t3  nvarchar(MAX),

                x1  bit,
                x2  decimal,
                x3  float(10),
                x4  varchar(32),
                x5  nvarchar(33),

                x6  int not null,
                x7  int default 123
            )");

            var schema = _storage.GetSchema();
            Assert.AreEqual(1, schema.Count);

            var cols = schema["foo"];
            Assert.IsFalse(cols.ContainsKey("id"));

            Assert.AreEqual(MsSqlDetails.RANK_BYTE, cols["b"]);
            Assert.AreEqual(MsSqlDetails.RANK_INT32, cols["i"]);
            Assert.AreEqual(MsSqlDetails.RANK_INT64, cols["l"]);
            Assert.AreEqual(MsSqlDetails.RANK_DOUBLE, cols["d"]);
            Assert.AreEqual(MsSqlDetails.RANK_TEXT_32, cols["t1"]);
            Assert.AreEqual(MsSqlDetails.RANK_TEXT_4000, cols["t2"]);
            Assert.AreEqual(MsSqlDetails.RANK_TEXT_MAX, cols["t3"]);

            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, cols["x1"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, cols["x2"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, cols["x3"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, cols["x4"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, cols["x5"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, cols["x6"]);
            Assert.AreEqual(CommonDatabaseDetails.RANK_CUSTOM, cols["x7"]);
        }

        [Test]
        public void CreateTable() {
            _storage.EnterFluidMode();

            var data = new Dictionary<string, IConvertible> {
                { "p1", null },
                { "p2", 1 },
                { "p3", -1 },
                { "p4", Int64.MaxValue },
                { "p5", 3.14 },
                { "p6", "abc" },
                { "p7", "".PadRight(33, 'a') },
                { "p8", "".PadRight(4001, 'a') },
            };

            _storage.Store("foo", data);

            var cols = _storage.GetSchema()["foo"];
            Assert.AreEqual(MsSqlDetails.RANK_BYTE, cols["p1"]);
            Assert.AreEqual(MsSqlDetails.RANK_BYTE, cols["p2"]);
            Assert.AreEqual(MsSqlDetails.RANK_INT32, cols["p3"]);
            Assert.AreEqual(MsSqlDetails.RANK_INT64, cols["p4"]);
            Assert.AreEqual(MsSqlDetails.RANK_DOUBLE, cols["p5"]);
            Assert.AreEqual(MsSqlDetails.RANK_TEXT_32, cols["p6"]);
            Assert.AreEqual(MsSqlDetails.RANK_TEXT_4000, cols["p7"]);
            Assert.AreEqual(MsSqlDetails.RANK_TEXT_MAX, cols["p8"]);
        }

        [Test]
        public void AlterTable() {
            _storage.EnterFluidMode();

            var data = new Dictionary<string, IConvertible> {
                { "p1", 1 },
                { "p2", -1 },
                { "p3", Int64.MaxValue },
                { "p4", 3.14 },
                { "p5", "abc" },
                { "p6", "".PadRight(33, 'a') },
                { "p7", "".PadRight(4001, 'a') },
            };

            _storage.Store("foo", data);

            for(var i = 1; i < 7; i++)
                data["p" + i] = data["p" + (i + 1)];

            data["p7"] = 123;
            data["p8"] = 123;

            _storage.Store("foo", data);

            var cols = _storage.GetSchema()["foo"];
            Assert.AreEqual(MsSqlDetails.RANK_INT32, cols["p1"]);
            Assert.AreEqual(MsSqlDetails.RANK_INT64, cols["p2"]);
            Assert.AreEqual(MsSqlDetails.RANK_DOUBLE, cols["p3"]);
            Assert.AreEqual(MsSqlDetails.RANK_TEXT_32, cols["p4"]);
            Assert.AreEqual(MsSqlDetails.RANK_TEXT_4000, cols["p5"]);
            Assert.AreEqual(MsSqlDetails.RANK_TEXT_MAX, cols["p6"]);
            Assert.AreEqual(MsSqlDetails.RANK_TEXT_MAX, cols["p7"]);
            Assert.AreEqual(MsSqlDetails.RANK_BYTE, cols["p8"]);
        }

        [Test, SetCulture("ru")]
        public void Roundtrip() {
            _storage.EnterFluidMode();
            var checker = new RoundtripChecker(_db, _storage);

            // supported ranks
            checker.Check(null, null);
            checker.Check(255, (byte)255);
            checker.Check(1000, 1000);            
            checker.Check(0x80000000L, 0x80000000L);
            checker.Check(3.14, 3.14);
            checker.Check("hello", "hello");

            // extremal vaues
            checker.Check(Int64.MinValue, Int64.MinValue);
            checker.Check(Int64.MaxValue, Int64.MaxValue);
            checker.Check(Double.Epsilon, Double.Epsilon);
            checker.Check(Double.MinValue, Double.MinValue);
            checker.Check(Double.MaxValue, Double.MaxValue);
            checker.Check(RoundtripChecker.LONG_STRING, RoundtripChecker.LONG_STRING);

            // conversion to string
            checker.Check(9223372036854775808, "9223372036854775808");
            checker.Check(9223372036854775808M, "9223372036854775808");
            checker.Check(new DateTime(1984, 6, 14, 13, 14, 15), "06/14/1984 13:14:15");

            // bool            
            checker.Check(true, (byte)1);
            checker.Check(false, (byte)0);

            // enum
            checker.Check(TypeCode.DateTime, (byte)16);
        }

    }

}

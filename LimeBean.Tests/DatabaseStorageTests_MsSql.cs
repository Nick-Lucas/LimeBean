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
        static string TEMP_DB_NAME = "lime_bean_" + Guid.NewGuid().ToString("N");

        IDbConnection _conn;
        IDatabaseAccess _db;
        DatabaseStorage _storage;

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

            db.Exec("create database " + TEMP_DB_NAME);
            db.Exec("use " + TEMP_DB_NAME);

            _db = db;
            _storage = storage;
        }

        [TearDown]
        public void TearDown() {
            _db.Exec("use master");
            _db.Exec("drop database " + TEMP_DB_NAME);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown() {
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

    }

}

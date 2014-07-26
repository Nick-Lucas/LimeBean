using MySql.Data.MySqlClient;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {

    [TestFixture]
    public class MariaDbStorageTests {
        IDbConnection _conn;
        IDatabaseAccess _db;
        DatabaseStorage _storage;

        [TestFixtureSetUp]
        public void CommonSetUp() {
            _conn = new MySqlConnection("server=localhost; uid=root; pwd=qwerty");
            _conn.Open();

            _db = new DatabaseAccess(_conn);
            _db.Exec("set sql_mode=STRICT_TRANS_TABLES");
        }

        [SetUp]
        public void SetUp() {
            const string dbname = "lime_bean_tests";
            _db.Exec("drop database if exists " + dbname);
            _db.Exec("create database " + dbname);
            _db.Exec("use " + dbname);

            _storage = new MariaDbStorage(_db);
        }

        [TestFixtureTearDown]
        public void CommonTearDown() {
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

            Assert.AreEqual(MariaDbStorage.RANK_INT8, t["ti1"]);
            Assert.AreEqual(MariaDbStorage.RANK_INT8, t["ti2"]);
            Assert.AreEqual(MariaDbStorage.RANK_INT8, t["ti3"]);
            Assert.AreEqual(MariaDbStorage.RANK_INT8, t["ti4"]);

            Assert.AreEqual(MariaDbStorage.RANK_INT32, t["i1"]);
            Assert.AreEqual(MariaDbStorage.RANK_INT32, t["i2"]);
            Assert.AreEqual(MariaDbStorage.RANK_INT32, t["i3"]);

            Assert.AreEqual(MariaDbStorage.RANK_INT64, t["bi1"]);
            Assert.AreEqual(MariaDbStorage.RANK_INT64, t["bi2"]);

            Assert.AreEqual(MariaDbStorage.RANK_DOUBLE, t["d1"]);
            Assert.AreEqual(MariaDbStorage.RANK_DOUBLE, t["d2"]);

            Assert.AreEqual(MariaDbStorage.RANK_TEXT5, t["t1"]);
            Assert.AreEqual(MariaDbStorage.RANK_TEXT8, t["t2"]);
            Assert.AreEqual(MariaDbStorage.RANK_TEXT8, t["t3"]);
            Assert.AreEqual(MariaDbStorage.RANK_TEXT16, t["t4"]);
            Assert.AreEqual(MariaDbStorage.RANK_TEXT24, t["t5"]);

            Assert.AreEqual(MariaDbStorage.RANK_CUSTOM, t["x1"]);
            Assert.AreEqual(MariaDbStorage.RANK_CUSTOM, t["x2"]);
            Assert.AreEqual(MariaDbStorage.RANK_CUSTOM, t["x3"]);
            Assert.AreEqual(MariaDbStorage.RANK_CUSTOM, t["x4"]);
            Assert.AreEqual(MariaDbStorage.RANK_CUSTOM, t["x6"]);
            Assert.AreEqual(MariaDbStorage.RANK_CUSTOM, t["x7"]);
            Assert.AreEqual(MariaDbStorage.RANK_CUSTOM, t["x8"]);
            Assert.AreEqual(MariaDbStorage.RANK_CUSTOM, t["x9"]);
            Assert.AreEqual(MariaDbStorage.RANK_CUSTOM, t["x10"]);
            Assert.AreEqual(MariaDbStorage.RANK_CUSTOM, t["x11"]);
            Assert.AreEqual(MariaDbStorage.RANK_CUSTOM, t["x12"]);
            Assert.AreEqual(MariaDbStorage.RANK_CUSTOM, t["x13"]);
            Assert.AreEqual(MariaDbStorage.RANK_CUSTOM, t["x14"]);
            Assert.AreEqual(MariaDbStorage.RANK_CUSTOM, t["x15"]);
            Assert.AreEqual(MariaDbStorage.RANK_CUSTOM, t["x16"]);
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
            Assert.AreEqual(MariaDbStorage.RANK_INT8, cols["p1"]);
            Assert.AreEqual(MariaDbStorage.RANK_INT8, cols["p2"]);
            Assert.AreEqual(MariaDbStorage.RANK_INT32, cols["p3"]);
            Assert.AreEqual(MariaDbStorage.RANK_INT64, cols["p4"]);
            Assert.AreEqual(MariaDbStorage.RANK_DOUBLE, cols["p5"]);
            Assert.AreEqual(MariaDbStorage.RANK_TEXT5, cols["p6"]);
            Assert.AreEqual(MariaDbStorage.RANK_TEXT8, cols["p7"]);
            Assert.AreEqual(MariaDbStorage.RANK_TEXT16, cols["p8"]);
            Assert.AreEqual(MariaDbStorage.RANK_TEXT24, cols["p9"]);
        }
    }

}

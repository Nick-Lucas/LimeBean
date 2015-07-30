#if !NO_MARIADB
using LimeBean.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using Xunit;

namespace LimeBean.Tests {

    public class DatabaseStorageTests_MariaDb : IDisposable, IClassFixture<MariaDbConnectionFixture> {
        IDatabaseAccess _db;
        DatabaseStorage _storage;

        public DatabaseStorageTests_MariaDb(MariaDbConnectionFixture fixture) {
            IDatabaseDetails details = new MariaDbDetails();
            IDatabaseAccess db = new DatabaseAccess(fixture.Connection, details);            
            DatabaseStorage storage = new DatabaseStorage(details, db, new KeyUtil());

            TestEnv.MariaSetUp(db);

            _db = db;
            _storage = storage;
        }

        public void Dispose() {
            TestEnv.MariaTearDown(_db);
        }

        [Fact]
        public void Schema() {
            _db.Exec(@"create table t(
                id int,

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
            Assert.Equal(1, schema.Count);

            var t = schema["t"];
            Assert.False(t.ContainsKey(Bean.ID_PROP_NAME));

            Assert.Equal(MariaDbDetails.RANK_INT8, t["ti1"]);
            Assert.Equal(MariaDbDetails.RANK_INT8, t["ti2"]);
            Assert.Equal(MariaDbDetails.RANK_INT8, t["ti3"]);
            Assert.Equal(MariaDbDetails.RANK_INT8, t["ti4"]);

            Assert.Equal(MariaDbDetails.RANK_INT32, t["i1"]);
            Assert.Equal(MariaDbDetails.RANK_INT32, t["i2"]);
            Assert.Equal(MariaDbDetails.RANK_INT32, t["i3"]);

            Assert.Equal(MariaDbDetails.RANK_INT64, t["bi1"]);
            Assert.Equal(MariaDbDetails.RANK_INT64, t["bi2"]);

            Assert.Equal(MariaDbDetails.RANK_DOUBLE, t["d1"]);
            Assert.Equal(MariaDbDetails.RANK_DOUBLE, t["d2"]);

            Assert.Equal(MariaDbDetails.RANK_TEXT5, t["t1"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT8, t["t2"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT8, t["t3"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT16, t["t4"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT24, t["t5"]);

            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x1"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x2"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x3"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x4"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x6"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x7"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x8"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x9"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x10"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x11"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x12"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x13"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x14"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x15"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x16"]);
        }

        [Fact]
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
            Assert.DoesNotContain("p1", cols.Keys);
            Assert.Equal(MariaDbDetails.RANK_INT8, cols["p2"]);
            Assert.Equal(MariaDbDetails.RANK_INT32, cols["p3"]);
            Assert.Equal(MariaDbDetails.RANK_INT64, cols["p4"]);
            Assert.Equal(MariaDbDetails.RANK_DOUBLE, cols["p5"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT5, cols["p6"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT8, cols["p7"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT16, cols["p8"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT24, cols["p9"]);
        }

        [Fact]
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

            Assert.Equal(MariaDbDetails.RANK_INT32, cols["p1"]);
            Assert.Equal(MariaDbDetails.RANK_INT64, cols["p2"]);
            Assert.Equal(MariaDbDetails.RANK_DOUBLE, cols["p3"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT5, cols["p4"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT8, cols["p5"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT16, cols["p6"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT24, cols["p7"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT24, cols["p8"]);
            Assert.Equal(MariaDbDetails.RANK_INT8, cols["p9"]);
        }

        [Fact]
        public void Roundtrip() {
            AssertExtensions.WithCulture("ru", delegate() {
                _storage.EnterFluidMode();
                var checker = new RoundtripChecker(_db, _storage);

                // supported ranks
                checker.Check(null, null);
                checker.Check((sbyte)123, (sbyte)123);
                checker.Check(1000, 1000);
                checker.Check(0x80000000L, 0x80000000L);
                checker.Check(3.14, 3.14);
                checker.Check("hello", "hello");

                // extremal vaues
                SharedChecks.CheckRoundtripOfExtremalValues(checker);

                // conversion to string
                SharedChecks.CheckRoundtripForcesString(checker);

                // bool            
                checker.Check(true, (sbyte)1);
                checker.Check(false, (sbyte)0);

                // enum
                checker.Check(TypeCode.DateTime, (sbyte)16);
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
#endif
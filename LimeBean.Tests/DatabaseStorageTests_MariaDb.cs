#if !NO_MARIADB
using LimeBean.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using Xunit;

namespace LimeBean.Tests {

    public class DatabaseStorageTests_MariaDb : IDisposable, IClassFixture<MariaDbConnectionFixture> {
        ConnectionFixture _fixture;
        IDatabaseAccess _db;
        DatabaseStorage _storage;

        public DatabaseStorageTests_MariaDb(MariaDbConnectionFixture fixture) {
            _fixture = fixture;
            _fixture.SetUpDatabase();

            IDatabaseDetails details = new MariaDbDetails();
            IDatabaseAccess db = new DatabaseAccess(_fixture.Connection, details);            
            DatabaseStorage storage = new DatabaseStorage(details, db, new KeyUtil());

            _db = db;
            _storage = storage;
        }

        public void Dispose() {
            _fixture.TearDownDatabase();
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

                t1  varchar(36),
                t2  VarChar(191),
                t3  LongText,

                dt1 datetime,                

                x1  smallint,
                x2  mediumint,
                x3  double(3,2),
                x4  float,
                x5  decimal,
                x6  date,
                x7  timestamp,
                x8  char(36),
                x9 varchar(123),
                x10 binary,
                x11 blob,
                x12 text,

                x13 int unsigned,
                x14 int not null,
                x15 int default '123'
            )");

            var schema = _storage.GetSchema();
            Assert.Equal(1, schema.Count);

            var t = schema["t"];
            Assert.False(t.ContainsKey("id"));

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

            Assert.Equal(MariaDbDetails.RANK_TEXT_36, t["t1"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_191, t["t2"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_MAX, t["t3"]);

            Assert.Equal(MariaDbDetails.RANK_STATIC_DATETIME, t["dt1"]);

            foreach(var i in Enumerable.Range(1, 15))
                Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x" + i]);
        }

        [Fact]
        public void CreateTable() {
            _storage.EnterFluidMode();

            var data = new Dictionary<string, object> {
                { "p1", null },
                { "p2", 1 },
                { "p3", 1000 },
                { "p4", Int64.MaxValue },
                { "p5", 3.14 },
                { "p6", "abc" },
                { "p7", "".PadRight(37, 'a') },
                { "p8", "".PadRight(192, 'a') },
                { "p9", DateTime.Now }
            };

            _storage.Store("foo", data);

            var cols = _storage.GetSchema()["foo"];
            Assert.DoesNotContain("p1", cols.Keys);
            Assert.Equal(MariaDbDetails.RANK_INT8, cols["p2"]);
            Assert.Equal(MariaDbDetails.RANK_INT32, cols["p3"]);
            Assert.Equal(MariaDbDetails.RANK_INT64, cols["p4"]);
            Assert.Equal(MariaDbDetails.RANK_DOUBLE, cols["p5"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_36, cols["p6"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_191, cols["p7"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_MAX, cols["p8"]);
            Assert.Equal(MariaDbDetails.RANK_STATIC_DATETIME, cols["p9"]);
        }

        [Fact]
        public void AlterTable() {
            _storage.EnterFluidMode();

            var data = new Dictionary<string, object> {
                { "p1", 1 },
                { "p2", 1000 },
                { "p3", Int64.MaxValue },
                { "p4", 3.14 },
                { "p5", "abc" },
                { "p6", "".PadRight(37, 'a') },
                { "p7", "".PadRight(192, 'a') }
            };

            _storage.Store("foo", data);

            for(var i = 1; i < data.Count; i++)
                data["p" + i] = data["p" + (i + 1)];

            data["p7"] = 123;
            data["p8"] = 123;

            _storage.Store("foo", data);

            var cols = _storage.GetSchema()["foo"];

            Assert.Equal(MariaDbDetails.RANK_INT32, cols["p1"]);
            Assert.Equal(MariaDbDetails.RANK_INT64, cols["p2"]);
            Assert.Equal(MariaDbDetails.RANK_DOUBLE, cols["p3"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_36, cols["p4"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_191, cols["p5"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_MAX, cols["p6"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_MAX, cols["p7"]);
            Assert.Equal(MariaDbDetails.RANK_INT8, cols["p8"]);
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
                checker.Check(SharedChecks.SAMPLE_DATETIME, SharedChecks.SAMPLE_DATETIME);

                // extremal vaues
                SharedChecks.CheckRoundtripOfExtremalValues(checker, checkDateTime: true);

                // conversion to string
                SharedChecks.CheckBigNumberRoundtripForcesString(checker);
                checker.Check(SharedChecks.SAMPLE_GUID, SharedChecks.SAMPLE_GUID.ToString());

                // bool            
                checker.Check(true, (sbyte)1);
                checker.Check(false, (sbyte)0);

                // enum
                checker.Check(DayOfWeek.Thursday, (sbyte)4);
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

        [Fact]
        public void CustomRankInFluidMode() {
            SharedChecks.CheckCustomRankInFluidMode(_db, _storage, false);
        }

        [Fact]
        public void CustomRankWithExistingTable() {
            SharedChecks.CheckCustomRankWithExistingTable(_db, _storage, "tinyblob");
        }

        [Fact]
        public void StaticRankInFluidMode() {
            SharedChecks.CheckStaticRankInFluidMode(_db, _storage, DateTime.Now);
        }

        [Fact]
        public void TransactionIsolation() {
            Assert.Equal(IsolationLevel.Unspecified, _db.TransactionIsolation);

            using(var otherFixture = new MariaDbConnectionFixture()) {
                var dbName = _db.Cell<string>(false, "select database()");
                var otherDb = new DatabaseAccess(otherFixture.Connection, null);

                otherDb.Exec("use " + dbName);
                SharedChecks.CheckReadUncommitted(_db, otherDb);
            }
        }

        [Fact]
        public void UTF8_mb4() {
            const string pile = "\U0001f4a9";
            _storage.EnterFluidMode();
            var id = _storage.Store("foo", SharedChecks.MakeRow("p", pile));
            Assert.Equal(pile, _storage.Load("foo", id)["p"]);
        }
    }

}
#endif
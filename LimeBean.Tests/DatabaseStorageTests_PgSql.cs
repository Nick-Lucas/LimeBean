#if !NO_PGSQL
using LimeBean.Tests.Fixtures;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

// NOTE http://www.roryhart.net/code/slow-create-database-with-postgresql/

namespace LimeBean.Tests {

    public class DatabaseStorageTests_PgSql : IDisposable, IClassFixture<PgSqlConnectionFixture> {
        ConnectionFixture _fixture;
        IDatabaseAccess _db;
        DatabaseStorage _storage;

        public DatabaseStorageTests_PgSql(PgSqlConnectionFixture fixture) {
            _fixture = fixture;
            _fixture.SetUpDatabase();

            var details = new PgSqlDetails();
            var db = new DatabaseAccess(_fixture.Connection, details);
            var storage = new DatabaseStorage(details, db, new KeyUtil());

            _db = db;
            _storage = storage;
        }

        public void Dispose() {
            _fixture.TearDownDatabase();
        }

        [Fact]
        public void Schema() {
            _db.Exec(@"create table foo(
                id  serial,

                b   boolean,
                i   Integer,
                l   BigInt,
                d   DOUBLE precision,
                n   Numeric,
                t   text,
                ts  timestamp,                
                tz  timestamptz,
                g   uuid,
                bl  bytea,

                b2  BOOL,
                i2  INT4,
                l2  INT8,
                d2  FLOAT8,
                ts2 timestamp without time zone,
                tz2 timestamp with time zone,

                x1  integer not null,
                x2  integer default 123,
                x3  numeric(10),
                x4  numeric(10, 5),

                x5  json,
                x6  varchar(255)
            )");

            var cols = _storage.GetSchema()["foo"];
            Assert.False(cols.ContainsKey("id"));

            Assert.Equal(PgSqlDetails.RANK_BOOLEAN, cols["b"]);
            Assert.Equal(PgSqlDetails.RANK_BOOLEAN, cols["b2"]);

            Assert.Equal(PgSqlDetails.RANK_INT32, cols["i"]);
            Assert.Equal(PgSqlDetails.RANK_INT32, cols["i2"]);

            Assert.Equal(PgSqlDetails.RANK_INT64, cols["l"]);
            Assert.Equal(PgSqlDetails.RANK_INT64, cols["l2"]);

            Assert.Equal(PgSqlDetails.RANK_DOUBLE, cols["d"]);
            Assert.Equal(PgSqlDetails.RANK_DOUBLE, cols["d2"]);

            Assert.Equal(PgSqlDetails.RANK_NUMERIC, cols["n"]);
            Assert.Equal(PgSqlDetails.RANK_TEXT, cols["t"]);

            Assert.Equal(PgSqlDetails.RANK_STATIC_DATETIME, cols["ts"]);
            Assert.Equal(PgSqlDetails.RANK_STATIC_DATETIME, cols["ts2"]);

            Assert.Equal(PgSqlDetails.RANK_STATIC_DATETIME_OFFSET, cols["tz"]);
            Assert.Equal(PgSqlDetails.RANK_STATIC_DATETIME_OFFSET, cols["tz2"]);

            Assert.Equal(PgSqlDetails.RANK_STATIC_GUID, cols["g"]);

            Assert.Equal(PgSqlDetails.RANK_STATIC_BLOB, cols["bl"]);

            foreach(var i in Enumerable.Range(1, 6))
                Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, cols["x" + i]);
        }

        [Fact]
        public void CreateTable() {
            _storage.EnterFluidMode();

            var data = new Dictionary<string, object> {
                { "p1", null },
                { "p2", true },
                { "p3", 1 },
                { "p4", Int64.MaxValue },
                { "p5", 3.14 },
                { "p6", Decimal.MaxValue },
                { "p7", UInt64.MaxValue },
                { "p8", "abc" },
                { "p9", DateTime.Now },
                { "p10", DateTimeOffset.Now },
                { "p11", Guid.NewGuid() },
                { "p12", new byte[0] }
            };

            _storage.Store("foo", data);

            var cols = _storage.GetSchema()["foo"];
            Assert.DoesNotContain("p1", cols.Keys);
            Assert.Equal(PgSqlDetails.RANK_BOOLEAN, cols["p2"]);
            Assert.Equal(PgSqlDetails.RANK_INT32, cols["p3"]);
            Assert.Equal(PgSqlDetails.RANK_INT64, cols["p4"]);
            Assert.Equal(PgSqlDetails.RANK_DOUBLE, cols["p5"]);
            Assert.Equal(PgSqlDetails.RANK_NUMERIC, cols["p6"]);
            Assert.Equal(PgSqlDetails.RANK_NUMERIC, cols["p7"]);
            Assert.Equal(PgSqlDetails.RANK_TEXT, cols["p8"]);
            Assert.Equal(PgSqlDetails.RANK_STATIC_DATETIME, cols["p9"]);
            Assert.Equal(PgSqlDetails.RANK_STATIC_DATETIME_OFFSET, cols["p10"]);
            Assert.Equal(PgSqlDetails.RANK_STATIC_GUID, cols["p11"]);
            Assert.Equal(PgSqlDetails.RANK_STATIC_BLOB, cols["p12"]);
        }

        [Fact]
        public void AlterTable() {
            _storage.EnterFluidMode();

            var data = new Dictionary<string, object> {
                { "p1", true },
                { "p2", 1 },
                { "p3", 1 + (long)Int32.MaxValue },
                { "p4", 3.14 }, 
                { "p5", Decimal.MaxValue },
                { "p6", "abc" }
            };

            _storage.Store("foo", data);

            for(var i = 1; i < data.Count; i++)
                data["p" + i] = data["p" + (1 + i)];

            data["p6"] = 1;
            data["p7"] = 1;

            _storage.Store("foo", data);

            var cols = _storage.GetSchema()["foo"];
            Assert.Equal(PgSqlDetails.RANK_INT32, cols["p1"]);
            Assert.Equal(PgSqlDetails.RANK_INT64, cols["p2"]);
            Assert.Equal(PgSqlDetails.RANK_DOUBLE, cols["p3"]);
            Assert.Equal(PgSqlDetails.RANK_NUMERIC, cols["p4"]);
            Assert.Equal(PgSqlDetails.RANK_TEXT, cols["p6"]);
            Assert.Equal(PgSqlDetails.RANK_INT32, cols["p7"]);
        }

        [Fact]
        public void AlterTable_BoolToInteger() {
            _db.Exec("create table foo(id serial, p bool)");
            _db.Exec("insert into foo (p) values (null)");
            _db.Exec("insert into foo (p) values (false)");
            _db.Exec("insert into foo (p) values (true)");            

            _storage.EnterFluidMode();
            _storage.Store("foo", new Dictionary<string, object> { { "p", 2 } });

            var col = _db.Col<string>(false, "select p from foo order by id");
            Assert.Equal(new[] { null, "0", "1", "2" }, col);
        }

        [Fact]
        public void LongToDouble() {
            SharedChecks.CheckLongToDouble(_db, _storage);
        }

        [Fact]
        public void Roundtrip() {
            AssertExtensions.WithCulture("ru", delegate() {
                _storage.EnterFluidMode();
                var checker = new RoundtripChecker(_db, _storage);

                // supported ranks
                checker.Check(null, null);
                checker.Check(true, true);
                checker.Check(1000, 1000);
                checker.Check(0x80000000L, 0x80000000L);
                checker.Check(3.14, 3.14);
                checker.Check(7.90M, 7.90M);
                checker.Check("hello", "hello");
                checker.Check(SharedChecks.SAMPLE_DATETIME, SharedChecks.SAMPLE_DATETIME);
                checker.Check(SharedChecks.SAMPLE_GUID, SharedChecks.SAMPLE_GUID);
                checker.Check(SharedChecks.SAMPLE_BLOB, SharedChecks.SAMPLE_BLOB);

                // https://github.com/npgsql/npgsql/issues/11
                checker.Check(
                    SharedChecks.SAMPLE_DATETIME_OFFSET, 
                    SharedChecks.SAMPLE_DATETIME_OFFSET.ToLocalTime().DateTime
                );

                // extremal vaues
                SharedChecks.CheckRoundtripOfExtremalValues(checker, checkDecimal: true, checkDateTime: true);
                checker.Check(UInt64.MaxValue, (decimal)UInt64.MaxValue);

                // enum
                checker.Check(DayOfWeek.Thursday, (int)4);
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
        public void GuidQuery() {
            SharedChecks.CheckGuidQuery(_db, _storage);
        }

        [Fact]
        public void CustomRank_MissingColumn() {
            SharedChecks.CheckCustomRank_MissingColumn(_db, _storage);
        }

        [Fact]
        public void CustomRank_ExistingColumn() {
            _db.Exec("create table foo(id int, p point)");
            _storage.Store("foo", SharedChecks.MakeRow("p", new NpgsqlPoint(54.2, 37.61667)));
            Assert.Equal(54.2, _db.Cell<NpgsqlPoint>(false, "select p from foo").X);
        }

    }

}
#endif
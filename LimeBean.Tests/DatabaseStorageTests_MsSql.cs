#if !NO_MSSQL
using LimeBean.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Xunit;

namespace LimeBean.Tests {

    [Trait("db", "mssql")]
    public class DatabaseStorageTests_MsSql : IClassFixture<MsSqlConnectionFixture> {
        IDatabaseAccess _db;
        DatabaseStorage _storage;

        public DatabaseStorageTests_MsSql(MsSqlConnectionFixture fixture) {
            IDatabaseDetails details = new MsSqlDetails();
            IDatabaseAccess db = new DatabaseAccess(fixture.Connection, details);
            DatabaseStorage storage = new DatabaseStorage(details, db, new KeyUtil());

            TestEnv.MsSqlSetUp(db, fixture.DropList);

            _db = db;
            _storage = storage;
        }

        [Fact]
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
            Assert.Equal(1, schema.Count);

            var cols = schema["foo"];
            Assert.False(cols.ContainsKey("id"));

            Assert.Equal(MsSqlDetails.RANK_BYTE, cols["b"]);
            Assert.Equal(MsSqlDetails.RANK_INT32, cols["i"]);
            Assert.Equal(MsSqlDetails.RANK_INT64, cols["l"]);
            Assert.Equal(MsSqlDetails.RANK_DOUBLE, cols["d"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_32, cols["t1"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_4000, cols["t2"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_MAX, cols["t3"]);

            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, cols["x1"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, cols["x2"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, cols["x3"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, cols["x4"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, cols["x5"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, cols["x6"]);
            Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, cols["x7"]);
        }

        [Fact]
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
            Assert.DoesNotContain("p1", cols.Keys);
            Assert.Equal(MsSqlDetails.RANK_BYTE, cols["p2"]);
            Assert.Equal(MsSqlDetails.RANK_INT32, cols["p3"]);
            Assert.Equal(MsSqlDetails.RANK_INT64, cols["p4"]);
            Assert.Equal(MsSqlDetails.RANK_DOUBLE, cols["p5"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_32, cols["p6"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_4000, cols["p7"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_MAX, cols["p8"]);
        }

        [Fact]
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
            Assert.Equal(MsSqlDetails.RANK_INT32, cols["p1"]);
            Assert.Equal(MsSqlDetails.RANK_INT64, cols["p2"]);
            Assert.Equal(MsSqlDetails.RANK_DOUBLE, cols["p3"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_32, cols["p4"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_4000, cols["p5"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_MAX, cols["p6"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_MAX, cols["p7"]);
            Assert.Equal(MsSqlDetails.RANK_BYTE, cols["p8"]);
        }

        [Fact]
        public void Roundtrip() {
            AssertExtensions.WithCulture("ru", delegate() {
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
                SharedChecks.CheckRoundtripOfExtremalValues(checker);

                // conversion to string
                SharedChecks.CheckRoundtripForcesString(checker);

                // bool            
                checker.Check(true, (byte)1);
                checker.Check(false, (byte)0);

                // enum
                checker.Check(TypeCode.DateTime, (byte)16);
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
        public void Blobs() {
            SharedChecks.CheckBlobs(_db, "varbinary(16)");
        }

        [Fact]
        public void ReadNonConvertibles() {
            var guid = Guid.NewGuid();

            _db.Exec("create table foo(f uniqueidentifier)");
            _db.Exec("insert into foo(f) values({0})", guid);

            Assert.Equal(guid.ToString(), _db.Cell<string>(false, "select f from foo"));
        }

        [Fact]
        public void TransactionIsolation() {
            Assert.Equal(IsolationLevel.Unspecified, _db.TransactionIsolation);

            using(var otherConnection = new SqlConnection(TestEnv.MsSqlConnectionString)) {
                otherConnection.Open();                
                
                var dbName = _db.Cell<string>(false, "select db_name()");
                var otherDb = new DatabaseAccess(otherConnection, null);

                otherDb.Exec("use " + dbName);
                try {
                    SharedChecks.CheckReadUncommitted(_db, otherDb);
                } finally {
                    otherDb.Exec("use master");
                }
            }
        }
    }

}
#endif
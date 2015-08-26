using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using Xunit;

namespace LimeBean.Tests {

    public class DatabaseBeanFinderTests : IDisposable {
        DbConnection _conn;
        IDatabaseAccess _db;
        IBeanFinder _finder;

        public DatabaseBeanFinderTests() {
            _conn = SQLitePortability.CreateConnection();
            _conn.Open();

            IDatabaseDetails details = new SQLiteDetails();           
            IDatabaseAccess db = new DatabaseAccess(_conn, details);
            IKeyAccess keys = new KeyUtil();
            IStorage storage = new DatabaseStorage(details, db, keys);
            IBeanCrud crud = new BeanCrud(storage, db, keys);
            IBeanFinder finder = new DatabaseBeanFinder(details, db, crud);

            db.Exec("create table foo(x)");
            db.Exec("insert into foo(x) values(1)");
            db.Exec("insert into foo(x) values(2)");
            db.Exec("insert into foo(x) values(3)");

            _db = db;
            _finder = finder;
        }

        public void Dispose() {
            _conn.Dispose();        
        }

        [Fact]
        public void Find() {
            Assert.Equal(3, _finder.Find(true, "foo").Count());
            Assert.Equal(3, _finder.Find<Foo>(true).Count());

            Assert.Equal(2, _finder.Find(true, "foo", "where x in ({0}, {1})", 1, 3).Count());
            Assert.Equal(1, _finder.Find<Foo>(true, "where x={0}", 3).Count());

            Assert.Empty(_finder.Find(true, "foo", "where x is null"));
            Assert.Empty(_finder.Find<Foo>(true, "where x is {0}", null));
        }

        [Fact]
        public void FindOne() {
            Assert.Equal(1L, _finder.FindOne(true, "foo", "order by x")["x"]);
            Assert.Equal(3L, _finder.FindOne<Foo>(true, "order by x desc")["x"]);

            Assert.Equal(2L, _finder.FindOne(true, "foo", "where x={0}", 2)["x"]);
            Assert.Equal(2L, _finder.FindOne<Foo>(true, "where x > {0} and x < {1}", 1, 3)["x"]);

            Assert.Null(_finder.FindOne(true, "foo", "where 0"));
            Assert.Null(_finder.FindOne<Foo>(true, "where x > {0}", 100));
        }

        [Fact]
        public void Caching() {
            var queryCount = 0;
            _db.QueryExecuting += cmd => queryCount++;

            _finder.Find(true, "foo", "where x > 2");
            _finder.Find<Foo>(true, "where x > 2");
            _finder.Find(true, "foo", "where x > 2");
            _finder.Find<Foo>(true, "where x > 2");

            Assert.Equal(1, queryCount);

            _finder.Find(false, "foo", "where x > 2");
            _finder.Find<Foo>(false, "where x > 2");

            Assert.Equal(3, queryCount);

            queryCount = 0;

            _finder.FindOne(true, "foo", "where x={0}", 1);
            _finder.FindOne<Foo>(true, "where x={0}", 1);
            _finder.FindOne(true, "foo", "where x={0}", 1);
            _finder.FindOne<Foo>(true, "where x={0}", 1);


            Assert.Equal(1, queryCount);

            _finder.FindOne(false, "foo", "where x={0}", 1);
            _finder.FindOne<Foo>(false, "where x={0}", 1);

            Assert.Equal(3, queryCount);
        }

        [Fact]
        public void Iterators() {
            AssertExtensions.Equivalent(new object[] { 1L, 3L }, _finder.FindIterator("foo", "where x <> {0}", 2).Select(b => b["x"]));
            AssertExtensions.Equivalent(new object[] { 1L, 3L }, _finder.FindIterator<Foo>("where x <> {0}", 2).Select(b => b["x"]));
        }

        [Fact]
        public void Count() {
            var queryCount = 0;
            _db.QueryExecuting += cmd => queryCount++;

            Assert.Equal(2, _finder.Count(true, "foo", "where x <> {0}", 2));
            Assert.Equal(2, _finder.Count<Foo>(true, "where x <> {0}", 2));
            Assert.Equal(2, _finder.Count(false, "foo", "where x <> {0}", 2));
            Assert.Equal(2, _finder.Count<Foo>(false, "where x <> {0}", 2));
            Assert.Equal(3, queryCount);
        }

        
        class Foo : Bean {
            public Foo()
                : base("foo") {
            }
        }

    }

}

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {

    [TestFixture]
    class DatabaseBeanFinderTests {
        IDbConnection _conn;
        IDatabaseAccess _db;
        IBeanFinder _finder;

        [SetUp]
        public void SetUp() {
            _conn = new SQLiteConnection("data source=:memory:");
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

        [TearDown]
        public void TestFixtureTearDown() {
            _conn.Dispose();
        }

        [Test]
        public void Find() {
            Assert.AreEqual(3, _finder.Find(true, "foo").Count());
            Assert.AreEqual(3, _finder.Find<Foo>(true).Count());

            Assert.AreEqual(2, _finder.Find(true, "foo", "where x in (?, ?)", 1, 3).Count());
            Assert.AreEqual(1, _finder.Find<Foo>(true, "where x={0}", 3).Count());

            Assert.IsEmpty(_finder.Find(true, "foo", "where x is null"));
            Assert.IsEmpty(_finder.Find<Foo>(true, "where x is ?", null));
        }

        [Test]
        public void FindOne() {
            Assert.AreEqual(1, _finder.FindOne(true, "foo", "order by x")["x"]);
            Assert.AreEqual(3, _finder.FindOne<Foo>(true, "order by x desc")["x"]);

            Assert.AreEqual(2, _finder.FindOne(true, "foo", "where x=?", 2)["x"]);
            Assert.AreEqual(2, _finder.FindOne<Foo>(true, "where x > {0} and x < {1}", 1, 3)["x"]);

            Assert.IsNull(_finder.FindOne(true, "foo", "where 0"));
            Assert.IsNull(_finder.FindOne<Foo>(true, "where x > ?", 100));
        }

        [Test]
        public void Caching() {
            var queryCount = 0;
            _db.QueryExecuting += cmd => queryCount++;

            _finder.Find(true, "foo", "where x > 2");
            _finder.Find<Foo>(true, "where x > 2");
            _finder.Find(true, "foo", "where x > 2");
            _finder.Find<Foo>(true, "where x > 2");

            Assert.AreEqual(1, queryCount);

            _finder.Find(false, "foo", "where x > 2");
            _finder.Find<Foo>(false, "where x > 2");

            Assert.AreEqual(3, queryCount);

            queryCount = 0;

            _finder.FindOne(true, "foo", "where x=?", 1);
            _finder.FindOne<Foo>(true, "where x=?", 1);
            _finder.FindOne(true, "foo", "where x=?", 1);
            _finder.FindOne<Foo>(true, "where x=?", 1);


            Assert.AreEqual(1, queryCount);

            _finder.FindOne(false, "foo", "where x=?", 1);
            _finder.FindOne<Foo>(false, "where x=?", 1);

            Assert.AreEqual(3, queryCount);
        }

        [Test]
        public void Iterators() {
            CollectionAssert.AreEquivalent(new[] { 1, 3 }, _finder.FindIterator("foo", "where x <> ?", 2).Select(b => b["x"]));
            CollectionAssert.AreEquivalent(new[] { 1, 3 }, _finder.FindIterator<Foo>("where x <> ?", 2).Select(b => b["x"]));
        }

        [Test]
        public void Count() {
            var queryCount = 0;
            _db.QueryExecuting += cmd => queryCount++;

            Assert.AreEqual(2, _finder.Count(true, "foo", "where x <> ?", 2));
            Assert.AreEqual(2, _finder.Count<Foo>(true, "where x <> ?", 2));
            Assert.AreEqual(2, _finder.Count(false, "foo", "where x <> ?", 2));
            Assert.AreEqual(2, _finder.Count<Foo>(false, "where x <> ?", 2));
            Assert.AreEqual(3, queryCount);
        }

        
        class Foo : Bean {
            public Foo()
                : base("foo") {
            }
        }

    }

}

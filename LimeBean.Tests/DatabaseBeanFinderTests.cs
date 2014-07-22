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

            _db = new DatabaseAccess(_conn);

            DatabaseStorage storage = new SQLiteStorage(_db);
            IBeanCrud crud = new BeanCrud(storage, _db);
            _finder = new DatabaseBeanFinder(storage, _db, crud);

            _db.Exec("create table foo(x)");
            _db.Exec("insert into foo(x) values(1)");
            _db.Exec("insert into foo(x) values(2)");
            _db.Exec("insert into foo(x) values(3)");
        }

        [TearDown]
        public void TearDown() {
            _conn.Dispose();
        }

        [Test]
        public void Find() {
            Assert.AreEqual(3, _finder.Find("foo").Count());
            Assert.AreEqual(3, _finder.Find<Foo>().Count());

            Assert.AreEqual(2, _finder.Find("foo", "where x in (?, ?)", 1, 3).Count());
            Assert.AreEqual(1, _finder.Find<Foo>("where x=@x", new { x = 3 }).Count());

            Assert.IsEmpty(_finder.Find("foo", "where x is null"));
            Assert.IsEmpty(_finder.Find<Foo>("where x is ?", null));
        }

        [Test]
        public void FindOne() {
            Assert.AreEqual(1, _finder.FindOne("foo", "order by x")["x"]);
            Assert.AreEqual(3, _finder.FindOne<Foo>("order by x desc")["x"]);

            Assert.AreEqual(2, _finder.FindOne("foo", "where x=?", 2)["x"]);
            Assert.AreEqual(2, _finder.FindOne<Foo>("where x > @from and x < @to", new { from = 1, to = 3 })["x"]);

            Assert.IsNull(_finder.FindOne("foo", "where 0"));
            Assert.IsNull(_finder.FindOne<Foo>("where x > ?", 100));
        }

        [Test]
        public void Caching() {
            var queryCount = 0;
            _db.QueryExecuting += cmd => queryCount++;

            _finder.Find("foo", "where x > 2");
            _finder.Find<Foo>("where x > 2");
            _finder.Find("foo", "where x > 2");
            _finder.Find<Foo>("where x > 2");

            Assert.AreEqual(1, queryCount);

            _finder.Find(false, "foo", "where x > 2");
            _finder.Find<Foo>(false, "where x > 2");

            Assert.AreEqual(3, queryCount);

            queryCount = 0;

            _finder.FindOne("foo", "where x=?", 1);
            _finder.FindOne<Foo>("where x=?", 1);
            _finder.FindOne("foo", "where x=?", 1);
            _finder.FindOne<Foo>("where x=?", 1);


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

            Assert.AreEqual(2, _finder.Count("foo", "where x <> ?", 2));
            Assert.AreEqual(2, _finder.Count<Foo>("where x <> ?", 2));
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

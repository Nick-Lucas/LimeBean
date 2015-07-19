using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {

    [TestFixture]
    public class DirtyTrackingTests {
        BeanApi _api;
        Bean _bean;
        int _queryCount;

        [SetUp]
        public void SetUp() {
            _api = new BeanApi("data source=:memory:", SQLiteFactory.Instance);
            _api.Exec("create table foo(id, a, b)");
            _api.Exec("insert into foo values(1, 'initial', 'initial')");

            _bean = _api.Load("foo", 1);

            _api.QueryExecuting += cmd => _queryCount++;
        }

        [Test]
        public void Default_NoChangedProps() {
            _bean["a"] = "temp";
            _bean["a"] = "initial";
            _api.Store(_bean);
            Assert.AreEqual(0, _queryCount);
        }

        [Test]
        public void Disabled_NoChangedProps() {
            _api.DirtyTracking = false;
            _bean["a"] = "temp";
            _bean["a"] = "initial";
            _api.Store(_bean);
            Assert.That(_queryCount, Is.GreaterThan(0));
        }

        [Test]
        public void Default_OnlyDirtyPropsWritten() {
            _bean["a"] = "bean change";
            _api.Exec("update foo set b='external change'");
            _api.Store(_bean);
            Assert.AreEqual("external change", _api.Cell<string>("select b from foo"));
        }

        [Test]
        public void Disabled_AllPropsWritten() {
            _api.DirtyTracking = false;
            _bean["a"] = "bean change";
            _api.Exec("update foo set b='external change'");
            _api.Store(_bean);
            Assert.AreEqual("initial", _api.Cell<string>("select b from foo"));        
        }

        [TearDown]
        public void TearDown() {
            _api.Dispose();
        }

    }

}

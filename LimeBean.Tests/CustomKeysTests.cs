using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {

    [TestFixture]
    public class CustomKeysTests {
        BeanApi _api;

        [SetUp]
        public void SetUp() { 
            _api = new BeanApi("data source=:memory:", SQLiteFactory.Instance);
        }

        [TearDown]
        public void TearDown() {
            _api.Dispose();
        }


        [Test]
        public void Assigned_FrozenMode() {
            _api.Exec("create table foo (pk, prop)");
            _api.Key("foo", "pk", false);

            var bean = _api.Dispense("foo");
            bean["pk"] = "pk1";
            bean["prop"] = "value1";

            var key = _api.Store(bean);
            Assert.AreEqual("pk1", key);

            bean["prop"] = "value2";
            Assert.AreEqual(key, _api.Store(bean));

            bean = _api.Load("foo", key);
            Assert.AreEqual(key, bean["pk"]);
            Assert.AreEqual("value2", bean["prop"]);

            _api.Trash(bean);
            Assert.AreEqual(0, _api.Count("foo"));  
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void Assigned_Null() {
            _api.Key("foo", "any", false);
            _api.Store(_api.Dispense("foo"));
        }

        [Test]
        public void Assigned_Modification() {
            _api.EnterFluidMode();
            _api.Key("foo", "pk", false);

            var bean = _api.Dispense("foo");
            bean["pk"] = 1;
            _api.Store(bean);

            bean["pk"] = 2;
            _api.Store(bean);

            Assert.AreEqual(2, _api.Count("foo"));
            // moral: keys should be immutable
        }

        [Test]
        public void AssignedCompound_FrozenMode() {
            _api.Exec("create table foo (pk1, pk2, pk3, prop)");
            _api.Key("foo", "pk1", "pk2", "pk3");

            var bean = _api.Dispense("foo");
            bean["pk1"] = 1;
            bean["pk2"] = 2;
            bean["pk3"] = 3;
            bean["prop"] = "value1";

            var key = _api.Store(bean) as CompoundKey;
            Assert.AreEqual("pk1=1, pk2=2, pk3=3", key.ToString());

            bean["prop"] = "value2";
            Assert.AreEqual("pk1=1, pk2=2, pk3=3", _api.Store(bean).ToString());

            bean = _api.Load("foo", key);
            Assert.AreEqual(1, bean["pk1"]);
            Assert.AreEqual(2, bean["pk2"]);
            Assert.AreEqual(3, bean["pk3"]);
            Assert.AreEqual("value2", bean["prop"]);

            _api.Trash(bean);
            Assert.AreEqual(0, _api.Count("foo"));        
        }

        [Test]
        public void CustomAutoIncrement_FluidMode() {
            _api.EnterFluidMode();
            _api.Key("foo", "custom field");
            _api.Store(_api.Dispense("foo"));

            Assert.AreEqual(1, _api.Count("foo", "where [custom field]=1"));
        }

        [Test]
        public void AssignedCompound_FluidMode() {
            _api.EnterFluidMode();
            _api.Key("foo", "a", "b");

            var bean = _api.Dispense("foo");
            bean["a"] = 1;
            bean["b"] = 2;
            _api.Store(bean);

            Assert.AreEqual(1, _api.Count("foo", "where a=1 and b=2"));
        }

        [Test]
        public void AssignedCompound_Load() {
            _api.Exec("create table foo (k1, k2, v)");
            _api.Exec("insert into foo values (1, 'a', 'ok')");
            _api.Key("foo", "k1", "k2");

            var bean = _api.Load("foo", 1, "a");
            Assert.AreEqual("ok", bean["v"]);
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void AssignedCompound_Malformed() {
            _api.EnterFluidMode();
            _api.Key("foo", "k1", "k2");

            var bean = _api.Dispense("foo");
            bean["k1"] = 1;
            _api.Store(bean);
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void AssignedCompound_Empty() {
            _api.Key("foo");
        }

    }


}

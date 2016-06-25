using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace LimeBean.Tests {

    public class CustomKeysTests : IDisposable {
        BeanApi _api;

        public CustomKeysTests() {
            _api = SQLitePortability.CreateApi();
        }

        public void Dispose() {
            _api.Dispose();        
        }

        [Fact]
        public void Assigned_FrozenMode() {
            _api.Exec("create table foo (pk, prop)");
            _api.Key("foo", "pk", false);

            var bean = _api.Dispense("foo");
            bean["pk"] = "pk1";
            bean["prop"] = "value1";

            var key = _api.Store(bean);
            Assert.Equal("pk1", key);

            bean["prop"] = "value2";
            Assert.Equal(key, _api.Store(bean));

            bean = _api.Load("foo", key);
            Assert.Equal(key, bean["pk"]);
            Assert.Equal("value2", bean["prop"]);

            _api.Trash(bean);
            Assert.Equal(0, _api.Count("foo"));  
        }

        [Fact]
        public void Assigned_Null() {
            _api.Key("foo", "any", false);
            Assert.Throws<InvalidOperationException>(delegate() {                
                _api.Store(_api.Dispense("foo"));
            });
        }

        [Fact]
        public void Assigned_Modification() {
            _api.EnterFluidMode();
            _api.Key("foo", "pk", false);

            var bean = _api.Dispense("foo");
            bean["pk"] = 1;
            _api.Store(bean);

            bean["pk"] = 2;
            _api.Store(bean);

            Assert.Equal(2, _api.Count("foo"));
            // moral: keys should be immutable
        }

        [Fact]
        public void AssignedCompound_FrozenMode() {
            _api.Exec("create table foo (pk1, pk2, pk3, prop)");
            _api.Key("foo", "pk1", "pk2", "pk3");

            var bean = _api.Dispense("foo");
            bean["pk1"] = 1;
            bean["pk2"] = 2;
            bean["pk3"] = 3;
            bean["prop"] = "value1";

            var key = _api.Store(bean) as CompoundKey;
            Assert.Equal("pk1=1, pk2=2, pk3=3", key.ToString());

            bean["prop"] = "value2";
            Assert.Equal("pk1=1, pk2=2, pk3=3", _api.Store(bean).ToString());

            bean = _api.Load("foo", key);
            Assert.Equal(1L, bean["pk1"]);
            Assert.Equal(2L, bean["pk2"]);
            Assert.Equal(3L, bean["pk3"]);
            Assert.Equal("value2", bean["prop"]);

            _api.Trash(bean);
            Assert.Equal(0, _api.Count("foo"));        
        }

        [Fact]
        public void CustomAutoIncrement_FluidMode() {
            _api.EnterFluidMode();
            _api.Key("foo", "custom field");
            _api.Store(_api.Dispense("foo"));

            Assert.Equal(1, _api.Count("foo", "where [custom field]=1"));
        }

        [Fact]
        public void AssignedCompound_FluidMode() {
            _api.EnterFluidMode();
            _api.Key("foo", "a", "b");

            var bean = _api.Dispense("foo");
            bean["a"] = 1;
            bean["b"] = 2;
            _api.Store(bean);

            Assert.Equal(1, _api.Count("foo", "where a=1 and b=2"));
        }

        [Fact]
        public void AssignedCompound_Load() {
            _api.Exec("create table foo (k1, k2, v)");
            _api.Exec("insert into foo values (1, 'a', 'ok')");
            _api.Key("foo", "k1", "k2");

            var bean = _api.Load("foo", 1, "a");
            Assert.Equal("ok", bean["v"]);
        }

        [Fact]
        public void AssignedCompound_Malformed() {
            _api.EnterFluidMode();
            _api.Key("foo", "k1", "k2");

            var bean = _api.Dispense("foo");
            bean["k1"] = 1;

            Assert.Throws<ArgumentNullException>(delegate() {
                _api.Store(bean);
            });
        }

        [Fact]
        public void AssignedCompound_Empty() {
            Assert.Throws<ArgumentException>(delegate() {
                _api.Key("foo");
            });
        }

    }


}

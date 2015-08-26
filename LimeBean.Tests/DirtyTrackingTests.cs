using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace LimeBean.Tests {

    public class DirtyTrackingTests : IDisposable {
        BeanApi _api;
        Bean _bean;
        int _queryCount;

        public DirtyTrackingTests() {
            _api = SQLitePortability.CreateApi();
            _api.Exec("create table foo(id, a, b)");
            _api.Exec("insert into foo values(1, 'initial', 'initial')");

            _bean = _api.Load("foo", 1);

            _api.QueryExecuting += cmd => _queryCount++;
        }

        [Fact]
        public void Default_NoChangedProps() {
            _bean["a"] = "temp";
            _bean["a"] = "initial";
            _api.Store(_bean);
            Assert.Equal(0, _queryCount);
        }

        [Fact]
        public void Disabled_NoChangedProps() {
            _api.DirtyTracking = false;
            _bean["a"] = "temp";
            _bean["a"] = "initial";
            _api.Store(_bean);
            Assert.True(_queryCount > 0);
        }

        [Fact]
        public void Default_OnlyDirtyPropsWritten() {
            _bean["a"] = "bean change";
            _api.Exec("update foo set b='external change'");
            _api.Store(_bean);
            Assert.Equal("external change", _api.Cell<string>("select b from foo"));
        }

        [Fact]
        public void Disabled_AllPropsWritten() {
            _api.DirtyTracking = false;
            _bean["a"] = "bean change";
            _api.Exec("update foo set b='external change'");
            _api.Store(_bean);
            Assert.Equal("initial", _api.Cell<string>("select b from foo"));        
        }

        public void Dispose() {
            _api.Dispose();
        }

    }

}

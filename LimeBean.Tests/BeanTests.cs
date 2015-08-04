using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace LimeBean.Tests {
    
    public class BeanTests {

        [Fact]
        public void Indexer() {
            var bean = new Bean();

            Assert.Null(bean["x"]);

            bean["x"] = "123";
            Assert.Equal("123", bean["x"]);

            bean["x"] = null;
            Assert.Null(bean["x"]);
        }

        [Fact]
        public void TypedAccessors() {
            var bean = new Bean();

            Assert.Equal(0, bean.Get<int>("x"));
            Assert.Null(bean.GetNullable<int>("x"));

            bean.Put("x", 0);
            Assert.Equal(0, bean.Get<int>("x"));
            Assert.Equal(0, bean.GetNullable<int>("x"));

            bean.Put("x", null);
            Assert.Equal(0, bean.Get<int>("x"));
            Assert.Null(bean.GetNullable<int>("x"));

            bean.Put("x", new Nullable<int>(1));
            Assert.Equal(1, bean.Get<int>("x"));
        }

        [Fact]
        public void TypedAccessors_Conversion() {
            AssertExtensions.WithCulture("ru", delegate() {
                var bean = new Bean();

                bean.Put("x", "3.14");
                Assert.Equal(3.14, bean.GetNullable<double>("x"));
                Assert.Equal(3.14M, bean.Get<decimal>("x"));

                bean.Put("x", "abc");
                Assert.Equal(0, bean.Get<int>("x"));
                Assert.Null(bean.GetNullable<int>("x"));
            });
        }

        [Fact]
        public void TypedAccessors_Enums() {
            var bean = new Bean();

            bean.Put("x", TypeCode.String);
            Assert.Equal(TypeCode.String, bean.Get<TypeCode>("x"));

            bean.Put("x", "OBJECT");
            Assert.Equal(TypeCode.Object, bean.Get<TypeCode>("x"));

            bean.Put("x", (ulong)TypeCode.DateTime);
            Assert.Equal(TypeCode.DateTime, bean.Get<TypeCode>("x"));

            bean.Put("x", "?");
            Assert.Equal(default(TypeCode), bean.Get<TypeCode>("x"));
        }

        [Fact]
        public void TypedAccessors_Dates() {
            var bean = new Bean();

            bean.Put("x", new DateTime(2011, 11, 11));
            Assert.Equal(2011, bean.Get<DateTime>("x").Year);

            bean.Put("x", "2012-12-12");
            Assert.Equal(2012, bean.Get<DateTime>("x").Year);

            bean.Put("x", "?");
            Assert.Equal(new DateTime(), bean.Get<DateTime>("x"));
        }

        [Fact]
        public void Kind() { 
            var bean = new Bean("kind1");
            Assert.Equal("kind1", bean.GetKind());
        }

        [Fact]
        public void ToStringMethod() {
            Assert.Equal("LimeBean.Bean", new Bean().ToString());
            Assert.Equal("product", new Bean("product").ToString());
        }

        [Fact]
        public void Export() {
            var bean = new Bean();
            bean["id"] = 123;
            bean.Put("a", 1).Put("b", "abc");
            AssertExtensions.Equivalent(bean.Export(), new Dictionary<string, IConvertible> { 
                { "id", 123 }, { "a", 1 }, { "b", "abc" }
            });
            Assert.NotSame(bean.Export(), bean.Export());
        }

        [Fact]
        public void Import() {
            var bean = new Bean();
            bean["a"] = 1;
            bean["b"] = 1;
            bean["c"] = 1;

            var data = new Dictionary<string, IConvertible> { { "b", 2 }, { "c", null } };
            bean.Import(data);

            Assert.Equal(1, bean["a"]);
            Assert.Equal(2, bean["b"]);
            Assert.Equal(null, bean["c"]);

            data["b"] = "changed";
            Assert.Equal(2, bean["b"]);
        }

        [Fact]
        public void GetDrityNames() {
            var bean = new Bean();
            Assert.Empty(bean.GetDirtyNames());

            bean["a"] = 1;
            AssertExtensions.Equivalent(new[] { "a" }, bean.GetDirtyNames());

            bean["a"] = null;
            Assert.Empty(bean.GetDirtyNames());

            bean["a"] = 1;
            bean.ForgetDirtyBackup();
            Assert.Empty(bean.GetDirtyNames());

            bean["a"] = null;
            AssertExtensions.Equivalent(new[] { "a" }, bean.GetDirtyNames());
        }
    }


}

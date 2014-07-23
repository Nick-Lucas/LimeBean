using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {
    
    [TestFixture]
    public class BeanTests {

        [Test]
        public void Indexer() {
            var bean = new Bean();

            Assert.IsNull(bean["x"]);

            bean["x"] = "123";
            Assert.AreEqual("123", bean["x"]);

            bean["x"] = null;
            Assert.IsNull(bean["x"]);
        }

        [Test]
        public void TypedAccessors() {
            var bean = new Bean();

            Assert.AreEqual(0, bean.Get<int>("x"));
            Assert.IsNull(bean.GetNullable<int>("x"));

            bean.Put("x", 0);
            Assert.AreEqual(0, bean.Get<int>("x"));
            Assert.AreEqual(0, bean.GetNullable<int>("x"));

            bean.Put<int>("x", null);
            Assert.AreEqual(0, bean.Get<int>("x"));
            Assert.IsNull(bean.GetNullable<int>("x"));

            bean.Put("x", new Nullable<int>(1));
            Assert.AreEqual(1, bean.Get<int>("x"));
        }

        [Test, SetCulture("ru")]
        public void TypedAccessors_Conversion() {
            var bean = new Bean();

            bean.Put("x", "3.14");
            Assert.AreEqual(3.14, bean.GetNullable<double>("x"));
            Assert.AreEqual(3.14M, bean.Get<decimal>("x"));

            bean.Put("x", "abc");
            Assert.AreEqual(0, bean.Get<int>("x"));
            Assert.IsNull(bean.GetNullable<int>("x"));
        }

        [Test]
        public void TypedAccessors_Enums() {
            var bean = new Bean();

            bean.Put("x", TypeCode.String);
            Assert.AreEqual(TypeCode.String, bean.Get<TypeCode>("x"));

            bean.Put("x", "OBJECT");
            Assert.AreEqual(TypeCode.Object, bean.Get<TypeCode>("x"));

            bean.Put("x", (ulong)TypeCode.DBNull);
            Assert.AreEqual(TypeCode.DBNull, bean.Get<TypeCode>("x"));

            bean.Put("x", "?");
            Assert.AreEqual(default(TypeCode), bean.Get<TypeCode>("x"));
        }

        [Test]
        public void TypedAccessors_Dates() {
            var bean = new Bean();

            bean.Put("x", new DateTime(2011, 11, 11));
            Assert.AreEqual(2011, bean.Get<DateTime>("x").Year);

            bean.Put("x", "2012-12-12");
            Assert.AreEqual(2012, bean.Get<DateTime>("x").Year);

            bean.Put("x", "?");
            Assert.AreEqual(new DateTime(), bean.Get<DateTime>("x"));
        }

        [Test]
        public void Kind() { 
            var bean = new Bean("kind1");
            Assert.AreEqual("kind1", bean.GetKind());
        }

        [Test]
        public void ToStringMethod() {
            Assert.AreEqual("LimeBean.Bean", new Bean().ToString());

            var bean = new Bean("product");
            Assert.AreEqual("product", bean.ToString());
            bean.ID = 123;
            Assert.AreEqual("product #123", bean.ToString());            
        }

        [Test]
        public void Export() {
            var bean = new Bean();
            bean.ID = 123;
            bean.Put("a", 1).Put("b", "abc");
            CollectionAssert.AreEquivalent(bean.Export(), new Dictionary<string, IConvertible> { 
                { Bean.ID_PROP_NAME, 123L }, { "a", 1 }, { "b", "abc" }
            });
            Assert.AreNotSame(bean.Export(), bean.Export());
        }

        [Test]
        public void Import() {
            var bean = new Bean();
            bean["a"] = 1;
            bean["b"] = 1;
            bean["c"] = 1;

            var data = new Dictionary<string, IConvertible> { { "b", 2 }, { "c", null } };
            bean.Import(data);

            Assert.AreEqual(1, bean["a"]);
            Assert.AreEqual(2, bean["b"]);
            Assert.AreEqual(null, bean["c"]);

            data["b"] = "changed";
            Assert.AreEqual(2, bean["b"]);
        }

    }


}

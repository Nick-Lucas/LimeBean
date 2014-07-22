using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {

    [TestFixture]
    public class CacheTests {

        [Test]
        public void MapFunctionality() {
            var cache = new Cache<string, int>();
            Assert.IsFalse(cache.Contains("a"));
            Assert.Throws<KeyNotFoundException>(delegate() {
                cache.Get("a");
            });

            cache.Put("a", 1);
            Assert.IsTrue(cache.Contains("a"));
            Assert.AreEqual(1, cache.Get("a"));
        }

        [Test]
        public void ContainsDoesNotChangeOrder() {
            var cache = new Cache<string, int>();
            cache.Put("a", 1);
            cache.Put("b", 2);

            cache.Contains("a");
            CollectionAssert.AreEqual(new[] { "b", "a" }, cache.EnumerateKeys());
        }

        [Test]
        public void GetChangesOrder() {
            var cache = new Cache<string, int>();
            cache.Put("a", 1);
            cache.Put("b", 2);

            cache.Get("a");
            CollectionAssert.AreEqual(new[] { "a", "b" }, cache.EnumerateKeys());
        }

        [Test]
        public void PutChangesOrder() {
            var cache = new Cache<string, int>();
            cache.Put("a", 1);
            cache.Put("b", 2);

            cache.Put("a", 10);
            CollectionAssert.AreEqual(new[] { "a", "b" }, cache.EnumerateKeys());
        }

        [Test]
        public void Trimming() {
            var cache = new Cache<string, int>();
            cache.Put("a", 1);
            cache.Put("b", 2);
            cache.Put("c", 3);            

            cache.Capacity = 2;
            cache.Put("a", 10);

            Assert.IsFalse(cache.Contains("b"));
            CollectionAssert.AreEqual(new[] { "a", "c" }, cache.EnumerateKeys());

            cache.Put("d", 20);
            CollectionAssert.AreEqual(new[] { "d", "a" }, cache.EnumerateKeys());
        }

        [Test]
        public void Clear() {
            var cache = new Cache<string, int>();
            cache.Put("a", 1);
            cache.Clear();
            Assert.IsFalse(cache.Contains("a"));
        }

    }

}

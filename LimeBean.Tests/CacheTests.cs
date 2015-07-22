using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace LimeBean.Tests {

    public class CacheTests {

        [Fact]
        public void MapFunctionality() {
            var cache = new Cache<string, int>();
            Assert.False(cache.Contains("a"));
            Assert.Throws<KeyNotFoundException>(delegate() {
                cache.Get("a");
            });

            cache.Put("a", 1);
            Assert.True(cache.Contains("a"));
            Assert.Equal(1, cache.Get("a"));
        }

        [Fact]
        public void ContainsDoesNotChangeOrder() {
            var cache = new Cache<string, int>();
            cache.Put("a", 1);
            cache.Put("b", 2);

            cache.Contains("a");
            Assert.Equal(new[] { "b", "a" }, cache.EnumerateKeys());
        }

        [Fact]
        public void GetChangesOrder() {
            var cache = new Cache<string, int>();
            cache.Put("a", 1);
            cache.Put("b", 2);

            cache.Get("a");
            Assert.Equal(new[] { "a", "b" }, cache.EnumerateKeys());
        }

        [Fact]
        public void PutChangesOrder() {
            var cache = new Cache<string, int>();
            cache.Put("a", 1);
            cache.Put("b", 2);

            cache.Put("a", 10);
            Assert.Equal(new[] { "a", "b" }, cache.EnumerateKeys());
        }

        [Fact]
        public void Trimming() {
            var cache = new Cache<string, int>();
            cache.Put("a", 1);
            cache.Put("b", 2);
            cache.Put("c", 3);            

            cache.Capacity = 2;
            cache.Put("a", 10);

            Assert.False(cache.Contains("b"));
            Assert.Equal(new[] { "a", "c" }, cache.EnumerateKeys());

            cache.Put("d", 20);
            Assert.Equal(new[] { "d", "a" }, cache.EnumerateKeys());
        }

        [Fact]
        public void Clear() {
            var cache = new Cache<string, int>();
            cache.Put("a", 1);
            cache.Clear();
            Assert.False(cache.Contains("a"));
        }

    }

}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Xunit;

namespace LimeBean.Tests {

    public class DbCommandDescriptorTests {

        [Fact]
        public void Equality_SqlOnly() {
            var cmd1 = new DbCommandDescriptor("a");
            var cmd2 = new DbCommandDescriptor("a");

            Assert.Equal(cmd1.GetHashCode(), cmd2.GetHashCode());
            Assert.Equal(cmd1, cmd2);

            Assert.True(cmd1.Equals((object)cmd2));
        }

        [Fact]
        public void Inequality_SqlOnly() {
            var cmd1 = new DbCommandDescriptor("a");
            var cmd2 = new DbCommandDescriptor("b");

            Assert.NotEqual(cmd1, cmd2);
            Assert.False(cmd1.Equals((object)cmd2));            
        }

        [Fact]
        public void Equality_Params_Null() {
            var cmd1 = new DbCommandDescriptor("a", null);
            var cmd2 = new DbCommandDescriptor("a", null);

            Assert.Equal(cmd1.GetHashCode(), cmd2.GetHashCode());
        }

        [Fact]
        public void Equality_Params() {
            var cmd1 = new DbCommandDescriptor("a", 1, 2, 3);
            var cmd2 = new DbCommandDescriptor("a", 1, 2, 3);

            Assert.Equal(cmd1.GetHashCode(), cmd2.GetHashCode());
            Assert.Equal(cmd1, cmd2);
        }

        [Fact]
        public void Inequality_Params() {
            var cmd1 = new DbCommandDescriptor("a", 1, 2);
            var cmd2 = new DbCommandDescriptor("a", 1, "2");
            var cmd3 = new DbCommandDescriptor("a", 1, null);
            var cmd4 = new DbCommandDescriptor("a", null, 2);
            var cmd5 = new DbCommandDescriptor("a", 1);
            var cmd6 = new DbCommandDescriptor("a", 1, 2, 3);

            Assert.NotEqual(cmd1, cmd2);
            Assert.NotEqual(cmd1, cmd3);
            Assert.NotEqual(cmd1, cmd4);
            Assert.NotEqual(cmd1, cmd5);
            Assert.NotEqual(cmd1, cmd6);
        }

        [Fact]
        public void Inequality_Tag() {
            var cmd1 = new DbCommandDescriptor(1, "a");
            var cmd2 = new DbCommandDescriptor(2, "a");

            Assert.NotEqual(cmd1, cmd2);
        }

        [Fact]
        public void DictionaryKey() {
            var cmd1 = new DbCommandDescriptor("abc", new { a = 123 });
            var cmd2 = new DbCommandDescriptor("abc", new { a = 123 });

            var dict = new Dictionary<object, object>();
            dict[cmd1] = "ok";

            Assert.Equal("ok", dict[cmd2]);        
        }

    }

}

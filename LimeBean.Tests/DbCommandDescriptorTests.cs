using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {

    [TestFixture]
    public class DbCommandDescriptorTests {

        [Test]
        public void Equality_SqlOnly() {
            var cmd1 = new DbCommandDescriptor("a");
            var cmd2 = new DbCommandDescriptor("a");

            Assert.AreEqual(cmd1.GetHashCode(), cmd2.GetHashCode());
            Assert.AreEqual(cmd1, cmd2);

            Assert.IsTrue(cmd1.Equals((object)cmd2));
        }

        [Test]
        public void Inequality_SqlOnly() {
            var cmd1 = new DbCommandDescriptor("a");
            var cmd2 = new DbCommandDescriptor("b");

            Assert.AreNotEqual(cmd1, cmd2);
            Assert.IsFalse(cmd1.Equals((object)cmd2));            
        }

        [Test]
        public void Equality_Params_Null() {
            var cmd1 = new DbCommandDescriptor("a", null);
            var cmd2 = new DbCommandDescriptor("a", null);

            Assert.AreEqual(cmd1.GetHashCode(), cmd2.GetHashCode());
        }

        [Test]
        public void Equality_Params() {
            var cmd1 = new DbCommandDescriptor("a", 1, 2, 3);
            var cmd2 = new DbCommandDescriptor("a", 1, 2, 3);

            Assert.AreEqual(cmd1.GetHashCode(), cmd2.GetHashCode());
            Assert.AreEqual(cmd1, cmd2);
        }

        [Test]
        public void Inequality_Params() {
            var cmd1 = new DbCommandDescriptor("a", 1, 2);
            var cmd2 = new DbCommandDescriptor("a", 1, "2");
            var cmd3 = new DbCommandDescriptor("a", 1, null);
            var cmd4 = new DbCommandDescriptor("a", null, 2);
            var cmd5 = new DbCommandDescriptor("a", 1);
            var cmd6 = new DbCommandDescriptor("a", 1, 2, 3);

            Assert.AreNotEqual(cmd1, cmd2);
            Assert.AreNotEqual(cmd1, cmd3);
            Assert.AreNotEqual(cmd1, cmd4);
            Assert.AreNotEqual(cmd1, cmd5);
            Assert.AreNotEqual(cmd1, cmd6);
        }

        [Test]
        public void Inequality_Tag() {
            var cmd1 = new DbCommandDescriptor(1, "a");
            var cmd2 = new DbCommandDescriptor(2, "a");

            Assert.AreNotEqual(cmd1, cmd2);
        }

        [Test]
        public void DictionaryKey() {
            var cmd1 = new DbCommandDescriptor("abc", new { a = 123 });
            var cmd2 = new DbCommandDescriptor("abc", new { a = 123 });

            var dict = new Hashtable();
            dict[cmd1] = "ok";

            Assert.AreEqual("ok", dict[cmd2]);        
        }

    }

}

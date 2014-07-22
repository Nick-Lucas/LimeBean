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
        public void Equality_PositionalParams_Null() {
            var cmd1 = new DbCommandDescriptor("a", null);
            var cmd2 = new DbCommandDescriptor("a", null);

            Assert.AreEqual(cmd1.GetHashCode(), cmd2.GetHashCode());
        }

        [Test]
        public void Equality_PositionalParams() {
            var cmd1 = new DbCommandDescriptor("a", 1, 2, 3);
            var cmd2 = new DbCommandDescriptor("a", 1, 2, 3);

            Assert.AreEqual(cmd1.GetHashCode(), cmd2.GetHashCode());
            Assert.AreEqual(cmd1, cmd2);
        }

        [Test]
        public void Inequality_PositionalParams() {
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
        public void Equality_NamedParams() {
            var cmd1 = new DbCommandDescriptor("a", new { a = 1, b = 2, c = 3 });
            var cmd2 = new DbCommandDescriptor("a", new OrderedDictionary { { "a", 1 }, { "b", 2 }, { "c", 3 } });
            var cmd3 = new DbCommandDescriptor("a", new OrderedDictionary { { "c", 3 }, { "b", 2 }, { "a", 1 } });

            Assert.AreEqual(cmd1.GetHashCode(), cmd2.GetHashCode());
            Assert.AreEqual(cmd1, cmd2);

            Assert.AreEqual(cmd1.GetHashCode(), cmd3.GetHashCode());
            Assert.AreEqual(cmd1, cmd3);
        }

        [Test]
        public void Inequality_NamedParamsVsPositionalParams() {
            var cmd1 = new DbCommandDescriptor("a", new { a = 123 });
            var cmd2 = new DbCommandDescriptor("a", 123);

            Assert.AreNotEqual(cmd1, cmd2);
        }

        [Test]
        public void Inequality_Tag() {
            var cmd1 = new DbCommandDescriptor(1, "a");
            var cmd2 = new DbCommandDescriptor(2, "a");

            Assert.AreNotEqual(cmd1, cmd2);
        }

        [Test]
        public void ToCommand() {
            using(var conn = new SQLiteConnection("data source=:memory:")) {
                conn.Open();

                // SQL only
                using(var cmd = new DbCommandDescriptor("abc").ToCommand(conn)) {
                    Assert.AreEqual("abc", cmd.CommandText);
                    Assert.IsEmpty(cmd.Parameters);
                }

                // single null
                using(var cmd = new DbCommandDescriptor("abc", null).ToCommand(conn)) {
                    Assert.AreEqual("abc", cmd.CommandText);
                    Assert.AreEqual(1, cmd.Parameters.Count);

                    var p = cmd.Parameters[0] as IDataParameter;
                    Assert.IsNull(p.Value);
                }

                // positional
                using(var cmd = new DbCommandDescriptor("abc", 1, "a", null).ToCommand(conn)) {
                    Assert.AreEqual("abc", cmd.CommandText);
                    Assert.AreEqual(3, cmd.Parameters.Count);
                    
                    var p1 = cmd.Parameters[0] as IDataParameter;
                    var p2 = cmd.Parameters[1] as IDataParameter;
                    var p3 = cmd.Parameters[2] as IDataParameter;

                    Assert.IsNull(p1.ParameterName);
                    Assert.IsNull(p2.ParameterName);
                    Assert.IsNull(p3.ParameterName);

                    Assert.AreEqual(1, p1.Value);
                    Assert.AreEqual("a", p2.Value);
                    Assert.AreEqual(null, p3.Value);
                }

                // named as object
                using(var cmd = new DbCommandDescriptor("abc", new { a = 1 }).ToCommand(conn)) {
                    Assert.AreEqual("abc", cmd.CommandText);
                    Assert.AreEqual(1, cmd.Parameters.Count);

                    var p = cmd.Parameters[0] as IDataParameter;
                    Assert.AreEqual("a", p.ParameterName);
                    Assert.AreEqual(1, p.Value);
                }

                // named as dict
                using(var cmd = new DbCommandDescriptor("abc", new OrderedDictionary { { "z", 123 }, { "a", null } }).ToCommand(conn)) {
                    Assert.AreEqual("abc", cmd.CommandText);
                    Assert.AreEqual(2, cmd.Parameters.Count);

                    var p1 = cmd.Parameters[0] as IDataParameter;
                    var p2 = cmd.Parameters[1] as IDataParameter;

                    Assert.AreEqual("a", p1.ParameterName);
                    Assert.AreEqual(null, p1.Value);

                    Assert.AreEqual("z", p2.ParameterName);
                    Assert.AreEqual(123, p2.Value);
                }
            }        
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

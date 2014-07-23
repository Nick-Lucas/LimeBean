using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace LimeBean.Tests.Examples {

    [TestFixture, Explicit]
    public class Stress {
        const string DB_PATH = "c:/temp/lime-stress.db";
        BeanApi R;

        [SetUp]
        public void SetUp() {
            R = new BeanApi("data source=" + DB_PATH, SQLiteFactory.Instance);            
        }

        [TearDown]
        public void TearDown() {
            R.Dispose();
        }


        [Test]
        public void Fill() {
            R.Exec("drop table if exists foo");
            R.EnterFluidMode();

            R.Transaction(delegate() {
                for(var i = 0; i < 10 * 1000; i++) {
                    var bean = R.Dispense("foo");
                    for(var c = 0; c < 12; c++)
                        bean["c" + c] = GenerateLongString();

                    R.Store(bean);
                }
            });
        }

        [Test]
        public void IteratorRead() {
            foreach(var bean in R.FindIterator("foo")) {
                var id = bean.ID;
                // Console.WriteLine(id);
            }        
        }


        string GenerateLongString() {
            var builder = new StringBuilder();
            for(var i = 0; i < 128; i++)
                builder.Append(Guid.NewGuid().ToString("N"));
            return builder.ToString();
        }

    }

}

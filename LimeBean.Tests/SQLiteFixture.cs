using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {
    
    public abstract class SQLiteFixture {
        IDbConnection _conn;
        internal IDatabaseDetails _details;
        internal IDatabaseAccess _db;

        [SetUp]
        public virtual void SetUp() {
            _conn = new SQLiteConnection("data source=:memory:");
            _conn.Open();
            _details = new SQLiteDetails();     
            _db = new DatabaseAccess(_conn, _details);
        }

        [TearDown]
        public virtual void TestFixtureTearDown() {
            _conn.Dispose();
        }
    }

}

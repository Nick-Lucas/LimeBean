using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {

    class RoundtripChecker {
        IDatabaseAccess _db;
        DatabaseStorage _storage;

        public RoundtripChecker(IDatabaseAccess db, DatabaseStorage storage) {
            _db = db;
            _storage = storage;
        }

        public void Check(IConvertible before, IConvertible after) {            
            _db.Exec("drop table if exists foo");
            _storage.InvalidateSchema();

            var id = _storage.Store("foo", new Dictionary<string, IConvertible> { 
                    { "p", before }
                });

            var loaded = _storage.Load("foo", id);
            Assert.AreEqual(after, loaded["p"]);

            if(after != null)
                Assert.AreEqual(after.GetType(), loaded["p"].GetType());        
        }

    }
}

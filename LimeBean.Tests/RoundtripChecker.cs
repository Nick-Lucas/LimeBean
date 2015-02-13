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
            var id = _storage.Store("foo", new Dictionary<string, IConvertible> { 
                    { "p", before }
                });

            try {
                var loaded = _storage.Load("foo", id);
                Assert.AreEqual(after, loaded.ContainsKey("p") ? loaded["p"] : null);

                if(after != null)
                    Assert.AreEqual(after.GetType(), loaded["p"].GetType());
            } finally {
                _db.Exec("drop table foo");
                _storage.InvalidateSchema();            
            }
        }

    }
}

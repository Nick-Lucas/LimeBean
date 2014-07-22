using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {

    [TestFixture]
    public class IntegrationTests {

        [Test]
        public void ImplicitTransactionsOnStoreAndTrash() {
            using(var conn = new SQLiteConnection("data source=:memory:")) {
                conn.Open();

                IDatabaseAccess db = new DatabaseAccess(conn);
                IStorage storage = new SQLiteStorage(db);
                IBeanCrud crud = new BeanCrud(storage, db);               

                (storage as DatabaseStorage).EnterFluidMode();

                var bean = crud.Dispense<ThrowingBean>();
                bean["foo"] = "ok";
                var id = crud.Store(bean);

                bean.Throw = true;
                bean["foo"] = "fail";

                try { crud.Store(bean); } catch { }
                Assert.AreEqual("ok", db.Cell<string>("select foo from test where " + Bean.ID_PROP_NAME + " = ?", id));

                try { crud.Trash(bean); } catch { }
                Assert.IsTrue(db.Cell<int>("select count(*) from test") > 0);
            }
        }

        class ThrowingBean : Bean {
            public bool Throw;

            public ThrowingBean()
                : base("test") {
            }

            protected internal override void AfterStore() {
                if(Throw)
                    throw new Exception();
            }

            protected internal override void AfterTrash() {
                if(Throw)
                    throw new Exception();
            }
        }

        [Test, Explicit]
        public void Example_CRUD() {
            // Based on https://code.google.com/p/orange-bean/#CRUD

            // Tell about your database
            var R = new BeanApi("data source=:memory:", new SQLiteFactory());

            // Enable automatic schema update
            R.EnterFluidMode();
            
            // create a bean 
            var bean = R.Dispense("person");

            // it's of kind "person"
            Console.WriteLine(bean.GetKind());

            // fill it
            bean["name"] = "Alex";
            bean["year"] = 1984;
            bean["smart"] = true;

            // store it
            var id = R.Store(bean);

            // Database schema will be updated automatically for you

            // Now the bean has an id
            Console.WriteLine(bean.ID);
            
            // load a bean
            bean = R.Load("person", id);

            // change it
            bean["name"] = "Lexa";
            bean["new_prop"] = 123;

            // commit changes
            R.Store(bean);

            // or delete it
            R.Trash(bean);

            // close the connection
            R.Dispose();
        }
    }

}

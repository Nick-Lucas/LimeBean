using MySql.Data.MySqlClient;
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

                IDatabaseDetails details = new SQLiteDetails();
                IDatabaseAccess db = new DatabaseAccess(conn, details);
                DatabaseStorage storage = new DatabaseStorage(details, db);
                IBeanCrud crud = new BeanCrud(storage, db);               

                storage.EnterFluidMode();

                var bean = crud.Dispense<ThrowingBean>();
                bean["foo"] = "ok";
                var id = crud.Store(bean);

                bean.Throw = true;
                bean["foo"] = "fail";

                try { crud.Store(bean); } catch { }
                Assert.AreEqual("ok", db.Cell<string>(true, "select foo from test where " + Bean.ID_PROP_NAME + " = ?", id));

                try { crud.Trash(bean); } catch { }
                Assert.IsTrue(db.Cell<int>(true, "select count(*) from test") > 0);
            }
        }

        [Test]
        public void Api_DetailsSelection() {
            Assert.AreEqual("SQLite", new BeanApi(new SQLiteConnection()).CreateDetails().DbName);
            Assert.AreEqual("MariaDB", new BeanApi(new MySqlConnection()).CreateDetails().DbName);            
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

    }

}

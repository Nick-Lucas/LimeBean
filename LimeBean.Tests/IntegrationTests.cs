using MySql.Data.MySqlClient;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
                IKeyAccess keys = new KeyUtil();
                DatabaseStorage storage = new DatabaseStorage(details, db, keys);
                IBeanCrud crud = new BeanCrud(storage, db, keys);

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
            Assert.AreEqual("MsSql", new BeanApi(new SqlConnection()).CreateDetails().DbName);            
        }

        [Test]
        public void Regression_NullingExistingProp() {
            using(var api = new BeanApi("data source=:memory:", SQLiteFactory.Instance)) {
                api.EnterFluidMode();

                var bean = api.Dispense("kind1");
                bean["p"] = 123;

                var id = api.Store(bean);

                bean["p"] = null;
                api.Store(bean);

                bean = api.Load("kind1", id);
                Assert.IsNull(bean["p"]);
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

    }

}

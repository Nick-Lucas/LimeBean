using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Xunit;

namespace LimeBean.Tests {

    public class IntegrationTests {

        [Fact]
        public void ImplicitTransactionsOnStoreAndTrash() {
            using(var conn = SQLitePortability.CreateConnection()) {
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
                Assert.Equal("ok", db.Cell<string>(true, "select foo from test where id = {0}", id));

                try { crud.Trash(bean); } catch { }
                Assert.True(db.Cell<int>(true, "select count(*) from test") > 0);
            }
        }

        [Fact]
        public void DisableImplicitTransactions() {
            using(var api = SQLitePortability.CreateApi()) {
                api.EnterFluidMode();
                api.ImplicitTransactions = false;

                var bean = api.Dispense<ThrowingBean>();
                bean.Throw = true;
                try { api.Store(bean); } catch { }

                Assert.Equal(1, api.Count<ThrowingBean>());
            }           
        }

        [Fact]
        public void Api_DetailsSelection() {
            Assert.Equal("SQLite", new BeanApi(SQLitePortability.CreateConnection()).CreateDetails().DbName);

#if !NO_MSSQL
            Assert.Equal("MsSql", new BeanApi(new SqlConnection()).CreateDetails().DbName);
#endif

#if !NO_MARIADB
            Assert.Equal("MariaDB", new BeanApi(new MySql.Data.MySqlClient.MySqlConnection()).CreateDetails().DbName);
#endif
        }

        [Fact]
        public void Regression_NullingExistingProp() {
            using(var api = SQLitePortability.CreateApi()) {
                api.EnterFluidMode();

                var bean = api.Dispense("kind1");
                bean["p"] = 123;

                var id = api.Store(bean);

                bean["p"] = null;
                api.Store(bean);

                bean = api.Load("kind1", id);
                Assert.Null(bean["p"]);
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

using System;
using System.Linq;
using System.Data.Common;
using System.Data.SQLite;
using System.Web;
using MySql.Data.MySqlClient;
using System.Data.SqlClient;
using System.Data;
using MySql.Data.Types;

namespace LimeBean.Website {

    class Body {
        /// ## About LimeBean
        /// LimeBean provides a simple and concise API for accessing **ADO.NET** data sources.
        /// It's **Hybrid-ORM**... halfway between a micro-ORM and plain old SQL.
        ///   
        /// Compatible with:
        /// 
        /// * **.NET Framework 4.x**
        /// * **.NET Core** (ASP.NET Core, UWP)
        /// * **Mono**
        /// 
        /// Supported databases include:
        /// 
        /// * **SQLite** 
        /// * **MySQL/MariaDB** 
        /// * **PostgreSQL**
        /// * **SQL Server**
        /// 
        /// Limebean uses synonyms for many things, key examples are:
        /// 
        /// * **Bean** - A single row of data
        /// * **Kind** - The name of a table which a Bean represents
        /// 
        /// ## Installation
        /// LimeBean is available on [NuGet Gallery](https://www.nuget.org/packages/LimeBean):
        /// 
        ///     PM> Install-Package LimeBean
        ///     
        ///     

        void ConnectToDatabase(DbConnection connection) {
            /// ## Getting started: Connecting
            /// LimeBean needs an ADO.NET driver to work with. You can use one of the following:
            /// 
            /// * [System.Data.SQLite.Core](https://www.nuget.org/packages/System.Data.SQLite.Core) for SQLite in .NET
            /// * [Mono.Data.Sqlite](http://www.mono-project.com/docs/database-access/providers/sqlite/) for SQLite in Mono
            /// * [Microsoft.Data.SQLite](https://www.nuget.org/packages/Microsoft.Data.SQLite) for SQLite in .NET Core
            /// * [MySql.Data](https://www.nuget.org/packages/MySql.Data/) for MySQL or MariaDB
            /// * [Npgsql](https://www.nuget.org/packages/Npgsql/) for PostgreSQL
            /// * [System.Data.SqlClient](https://msdn.microsoft.com/en-us/library/System.Data.SqlClient.aspx) for SQL Server
            /// 
            /// To start using LimeBean, create an instance of the `BeanApi` class:
            {
#if CODE     
                // Using a connection string and an ADO.NET provider factory                
                var api = new BeanApi("server=localhost; database=db1; ...", MySqlClientFactory.Instance);
#endif
            }
            {
#if CODE
                // Using a connection string and a connection type
                var api = new BeanApi("data source=/path/to/db", typeof(SQLiteConnection));
#endif
            }
            {
#if CODE
                // Using a shared pre-opened connection
                var api = new BeanApi(connection);
#endif
            }
            /// **NOTE:** `BeanApi` implements `IDisposable`. When created from a connection string (two first cases above), 
            /// the underlying connection is initiated on the first usage and closed on dispose.
            /// Shared connections are used as-is, their state is not changed.
            /// 
            /// See also: [BeanApi Object Lifetime](#beanapi-object-lifetime)
        }

        void CRUD(BeanApi api) {
            /// ## Getting Started: Basic CRUD (Create/Read/Update/Delete)
            /// 
            /// For basic usage, LimeBean requires no configuration or table classes! 
            /// 
            /// Take a look at some basic CRUD scenarios:
            /// 
            /// **Create**
#if CODE

            // Create a Bean. 
            // "Bean" means row, and "Dispense" makes an empty Bean for a table.
            var bean = api.Dispense("book");

            // Each bean has a "Kind". Kind is a synonym for "table name"
            // You give a Bean its Kind when you Dispense it, or query the database
            var kind = bean.GetKind();
            Console.WriteLine(kind);

            // Fill the new Bean with some data
            bean["title"] = "Three Comrades";
            bean["rating"] = 10;

            // You can also chain .Put() to do this
            bean.Put("title", "Three Comrades")
                .Put("rating", 10);

            // Store it
            // Store() will Create or Update a record intelligently
            var id = api.Store(bean);

            // Store also returns the Primary Key for the saved Bean, even for [multi-column/compound keys](#primary-keys)
            Console.WriteLine(id);
#endif
            /// **Read** and **Update**
#if CODE
            // Load a Bean with a known ID
            bean = api.Load("book", id);

            // Make some edits
            bean["release_date"] = new DateTime(2015, 7, 30);
            bean["rating"] = 5;

            // Update database
            api.Store(bean);
#endif
            /// **Delete**
#if CODE
            api.Trash(bean);
#endif
        }

        void TypedAccessors(Bean bean) {
            /// ## Typed Accessors
            /// To access bean properties in a strongly-typed fashion, use the `Get<T>` method:
#if CODE
            bean.Get<string>("title");
            bean.Get<decimal>("price");
            bean.Get<bool?>("someFlag");
#endif
            /// And there is a companion `Put` method which is chainable:
#if CODE
            bean
                .Put("name", "Jane Doe")
                .Put("comment", null);
#endif
            /// See also: [Custom Bean Classes](#custom-bean-classes)
        }

        void FluidMode(BeanApi api) { 
            /// ## Fluid Mode
            /// LimeBean mitigates the common inconvenience associated with relational databases,
            /// namely necessity to manually create tables, columns and adjust their data types. 
            /// In this sense, LimeBean takes SQL databases a little closer to NoSQL ones like MongoDB.
            /// 
            /// **Fluid Mode** is optional, turned off by default, and is recommended for use only during early development stages
            /// (particularly for prototyping and scaffolding).
            /// To enable it, invoke the `EnterFluidMode` method on the `BeanApi` object:
#if CODE
            api.EnterFluidMode();

            // Make a Bean for a table which doesn't yet exist
            var bean = api.Dispense("book_types");

            // Fill it with some data
            // Limebean will automatically detect Types and create columns with the correct Type
            bean.Put("name", "War")
                .Put("fiction", true);

            // Store will automatically create any missing tables (with an auto-incrementing 'id' column) and columns, 
            // then add the Bean as a new row
            var id = api.Store(bean);

            // The bean is now available in the database
            var savedBean = api.Load("book_types", id);

#endif
            /// How does this work? When you save a Bean while in Fluid Mode, LimeBean analyzes its fields and compares 
            /// their names and types to the database schema. 
            /// If new data cannot be stored to an existing table, schema alteration occurs.
            /// LimeBean can create new tables, add missing columns, and widen data types.
            /// It will never truncate data or delete unused columns.
            /// 
            /// **NOTE:** LimeBean will not detect renamings.
            /// 
            /// **CAUTION:** Automatically generated schema is usually sub-optimal and lacks indexes which are essential
            /// for performance. When most of planned tables are already in place, 
            /// it is recommended to turn the Fluid Mode off, audit the database structure, add indexes, and make further schema
            /// changes with a dedicated database management tool (like HeidiSQL, SSMS, pgAdmin, etc).
            /// 
        }

        void FindingBeansWithSql(BeanApi api) {
            /// ## Finding Beans with SQL
            /// LimeBean doesn't introduce any custom query language, nor does it implement a LINQ provider. 
            /// To find beans matching a criteria, use fragments of plain SQL:
            /// 
            {
#if CODE
                var list = api.Find("book", "WHERE rating > 7");
#endif
            }
            /// Instead of embedding values into SQL code, it is recommended to use **parameters**:
            {
#if CODE
                var list = api.Find("book", "WHERE rating > {0}", 7);
#endif
            }
            /// Usage of parameters looks similar to `String.Format`, but instead of direct interpolation,
            /// they are transformed into fair ADO.NET command parameters to protect your queries from SQL-injection attacks.
            /// 
            {
#if CODE
                var list = api.Find(
                    "book", 
                    "WHERE release_date BETWEEN {0} and {1} AND author LIKE {2}",
                    new DateTime(1930, 1, 1), new DateTime(1950, 1, 1), "%remarque%"
                );
#endif
            }
            /// 
            /// You can use any SQL as long as the result maps to a set of beans. 
            /// For other cases, see [Generic Queries](#generic-sql-queries).
            /// 
            /// To find a single bean:
#if CODE
            var best = api.FindOne("book", "ORDER BY rating DESC LIMIT 1");
#endif
            /// To find out the number of beans without loading them:
#if CODE
            var count = api.Count("book", "WHERE rating > {0}", 7);
#endif
            /// It is also possible to perform unbuffered (memory-optimized) load for processing in a `foreach` loop.
            /// 
            /// Data is 'Lazy Loaded' on each iteration using [C-sharp's IEnumerable Yield](http://programmers.stackexchange.com/a/97350)
#if CODE
            foreach (var bean in api.FindIterator("book", "ORDER BY rating")) {
                // do something with bean
            }
#endif
        }

        class CustomBeanClasses {
            /// ## Custom Bean Classes
            /// You can create Table classes like in a full ORM: It's convenient to inherit from the base `Bean` class:
            /// 
#if CODE
            public class Book : Bean {
                public Book()
                    : base("book") {
                }

                public string Title {
                    get { return Get<string>("title"); }
                    set { Put("title", value); }
                }

                // ...
            }
#endif
            /// Doing so has several advantages:
            /// 
            /// - All strings prone to typos (bean kind and field names) are encapsulated inside.
            /// - You get compile-time checks, IDE assistance and [typed properties](#typed-accessors).
            /// - With [Lifecycle Hooks](#lifecycle-hooks), it is easy to implement [data validation](#data-validation) and [relations](#relations).
            /// 
            /// For [Custom Beans Classes](#custom-bean-classes), use method overloads with a generic parameter:
            /// 
            void Overloads(BeanApi api) {
#if CODE
                api.Dispense<Book>();
                api.Load<Book>(1);
                api.Find<Book>("WHERE rating > {0}", 7);
                // and so on
#endif
            }
        }

        class CustomBeanClasses_nameof {
            /// ### Using `nameof()`
            /// With the help of the [nameof](https://msdn.microsoft.com/en-us/library/dn986596.aspx) operator
            /// (introduced in C# 6 / Visual Studio 2015),
            /// it's possible to define properties without using strings at all:
            
            public class Book : Bean {
                public Book()
                    : base("") {
                }
#if CODE
                public string Title {
                    get { return Get<string>(nameof(Title)); }
                    set { Put(nameof(Title), value); }
                }
#endif

                // ...
            }        
        }

        class LifecycleHooks { 
            /// ## Lifecycle Hooks
            /// [Custom Bean Classes](#custom-bean-classes) provide lifecycle hook methods which you can override to receive 
            /// notifications about [CRUD operations]getting-started-basic-crud-create-read-update-delete occurring to this bean:
            /// 
#if CODE
            public class Product : Bean {
                public Product()
                    : base("product") {
                }

                protected override void AfterDispense() {
                }

                protected override void BeforeLoad() {
                }

                protected override void AfterLoad() {
                }

                protected override void BeforeStore() {
                }

                protected override void AfterStore() {
                }

                protected override void BeforeTrash() {
                }

                protected override void AfterTrash() {
                }
            }
#endif
            /// Particularly useful are `BeforeStore` and `BeforeTrash` methods. 
            /// They can be used for [validation](#data-validation), implementing [relations](#relations),
            /// assigning default values, etc.
            /// 
            /// See also: [Bean Observers](#bean-observers)
        }

        class PrimaryKeys {
            /// ## Primary Keys
            /// By default, all beans have auto-incrementing integer key named `"id"`. 
            /// Keys are customizable in all aspects:

            class Book : Bean {
                public Book()
                    : base("") {
                }
            }

            void Samples(BeanApi api) {
#if CODE
                // Custom key name for beans of kind "book"
                api.Key("book", "book_id");

                // Custom key name for custom bean class Book (see Custom Bean Classes)
                api.Key<Book>("book_id");

                // Custom non-autoincrement key
                api.Key("book", "book_id", false);

                // Compound key (order_id, product_id) for beans of kind "order_item"
                api.Key("order_item", "order_id", "product_id");

                // Change defaults for all beans
                api.DefaultKey("Oid", false);
#endif
            }
            /// **NOTE:** non auto-increment keys must be assigned manually prior to saving.
            /// 
            /// The [Bean Observers](#bean-observers) section contains an example of using GUID keys for all beans.
        }

        void GenericSqlQueries(BeanApi api) {
            /// ## Generic SQL Queries
            /// Often it's needed to execute queries which don't map to beans: 
            /// aggregates, grouping, joins, selecting single column, etc.
            /// 
            /// `BeanApi` provides methods for such tasks:
            /// 
            {
#if CODE
                // Load multiple rows
                var rows = api.Rows(@"SELECT author, COUNT(*) 
                                      FROM book 
                                      WHERE rating > {0} 
                                      GROUP BY author", 7);
      
                // Load a single row
                var row = api.Row(@"SELECT author, COUNT(*) 
                                    FROM book 
                                    WHERE rating > {0}
                                    GROUP BY author 
                                    ORDER BY COUNT(*) DESC 
                                    LIMIT 1", 7);
      
                // Load a column
                var col = api.Col<string>("SELECT DISTINCT author FROM book ORDER BY author");
      
                // Load a single value
                var count = api.Cell<int>("SELECT COUNT(*) FROM book");
#endif
            }
            /// For `Rows` and `Col`, there are unbuffered (memory-optimized) counterparts:
            {
#if CODE
                foreach(var row in api.RowsIterator("SELECT...")) {
                    // do something
                }

                foreach(var item in api.ColIterator("SELECT...")) {
                    // do something
                }
#endif
            }
            /// To execute a non-query SQL command, use the `Exec` method:
#if CODE
            api.Exec("SET autocommit = 0");
#endif
            /// **NOTE:** all described functions accept parameters in the same form as [finder methods](#finding-beans-with-sql) do.
        }

        void CustomizingSqlCommands(BeanApi api, Bean bean) {
            /// ## Customizing SQL Commands
            /// In some cases it is necessary to manually adjust parameters of a SQL command which is about to execute.
            /// This can be done in the `QueryExecuting` event handler. 
            /// 
            /// **Example 1.**  Force `datetime2` type for all dates (SQL Server):
#if CODE
            api.QueryExecuting += cmd => {
                foreach(SqlParameter p in cmd.Parameters)
                    if(p.Value is DateTime)
                        p.SqlDbType = SqlDbType.DateTime2;
            };
#endif
            /// **Example 2.** Work with `MySqlGeometry` objects (MySQL/MariaDB):
#if CODE
            api.QueryExecuting += cmd => {
                foreach(MySqlParameter p in cmd.Parameters)
                    if(p.Value is MySqlGeometry)
                        p.MySqlDbType = MySqlDbType.Geometry;
            };

            bean["point"] = new MySqlGeometry(34.962, 34.066);
            api.Store(bean);
#endif
        }

        class DataValidation {
            /// ## Data Validation
            /// The `BeforeStore` [hook](#lifecycle-hooks) can be used to prevent bean from storing under certain
            /// circumstances. For example, let's define a [custom bean](#custom-bean-classes) `Book` which cannot be stored 
            /// unless it has a non-empty title:
#if CODE
            public class Book : Bean {
                public Book()
                    : base("book") {
                }

                public string Title {
                    get { return Get<string>("title"); }
                    set { Put("title", value); }
                }

                protected override void BeforeStore() {
                    if(String.IsNullOrWhiteSpace(Title))
                        throw new Exception("Title must not be empty");
                }
            }
#endif
            /// See also: [Custom Bean Classes](#custom-bean-classes), [Lifecycle Hooks](#lifecycle-hooks)
        }

        class Relations {
            /// ## Relations
            /// Consider an example of two [custom beans](#custom-bean-classes): `Category` and `Product`:
#if CODE
            public partial class Category : Bean {
                public Category()
                    : base("category") {
                }

            }

            public partial class Product : Bean {
                public Product()
                    : base("product") {
                }
            }
#endif
            /// We are going to link them so that a product knows its category, and a category can list all its products.
            /// 
            /// In the `Product` class, let's declare a method `GetCategory()`:
#if CODE
            partial class Product {
                public Category GetCategory() {
                    return GetApi().Load<Category>(this["category_id"]);
                }
            }
#endif
            /// In the `Category` class, we'll add a method named `GetProducts()`:
#if CODE
            partial class Category {
                public Product[] GetProducts() {
                    return GetApi().Find<Product>("WHERE category_id = {0}", this["id"]);
                }
            }
#endif
            /// **NOTE:** LimeBean uses the [internal query cache](#internal-query-cache), therefore repeated `Load` and `Find` calls don't hit the database.
            /// 
            /// Now let's add some [validation logic](#data-validation) to prevent saving a product without a category and to prevent deletion of 
            /// a non-empty category:
#if CODE
            partial class Product {
                protected override void BeforeStore() {
                    if(GetCategory() == null)
                        throw new Exception("Product must belong to an existing category");
                }
            }

            partial class Category {
                protected override void BeforeTrash() {
                    if(GetProducts().Any())
                        throw new Exception("Category still contains products");
                }
            }
#endif
            /// Alternatively, we can implement cascading deletion:

            class DrasticCategory : Category {
#if CODE
                protected override void BeforeTrash() {
                    foreach(var p in GetProducts())
                        GetApi().Trash(p);
                }
#endif
            }

            /// **NOTE:** `Store` and `Trash` always run in a transaction (see [Implicit Transactions](#implicit-transactions)),
            /// therefore even if something goes wrong inside the cascading deletion loop, database will remain in a consistent state!
        }


        void Transactions(BeanApi api) {
            /// ## Transactions
            /// To execute a block of code in a transaction, wrap it in a delegate and pass to the `Transaction` method:
#if CODE
            api.Transaction(delegate() { 
                // do some work
            });
#endif
            /// Transaction is automatically rolled back if:
            /// 
            /// * An unhandled exception is thrown during the execution
            /// * The delegate returns `false`
            ///
            /// Otherwise it's committed.
            /// 
            /// Transactions can be nested (if the underlying ADO.NET provider allows this):
#if CODE
            api.Transaction(delegate() {
                // outer transaction

                api.Transaction(delegate() { 
                    // nested transaction
                });
            });
#endif
            /// ## Implicit Transactions
            /// When you invoke `Store` or `Trash` (see [CRUD]getting-started-basic-crud-create-read-update-delete) outside a transaction, then an implicit transaction
            /// is initiated behind the scenes. This is done to enforce database integrity in case of 
            /// additional modifications performed in 
            /// [hooks](#lifecycle-hooks) and [observers](#bean-observers) (such as cascading delete, etc).
            /// 
            /// There are special cases when you may need to turn this behavior off 
            /// (for example when using [LOCK TABLES with InnoDB](https://dev.mysql.com/doc/refman/5.0/en/lock-tables-and-transactions.html)):
#if CODE
            api.ImplicitTransactions = false;
#endif
        }

        class BeanObservers {
            /// ## Bean Observers      
            /// Bean observers have the same purpose as [Lifecycle Hooks](#lifecycle-hooks) with the difference that former
            /// are invoked for all beans. With observers you can implement plugins and extensions.
            /// 
            /// For example, let's make so that all beans have GUID keys insted of integer auto-increments:
#if CODE
            class GuidKeyObserver : BeanObserver {
                public override void BeforeStore(Bean bean) {
                    if(bean["id"] == null)
                        bean["id"] = Guid.NewGuid();
                }
            }
#endif
            void Snippet1(BeanApi api) { 
#if CODE
                api.DefaultKey(false); // turn off auto-increment keys
                api.AddObserver(new GuidKeyObserver());

                // but beware of http://www.informit.com/articles/printerfriendly/25862
#endif
            }
            /// Another example is adding automatic timestamps:
#if CODE
            class TimestampObserver : BeanObserver {
                public override void AfterDispense(Bean bean) {
                    bean["created_at"] = DateTime.Now;
                }
                public override void BeforeStore(Bean bean) {
                    bean["updated_at"] = DateTime.Now;
                }
            }
#endif
        }

        class BeanApiObjectLifetime { 
            /// ## BeanApi Object Lifetime
            /// The `BeanApi` class implements `IDisposable` (it holds the `DbConnection`) and is not thread-safe.
            /// Care should be taken to ensure that the same `BeanApi` and `DbConnection` instance is not used from multiple threads without
            /// synchronization, and that it is properly disposed. Let's consider some common usage scenarios.
            /// 
            /// ### Local Usage
            /// If LimeBean is used locally, then it should be enclosed in a `using` block:
            void LocalUsage(string connectionString, Type connectionType) {
#if CODE
                using(var api = new BeanApi(connectionString, connectionType)) {
                    api.EnterFluidMode();
                     
                    // work with beans
                }
#endif
            }
            /// ### Global Singleton
            /// For simple applications like console tools, you can use a single globally available statiс instance:
#if CODE
            class Globals {
                public static readonly BeanApi MyBeanApi;

                static Globals() {
                    MyBeanApi = new BeanApi("connection string", SQLiteFactory.Instance);
                    MyBeanApi.EnterFluidMode();
                }
            }
#endif
            /// In case of multi-threading, synchronize operations with `lock` or other techniques.
            /// 
            /// ### Web Applications (classic)
            /// In a classic ASP.NET app, create one `BeanApi` per web request. 
            /// You can use a Dependency Injection framework which supports per-request scoping,
            /// or do it manually like shown below:
#if CODE
            // This is your Global.asax file
            public class Global : HttpApplication {
                const string MY_BEAN_API_KEY = "bYeU3kLOQgGiWqUIql7Hqg"; // any unique value

                public static BeanApi MyBeanApi {
                    get { return (BeanApi)HttpContext.Current.Items[MY_BEAN_API_KEY]; }
                    set { HttpContext.Current.Items[MY_BEAN_API_KEY] = value; }
                }

                protected void Application_BeginRequest(object sender, EventArgs e) {
                    MyBeanApi = new BeanApi("connection string", SQLiteFactory.Instance);
                    MyBeanApi.EnterFluidMode();
                }

                protected void Application_EndRequest(object sender, EventArgs e) {
                    MyBeanApi.Dispose();
                }

            }
#endif
            class NetCore {
                /// ### ASP.NET Core Applications
                /// Subclass `BeanApi` and register it as a **scoped** service in the Startup.cs file:

                #region Fake ASP
                public interface IServiceCollection {
                    void AddScoped<T>();
                }
                public interface IActionResult { }
                public class ViewResult : IActionResult { 
                }
                public class Controller {
                    protected dynamic ViewBag = null;
                }
                #endregion

                interface SqliteConnection { }
#if CODE
                public class MyBeanApi : BeanApi {
                    public MyBeanApi()
                        : base("data source=data.db", typeof(SqliteConnection)) {
                        EnterFluidMode();
                    }
                }

                public class Startup {
                    public void ConfigureServices(IServiceCollection services) {
                        // . . .
                        services.AddScoped<MyBeanApi>();
                    }
                }
#endif
                /// Then inject it into any controller:
#if CODE
                public class HomeController : Controller {
                    BeanApi _beans;

                    public HomeController(MyBeanApi beans) {
                        _beans = beans;
                    }

                    public IActionResult Index() {
                        ViewBag.Books = _beans.Find("book", "ORDER BY title");
                        return new ViewResult();
                    }
                }
#endif
            
            }
        }

        void InternalQueryCache(BeanApi api) {
            /// ## Internal Query Cache
            /// Results of all recent read-only SQL queries initiated by 
            /// [finder](#finding-beans-with-sql) and [generic query](#generic-sql-queries) functions are cached internally 
            /// on the *least recently used* (LRU) basis. This saves database round trips during repeated reads.
            /// 
            /// The number of cached results can be adjusted by setting the `CacheCapacity` property:
#if CODE
            // increase
            api.CacheCapacity = 500;

            // turn off completely
            api.CacheCapacity = 0;
#endif
            /// Cache is fully invalidated (cleared) on:
            /// 
            /// * any non-readonly query (UPDATE, etc)
            /// * failed [transaction](#transactions) 
            /// 
            /// In rare special cases you may need to **bypass** the cache. 
            /// For this purpose, all query functions provide overloads with the `useCache` argument:
#if CODE
            var uid = api.Cell<string>(false, "select hex(randomblob(16))");
#endif
        }
    }

}

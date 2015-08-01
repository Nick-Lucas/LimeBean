using System;
using System.Linq;
using System.Data.Common;
using System.Data.SQLite;
using System.Web;

namespace LimeBean.Website {

    class Body {
        /// ## About LimeBean
        /// LimeBean provides simple and concise API for accessing ADO.NET data sources.
        /// It' compatible with .NET framework, ASP.NET 5 (DNX / DNXCore), Mono and Xamarin.
        /// Supported databases include **SQLite**, **MySQL/MariaDB** and **SQL Server**.
        /// 
        /// The library is inspired by [RedBeanPHP](http://redbeanphp.com).
        /// 
        /// ## Installation
        /// LimeBean is available on [NuGet Gallery](https://www.nuget.org/packages/LimeBean):
        /// 
        ///     PM> Install-Package LimeBean
        ///     
        /// For ASP.NET 5 projects, add a dependency to the project.json file:
        /// 
        ///     {
        ///         "dependencies": {
        ///             "LimeBean": "0.3"
        ///         }
        ///     }

        void ConnectToDatabase(BeanApi api, DbConnection connection) {
            /// ## Connect to Database
            /// LimeBean needs an ADO.NET driver to work with. Use one of the following:
            /// 
            /// * [System.Data.SQLite](https://www.nuget.org/packages/System.Data.SQLite), 
            ///   [Mono.Data.Sqlite](http://www.mono-project.com/download/) or
            ///   [Microsoft.Data.Sqlite](https://www.nuget.org/packages/Microsoft.Data.SQLite/)
            ///   for SQLite
            /// * [MySql.Data](https://www.nuget.org/packages/MySql.Data/) for MySQL or MariaDB
            /// * [System.Data.SqlClient](https://msdn.microsoft.com/en-us/library/System.Data.SqlClient.aspx) for SQL Server
            /// 
            /// Create an instance of the `BeanApi` class:
#if CODE
            // Using a connection string and an ADO.NET provider factory
            api = new BeanApi("data source=/path/to/db", SQLiteFactory.Instance);

            // Using a connection string and a connection type
            api = new BeanApi("data source=/path/to/db", typeof(SQLiteConnection));

            // Using a shared opened connection
            api = new BeanApi(connection);
#endif
            /// **NOTE:** `BeanApi` class is `IDisposable`.
            /// 
            /// When `BeanApi` is created from a connection string, the underlying connection is initiated on the first usage and closed on dispose.
            /// Shared connections are used as-is, their state it not changed.
            /// 
            /// See also: [BeanApi Object Lifetime](#beanapi-object-lifetime)
        }

        void CRUD(BeanApi api) {
            /// ## CRUD 
            /// For basic usage, LimeBean is ready to go. Zero configuration and no additional code!
            /// When operating in [fluid mode](#fluid-mode), 
            /// database schema is maintained on-the-fly, behind the scenes: 
            /// you don't need to create tables and columns by yourself.
            /// 
            /// Take a look at the following example:
            /// 
#if CODE
            // Enter the fluid mode (use during development only!)
            api.EnterFluidMode();

            // Create a bean. "Bean" means "data record", "dispense" means "instantiate new".
            var bean = api.Dispense("book");

            // Each bean has a kind. "Kind" is a synonym for "table name"
            var kind = bean.GetKind();
            Console.WriteLine(kind);

            // Fill it with some data
            bean["title"] = "Three Comrades";
            bean["rating"] = 10;

            // Store it
            // Table "book" with 2 columns, one string and one integer, will be generated automatically
            var id = api.Store(bean);

            // Each saved bean has an ID, or primary key
            Console.WriteLine(id);

            // Load back
            bean = api.Load("book", id);

            // Make some edits
            bean["title"] = "Learn LimeBean";
            bean["release_date"] = new DateTime(2015, 7, 30);
            bean["rating"] = "good";

            // Save updated bean
            // One new column ("release_date") will be added
            // The type of column "rating" will be expanded from integer to string
            api.Store(bean);

            // Or delete it
            api.Trash(bean);

            // Close the connection
            api.Dispose();
#endif
        }

        void TypedAccessors(Bean bean) {
            /// ## Typed Accessors
            /// Data values are stored within a bean as `IConvertible` which is a common interface implemented by
            /// all primitive types. In most cases however, you need specific types, such as strings or numbers.
            /// Beans provide the `Get<T>` method for this purpose:
            /// 
#if CODE
            bean.Get<string>("title");
            bean.Get<decimal>("price");
#endif
            /// Nullable values are supported too:
            /// 
#if CODE
            bean.GetNullable<bool>("flag");
#endif
            /// And there is a companion `Put` method which is chainable:
            /// 
#if CODE
            bean
                .Put("name", "Jane Doe")
                .Put("comment", null);
#endif
            /// See also: [Custom Bean Classes](#custom-bean-classes)
        }

        void FluidMode(BeanApi api) { 
            /// ## Fluid Mode
            /// LimeBean tries to mitigate the common inconvenience associated with relational databases,
            /// that is necessity to manually create tables, columns and adjust their data types. 
            /// In this sense, LimeBean takes SQL databases a little closer to NoSQL ones like MongoDB.
            /// 
            /// Fluid mode is optional, and is recommended for use only during early development stages
            /// (particularly for prototyping and scaffolding).
            /// To enable it, invoke the `EnterFluidMode` method on the `BeanApi` object:
#if CODE
            api.EnterFluidMode();
#endif
            /// How does it work? When you save the next bean, LimeBean analyzes its fields and compares 
            /// their names and types to the database schema. 
            /// If new data cannot be stored to an existing table, schema alteration occurs.
            /// LimeBean can create new tables, add missing columns, and widen data types.
            /// It will never truncate data or delete unused columns.
            /// 
            /// **NOTE:** LimeBean cannot detect renamings.
            /// 
            /// Automatically generated schema is usually sub-optimal and lacks indexes which are essential
            /// for performance. When most of planned tables are already in place, 
            /// and only minor changes are expected, 
            /// it is recommended to turn fluid mode off, audit the database structure and make further schema
            /// changes with a dedicated database management tool (like HeidiSQL, SSMS, etc).
            /// 
        }

        void FindingBeansWithSql(BeanApi api) {
            /// ## Finding Beans with SQL
            /// LimeBean doesn't introduce any custom query language, nor does it implement a LINQ provider. 
            /// To find beans matching a criteria, use snippets of plain SQL:
            /// 
            {
#if CODE
                var list = api.Find("book", "WHERE rating > 7");
#endif
            }
            /// Instead of embedding values into SQL code, it is recommended to use parameters:
            {
#if CODE
                var list = api.Find("book", "WHERE rating > {0}", 7);
#endif
            }
            /// Usage of parameters look similar to `String.Format`, but instead of direct interpolation,
            /// they are transformed into fair ADO.NET command parameters to protect your queries from injection-attacks.
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
            /// You can use any SQL as long as the result is a set of beans. 
            /// For other cases, see [generic queries](#generic-sql-queries).
            /// 
            /// To find a single bean:
#if CODE
            var best = api.FindOne("book", "ORDER BY rating DESC LIMIT 1");
#endif
            /// To find out the number of beans without loading them:
#if CODE
            var count = api.Count("book", "WHERE rating > {0}", 7);
#endif
            /// It is also possible to perform unbuffered load for processing in a foreach-loop:
#if CODE
            foreach(var bean in api.FindIterator("book", "ORDER BY rating")) {
                // do something with bean
            }
#endif
        }

        class CustomBeanClasses {
            /// ## Custom Bean Classes
            /// It is convenient to inherit from the base `Bean` class:
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
            /// - You take advantage of compile-time checks, IDE assistance and [strong-typed properties](#typed-accessors).
            /// - With [lifecycle hooks](#lifecycle-hooks), it is easy to implement [data validation](#data-validation) and [relations](#relations).
            /// 
            /// For custom beans classes, use method overloads with a generic parameter:
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

        class LifecycleHooks { 
            /// ## Lifecycle Hooks
            /// In [custom Bean classes](#custom-bean-classes) you can override lifecycle hook methods to receive 
            /// notifications about [CRUD operations](#crud) occurring to this bean:
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

        void PrimaryKeys(BeanApi api) { 
            /// ## Primary Keys
            /// By default, all beans have auto-increment integer key named `"id"`. It is possible 
            /// to customize keys in all aspects:
#if CODE
            // Custom key name for beans of kind "book"
            api.Key("book", "book_id");
            
            // Custom non-autoincrement key
            api.Key("book", "book_id", false);
            
            // Compound key `(order_id, product_id)`
            api.Key("order_item", "order_id", "product_id");
#endif
            /// **NOTE:** non auto-increment keys must be assigned manually prior to saving.
        }

        void GenericSqlQueries(BeanApi api) {
            /// ## Generic SQL Queries
            /// Often it's needed to execute queries which don't map to beans. 
            /// Examples include aggregation, grouping, joins, selecting single column, etc.
            /// 
            /// `BeanApi` provides methods for such tasks:
            /// 
            {
#if CODE
                // Load multiple rows
                var rows = api.Rows("SELECT author, COUNT(*) FROM book WHERE rating > {0} GROUP BY author", 7);
      
                // Load a single row
                var row = api.Row("SELECT author, COUNT(*) FROM book GROUP BY author ORDER BY COUNT(*) DESC LIMIT 1");
      
                // Load a column
                var col = api.Col<string>("SELECT DISTINCT author FROM book ORDER BY author");
      
                // Load a single value
                var count = api.Cell<int>("SELECT COUNT(*) FROM book");
#endif
            }
            /// For `Rows` and `Col`, there are unbuffered counterparts:
            {
#if CODE
                foreach(var row in api.RowsIterator("...")) {
                    // do something
                }

                foreach(var item in api.ColIterator<string>("...")) {
                    // do something
                }
#endif
            }
            /// To execute a non-query SQL command, use the `Exec` function:
#if CODE
            api.Exec("SET autocommit = 0");
#endif
            /// All methods accept parameters in the same form as [finder methods](#finding-beans-with-sql) do.
        }

        class DataValidation {
            /// ## Data Validation
            /// The `BeforeStore` [hook](#lifecycle-hooks) can be used to prevent bean from storing under certain
            /// circumstances. For example, let's define a bean `Book` which cannot be stored unless 
            /// it has a non-empty title:
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
            /// See also: [Custom Bean Classes](#custom-bean-classes), [Bean Observers](#bean-observers)
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
            class Globals {
                public static BeanApi LimeBean { get; set; }
            }
            /// Let's link them so that a product knows its category, and a category can list all its products. 
            /// Assume that we can access the `BeanApi` object via the globally available `Globals.LimeBean` property
            /// ([read how to accomplish that](#beanapi-object-lifetime)).
            /// 
            /// In the `Product` class, let's declare a method `GetCategory()`:
#if CODE
            partial class Product {
                public Category GetCategory() {
                    return Globals.LimeBean.Load<Category>(this["category_id"]);
                }
            }
#endif
            /// In the `Category` class, we'll add a method named `GetProducts()`:
#if CODE
            partial class Category {
                public Product[] GetProducts() {
                    return Globals.LimeBean.Find<Product>("where category_id = {0}", this[ID_PROP_NAME]);
                }
            }
#endif
            /// **NOTE:** LimeBean uses the [internal query cache](#internal-query-cache), therefore repeated `Load` and `Find` calls will not hit the database.
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
                        Globals.LimeBean.Trash(p);
                }
#endif
            }

            /// **NOTE:** `Store` and `Trash` always run in a transaction (see [Implicit Transactions](#implicit-transactions)),
            /// therefore even if something goes wrong inside the cascading deletion loop, database will remain in a consistent state!
        }

        
        /// ## Bean Observers      
        /// TODO
        
        /// ## Transactions
        /// TODO
        
        /// ## Implicit Transactions
        /// TODO

        class BeanApiObjectLifetime { 
            /// ## BeanApi Object Lifetime
            /// The `BeanApi` class is `IDisposable` (because it holds a `DbConnection`) and is not thread-safe.
            /// Care should be taken to ensure that the same `BeanApi` is not used from multiple threads without
            /// synchronization, and that it is properly disposed. Let's consider some common usage scenarios.
            /// 
            /// ### Local Usage
            /// If LimeBean is used locally, then it should be enclosed in a `using` block:
            void LocalUsage(string connectionString, Type connectionType) {
#if CODE
                using(var api = new BeanApi(connectionString, connectionType)) { 
                    // work with beans
                }
#endif
            }
            /// ### Global Singleton
            /// For simple applications like console tools, you can use a single globally available statiс instance:
#if CODE
            class Globals { 
                public static readonly BeanApi LimeBean = new BeanApi("connection string", SQLiteFactory.Instance);
            }
#endif
            /// In case of multi-threading, synchronize operations with `lock` or other techniques.
            /// 
            /// ### Web Applications
            /// In a web app (ASP.NET, etc) use one `BeanApi` per web request. 
            /// You can use a Dependency Injection framework which supports per-request scoping,
            /// or do it manually like shown below:
#if CODE
            // This is your Global.asax file
            public class Global : HttpApplication {
                const string LIME_BEAN_KEY = "bYeU3kLOQgGiWqUIql7Hqg"; // any unique value

                public static BeanApi LimeBean {
                    get { return (BeanApi)HttpContext.Current.Items[LIME_BEAN_KEY]; }
                    set { HttpContext.Current.Items[LIME_BEAN_KEY] = value; }
                }

                protected void Application_BeginRequest(object sender, EventArgs e) {
                    LimeBean = new BeanApi("connection string", SQLiteFactory.Instance);                   
                }

                protected void Application_EndRequest(object sender, EventArgs e) {
                    LimeBean.Dispose();
                }

            }
#endif
        }
    
        /// ## Internal Query Cache
        /// TODO
    }

}

using System;
using System.Data.Common;
using System.Data.SQLite;

namespace LimeBean.Website {

    class Body {
        
        /// ## About LimeBean
        /// LimeBean provides simple and concise API for accessing ADO.NET data sources.
        /// Compatible with conventional .NET, ASP.NET 5 (DNX / DNXCore), Mono and Xamarin.
        /// Supports **SQLite**, **MySQL/MariaDB** and **SQL Server**.
        /// 
        /// The library is inspired by [RedBeanPHP](http://redbeanphp.com).
        /// 
        /// ## Installation
        /// LimeBean is available on [NuGet Gallery](https://www.nuget.org/packages/LimeBean).
        /// 
        /// In Visual Studio, use the "Manage NuGet packages..." dialog or Package Manger Console:
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
        ///     

#warning fast prototyping, like mongo
        
        void Connect(BeanApi api, DbConnection connection) {
            /// ## Connect to Database
            /// 
#if CODE
            // Using an ADO.NET provider factory
            api = new BeanApi("data source=/path/to/db", SQLiteFactory.Instance);

            // Using a connection type
            api = new BeanApi("data source=/path/to/db", typeof(SQLiteConnection));

            // Using a shared opened connection
            api = new BeanApi(connection);
#endif
            /// `BeanApi` object is `IDisposable`.
            /// When created from a connection string, the underlying connection is closed on dispose.
            /// Shared connections remain opened.
            /// 
#warning thread-safety, usage in web, IoC
        }

        void CRUD(BeanApi api) {
            /// ## CRUD 
            /// For basic usage, LimeBean is ready to go! You don't need any additional configuration or code. 
            /// Database schema is maintained behind the scenes, on-the-fly (in *fluid mode* only).
            /// 
#if CODE
            // Enter the fluid mode (use during development only!)
            api.EnterFluidMode();

            // Create a bean. "Bean" means "data record", "dispense" means "instantiate new".
            var bean = api.Dispense("person");

            // Each bean has kind. "Kind" is a synonym for "table name"
            Console.WriteLine(bean.GetKind());

            // Fill it with some data
            bean["first_name"] = "John";
            bean["last_name"] = "Doe";
            bean["birthdate"] = new DateTime(1990, 9, 9);

            // Store it
            // Table will be created automatically
            var id = api.Store(bean);

            // Load back
            bean = api.Load("person", id);

            // Change existing fields and add a couple of new
            bean["first_name"] = "Jane";
            bean["email"] = "jane@example.com";
            bean["email_hide"] = true;

            // Save updated bean
            // Additional columns will be added automatically
            api.Store(bean);

            // Or delete it
            api.Trash(bean);

            // Close the connection
            api.Dispose();
#endif
        }

        /// ## To Be Continued...
        /// I'll be adding more content gradually. In the meantime check [examples on GitHub](https://github.com/AlekseyMartynov/LimeBean/tree/master/LimeBean.Tests/Examples).
    }

}

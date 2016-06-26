using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace LimeBean {

    public partial class BeanApi : IDisposable, IBeanCrud, IBeanFinder, IDatabaseAccess, IValueRelaxations {
        ConnectionContainer _connectionContainer;
        IDatabaseDetails _details;
        IDatabaseAccess _db;
        KeyUtil _keyUtil;
        DatabaseStorage _storage;
        IBeanCrud _crud;
        IBeanFinder _finder;

        public BeanApi(string connectionString, DbProviderFactory factory) {
            _connectionContainer = new ConnectionContainer.LazyImpl(connectionString, factory.CreateConnection);
        }

        public BeanApi(DbConnection connection) {
            _connectionContainer = new ConnectionContainer.SimpleImpl(connection);
        }

        public BeanApi(string connectionString, Type connectionType) {
            _connectionContainer = new ConnectionContainer.LazyImpl(connectionString, delegate() {
                return (DbConnection)Activator.CreateInstance(connectionType);
            });
        }


        // Properties

        public DbConnection Connection {
            get { return _connectionContainer.Connection; }
        }

        IDatabaseDetails Details {
            get {
                if(_details == null)
                    _details = CreateDetails();
                return _details;
            }
        }

        IDatabaseAccess Db {
            get {
                if(_db == null)
                    _db = new DatabaseAccess(Connection, Details);
                return _db;
            }
        }

        KeyUtil KeyUtil { 
            get {
                if(_keyUtil == null)
                    _keyUtil = new KeyUtil();
                return _keyUtil;
            }
        }

        DatabaseStorage Storage {
            get {
                if(_storage == null)
                    _storage = new DatabaseStorage(Details, Db, KeyUtil);
                return _storage;
            }
        }

        IBeanCrud Crud {
            get {
                if(_crud == null) {
                    _crud = new BeanCrud(Storage, Db, KeyUtil);
                    _crud.AddObserver(new BeanApiLinker(this));
                }
                return _crud;
            }
        }

        IBeanFinder Finder {
            get {
                if(_finder == null)
                    _finder = new DatabaseBeanFinder(Details, Db, Crud);
                return _finder;
            }
        }

        internal IDatabaseDetails CreateDetails() { 
            switch(Connection.GetType().FullName) { 
                case "System.Data.SQLite.SQLiteConnection":
                case "Microsoft.Data.Sqlite.SqliteConnection":
                case "Mono.Data.Sqlite.SqliteConnection":
                    return new SQLiteDetails();

#if !NO_MARIADB
                case "MySql.Data.MySqlClient.MySqlConnection":
                    return new MariaDbDetails();
#endif

#if !NO_MSSQL
                case "System.Data.SqlClient.SqlConnection":
                    return new MsSqlDetails();
#endif

#if !NO_PGSQL
                case "Npgsql.NpgsqlConnection":
                    return new PgSqlDetails();
#endif
            }

            throw new NotSupportedException();
        }


        // Methods

#if !DEBUG
        [Obsolete("Use Fluid Mode in DEBUG mode only!")]
#endif
        /// <summary>
        /// Use LimeBean in 'Fluid Mode' which will auto create missing 
        /// configuration (ie. columns) which you are trying to interact with in the Database
        /// </summary>
        public void EnterFluidMode() {
            Storage.EnterFluidMode();
        }

        public void Dispose() {
            _connectionContainer.Dispose();
        }


        // IBeanCrud

        /// <summary>
        /// Default True. Gets or Sets whether changes to a Bean are tracked per Column.
        /// When True, only Columns which are changed will be Updated on Store. Otherwise all Columns are updated
        /// </summary>
        public bool DirtyTracking {
            get { return Crud.DirtyTracking; }
            set { Crud.DirtyTracking = value; }
        }

        /// <summary>
        /// Registers a class implementing BeanObserver to recieve 
        /// notifications whenever Crud actions are applied to the database
        /// </summary>
        /// <param name="observer">A subclass of BeanObserver</param>
        public void AddObserver(BeanObserver observer) {
            Crud.AddObserver(observer);
        }

        /// <summary>
        /// Deregisters a class implementing BeanObserver from recieving
        /// notifications whenever Crud actions are applied to the database
        /// </summary>
        /// <param name="observer">A subclass of BeanObserver</param>
        public void RemoveObserver(BeanObserver observer) {
            Crud.RemoveObserver(observer);
        }

        /// <summary>
        /// Create a new Bean of a given Kind (table name)
        /// </summary>
        /// <param name="kind">The name of a table to create a Bean for</param>
        /// <returns>A Bean representing the requested Kind (table name)</returns>
        public Bean Dispense(string kind) {
            return Crud.Dispense(kind);
        }

        /// <summary>
        /// Create a new Bean of a given Bean subclass
        /// </summary>
        /// <typeparam name="T">A subclass of Bean representing a Bean Kind (table)</typeparam>
        /// <returns>A Bean representing the requested Bean Kind (table)</returns>
        public T Dispense<T>() where T : Bean, new() {
            return Crud.Dispense<T>();
        }

        /// <summary>
        /// Create a new Bean of a given Kind (table) and populate it with a given data set
        /// </summary>
        /// <param name="kind">The name of a table to create the Bean for</param>
        /// <param name="row">The data to populate the Bean with</param>
        /// <returns>A Bean of the given Kind (table) populated with the given data</returns>
        public Bean RowToBean(string kind, IDictionary<string, object> row) {
            return Crud.RowToBean(kind, row);
        }

        /// <summary>
        /// Create a new Bean of a given subclass representing a given Kind (table), and populate it with a given data set
        /// </summary>
        /// <typeparam name="T">A subclass of Bean representing a Bean Kind (table)</typeparam>
        /// <param name="row">The data to populate the Bean with</param>
        /// <returns>A Bean of the given subclass populated with the given data</returns>
        public T RowToBean<T>(IDictionary<string, object> row) where T : Bean, new() {
            return Crud.RowToBean<T>(row);
        }

        /// <summary>
        /// Query a Bean (row) from the Database
        /// </summary>
        /// <param name="kind">The Kind (table) to query</param>
        /// <param name="key">The value of the primary key on the required row</param>
        /// <returns>A new Bean representing the requested row from the database</returns>
        public Bean Load(string kind, object key) {
            return Crud.Load(kind, key);
        }

        /// <summary>
        /// Query a Bean (row) of a given subclass from the Database
        /// </summary>
        /// <typeparam name="T">The Bean subclass to query</typeparam>
        /// <param name="key">The value of the primary key on the required row</param>
        /// <returns>A new Bean of the given subclass representing the requested row from the database</returns>
        public T Load<T>(object key) where T : Bean, new() {
            return Crud.Load<T>(key);
        }

        /// <summary>
        /// Query a Bean (row) from the Database
        /// </summary>
        /// <param name="kind">The Kind (table) to query</param>
        /// <param name="key">An array of the multi-column primary key values on the required row</param>
        /// <returns>A new Bean representing the requested row from the database</returns>
        public Bean Load(string kind, params object[] compoundKey) {
            return Load(kind, KeyUtil.PackCompoundKey(kind, compoundKey));
        }

        /// <summary>
        /// Query a Bean (row) of a given subclass from the Database
        /// </summary>
        /// <typeparam name="T">The Bean subclass to query</typeparam>
        /// <param name="key">An array of the multi-column primary key values on the required row</param>
        /// <returns>A new Bean of the given subclass representing the requested row from the database</returns>
        public T Load<T>(params object[] compoundKey) where T : Bean, new() {
            return Load<T>(KeyUtil.PackCompoundKey(Bean.GetKind<T>(), compoundKey));
        }

        /// <summary>
        /// Save a given Bean to the database. Insert or Update a record as appropriate
        /// </summary>
        /// <param name="bean">A Bean or subclass thereof</param>
        /// <returns>The primary key(s) for the stored Bean</returns>
        public object Store(Bean bean) {
            return Crud.Store(bean);
        }

        /// <summary>
        /// Delete the underlying record of a Bean (row) from the database
        /// </summary>
        /// <param name="bean">A Bean (row) or subclass thereof</param>
        public void Trash(Bean bean) {
            Crud.Trash(bean);
        }


        // IBeanFinder

        /// <summary>
        /// Query the database for one or more Beans (rows) which match the given filter conditions
        /// </summary>
        /// <param name="useCache">When true, Beans will be queried from the cache first 
        /// and stored if not already cached. When false any cached Beans will be removed</param>
        /// <param name="kind">The Kind (table) to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placholdered like in String.Format(...)</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans which meet the given query conditions</returns>
        public Bean[] Find(bool useCache, string kind, string expr = null, params object[] parameters) {
            return Finder.Find(useCache, kind, expr, parameters);
        }

        /// <summary>
        /// Query the database for one or more Beans (rows) of the given subclass which match the given filter conditions
        /// </summary>
        /// <param name="useCache">When true, Beans will be queried from the cache first 
        /// and stored if not already cached. When false any cached Beans will be removed</param>
        /// <param name="kind">The Kind (table) to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placholdered like in String.Format(...)</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans of the given subclass which meet the given query conditions</returns>
        public T[] Find<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new() {
            return Finder.Find<T>(useCache, expr, parameters);
        }

        /// <summary>
        /// Query the database for one or more Beans (rows) which match the given filter conditions. Uses caching.
        /// </summary>
        /// <param name="kind">The Kind (table) to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placholdered like in String.Format(...)</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans which meet the given query conditions</returns>
        public Bean[] Find(string kind, string expr = null, params object[] parameters) {
            return Find(true, kind, expr, parameters);
        }

        /// <summary>
        /// Query the database for one or more Beans (rows) of the given subclass which match the given filter conditions. Uses caching.
        /// </summary>
        /// <param name="kind">The Kind (table) to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placholdered like in String.Format(...)</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans of the given subclass which meet the given query conditions</returns>
        public T[] Find<T>(string expr = null, params object[] parameters) where T : Bean, new() {
            return Find<T>(true, expr, parameters);
        }

        /// <summary>
        /// Query the database for the first Bean (row) which matches the given filter conditions
        /// </summary>
        /// <param name="useCache">When true, Beans will be queried from the cache first 
        /// and stored if not already cached. When false any cached Beans will be removed</param>
        /// <param name="kind">The Kind (table) to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placholdered like in String.Format(...)</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans which meet the given query conditions</returns>
        public Bean FindOne(bool useCache, string kind, string expr = null, params object[] parameters) {
            return Finder.FindOne(useCache, kind, expr, parameters);
        }

        /// <summary>
        /// Query the database for the first Bean (rows) of the given subclass which matches the given filter conditions
        /// </summary>
        /// <param name="useCache">When true, Beans will be queried from the cache first 
        /// and stored if not already cached. When false any cached Beans will be removed</param>
        /// <param name="kind">The Kind (table) to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placholdered like in String.Format(...)</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans of the given subclass which meet the given query conditions</returns>
        public T FindOne<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new() {
            return Finder.FindOne<T>(useCache, expr, parameters);
        }

        /// <summary>
        /// Query the database for the first Bean (row) which matches the given filter conditions. Uses caching.
        /// </summary>
        /// <param name="kind">The Kind (table) to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placholdered like in String.Format(...)</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans which meet the given query conditions</returns>
        public Bean FindOne(string kind, string expr = null, params object[] parameters) {
            return FindOne(true, kind, expr, parameters);
        }

        /// <summary>
        /// Query the database for the first Bean (rows) of the given subclass which matches the given filter conditions. Uses cachine
        /// </summary>
        /// <param name="kind">The Kind (table) to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placholdered like in String.Format(...)</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans of the given subclass which meet the given query conditions</returns>
        public T FindOne<T>(string expr = null, params object[] parameters) where T : Bean, new() {
            return FindOne<T>(true, expr, parameters);
        }

        /// <summary>
        /// Query the database for one or more Beans (rows) which match the given filter conditions. No caching.
        /// </summary>
        /// <param name="kind">The Kind (table) to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placholdered like in String.Format(...)</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An IEnumerable of Beans which meet the given query conditions</returns>
        public IEnumerable<Bean> FindIterator(string kind, string expr = null, params object[] parameters) {
            return Finder.FindIterator(kind, expr, parameters);
        }

        /// <summary>
        /// Query the database for one or more Beans (rows) of a given subclass, which match the given filter conditions. No caching.
        /// </summary>
        /// <param name="kind">The Kind (table) to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placholdered like in String.Format(...)</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An IEnumerable of the given Bean subclass which meet the given query conditions</returns>
        public IEnumerable<T> FindIterator<T>(string expr = null, params object[] parameters) where T : Bean, new() {
            return Finder.FindIterator<T>(expr, parameters);
        }

        /// <summary>
        /// Count the number of rows which match the given filter conditions on the given Kind (table)
        /// </summary>
        /// <param name="useCache">When true, Beans will be queried from the cache first 
        /// and stored if not already cached. When false any cached Beans will be removed</param>
        /// <param name="kind">The Kind (table) to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placholdered like in String.Format(...)</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>A count of the number of rows matching the given conditions</returns>
        public long Count(bool useCache, string kind, string expr = null, params object[] parameters) {
            return Finder.Count(useCache, kind, expr, parameters);            
        }

        /// <summary>
        /// Count the number of rows which match the given filter conditions on the Kind of the given Bean subclass
        /// </summary>
        /// <typeparam name="T">The Bean subclass which contains information of what Kind (table) to Count on</typeparam>
        /// <param name="useCache">When true, Beans will be queried from the cache first 
        /// and stored if not already cached. When false any cached Beans will be removed</param>
        /// <param name="kind">The Kind (table) to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placholdered like in String.Format(...)</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>A count of the number of rows matching the given conditions</returns>
        public long Count<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new() {
            return Finder.Count<T>(useCache, expr, parameters);
        }

        /// <summary>
        /// Count the number of rows which match the given filter conditions on the given Kind (table). Uses caching
        /// </summary>
        /// <param name="kind">The Kind (table) to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placholdered like in String.Format(...)</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>A count of the number of rows matching the given conditions</returns>
        public long Count(string kind, string expr = null, params object[] parameters) {
            return Count(true, kind, expr, parameters);
        }

        /// <summary>
        /// Count the number of rows which match the given filter conditions on the Kind of the given Bean subclass. Uses caching
        /// </summary>
        /// <typeparam name="T">The Bean subclass which contains information of what Kind (table) to Count on</typeparam>
        /// <param name="kind">The Kind (table) to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placholdered like in String.Format(...)</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>A count of the number of rows matching the given conditions</returns>
        public long Count<T>(string expr = null, params object[] parameters) where T : Bean, new() {
            return Count<T>(true, expr, parameters);
        }


        // IDatabaseAccess

        public event Action<DbCommand> QueryExecuting {
            add { Db.QueryExecuting += value; }
            remove { Db.QueryExecuting -= value; }
        }

        public int CacheCapacity {
            get { return Db.CacheCapacity; }
            set { Db.CacheCapacity = value; }
        }

        public int Exec(string sql, params object[] parameters) {
            return Db.Exec(sql, parameters);
        }

        public IEnumerable<T> ColIterator<T>(string sql, params object[] parameters) {
            return Db.ColIterator<T>(sql, parameters);
        }

        public IEnumerable<object> ColIterator(string sql, params object[] parameters) {
            return ColIterator<object>(sql, parameters);
        }

        public IEnumerable<IDictionary<string, object>> RowsIterator(string sql, params object[] parameters) {
            return Db.RowsIterator(sql, parameters);
        }

        public T Cell<T>(bool useCache, string sql, params object[] parameters) {
            return Db.Cell<T>(useCache, sql, parameters);
        }

        public T Cell<T>(string sql, params object[] parameters) {
            return Cell<T>(true, sql, parameters);
        }

        public object Cell(string sql, params object[] parameters) {
            return Cell<object>(sql, parameters);
        }

        public T[] Col<T>(bool useCache, string sql, params object[] parameters) {
            return Db.Col<T>(useCache, sql, parameters);
        }

        public T[] Col<T>(string sql, params object[] parameters) {
            return Col<T>(true, sql, parameters);
        }

        public object[] Col(string sql, params object[] parameters) {
            return Col<object>(true, sql, parameters);
        }

        public IDictionary<string, object> Row(bool useCache, string sql, params object[] parameters) {
            return Db.Row(useCache, sql, parameters);
        }

        public IDictionary<string, object>[] Rows(bool useCache, string sql, params object[] parameters) {
            return Db.Rows(useCache, sql, parameters);
        }

        public IDictionary<string, object> Row(string sql, params object[] parameters) {
            return Row(true, sql, parameters);
        }

        public IDictionary<string, object>[] Rows(string sql, params object[] parameters) {
            return Rows(true, sql, parameters);
        }


        // ITransactionSupport

        /// <summary>
        /// Gets or Sets whether a transaction will automatically be used on the CUD aspects of Crud a operation
        /// Implicit Transactions do not occur if a Transaction is currently being handled by this instance of BeanAPI
        /// </summary>
        public bool ImplicitTransactions {
            get { return Db.ImplicitTransactions; }
            set { Db.ImplicitTransactions = value; }
        }

        /// <summary>
        /// Gets whether there are any transactions currently being worked on
        /// </summary>
        public bool InTransaction {
            get { return Db.InTransaction; }
        }

        /// <summary>
        /// Gets or sets the IsolationLevel of Limebean database transactions
        /// </summary>
        public IsolationLevel TransactionIsolation {
            get { return Db.TransactionIsolation; }
            set { Db.TransactionIsolation = value; }
        }

        /// <summary>
        /// Wraps an Action in a database transaction. Anything done here will roll back and throw if any error occurs
        /// </summary>
        /// <param name="action">The process to take place</param>
        public void Transaction(Func<bool> action) {
            Db.Transaction(action);
        }

        /// <summary>
        /// Wraps an Action in a database transaction. Anything done here will roll back and throw if any error occurs
        /// </summary>
        /// <param name="action">The process to take place</param>
        public void Transaction(Action action) {
            Transaction(delegate () {
                action();
                return true;
            });
        }


        // IValueRelaxations

        public bool TrimStrings {
            get { return Storage.TrimStrings; }
            set { Storage.TrimStrings = value; }
        }

        public bool ConvertEmptyStringToNull {
            get { return Storage.ConvertEmptyStringToNull; }
            set { Storage.ConvertEmptyStringToNull = value; }
        }

        public bool RecognizeIntegers {
            get { return Storage.RecognizeIntegers; }
            set { Storage.RecognizeIntegers = value; }
        }


        // Custom keys

        public void Key(string kind, string name, bool autoIncrement) {
            KeyUtil.RegisterKey(kind, new[] { name }, autoIncrement);
        }

        public void Key(string kind, params string[] names) {
            KeyUtil.RegisterKey(kind, names, null);
        }

        public void Key<T>(string name, bool autoIncrement) where T : Bean, new() {
            Key(Bean.GetKind<T>(), name, autoIncrement);
        }

        public void Key<T>(params string[] names) where T : Bean, new() {
            Key(Bean.GetKind<T>(), names);
        }

        public void DefaultKey(bool autoIncrement) {
            KeyUtil.DefaultAutoIncrement = autoIncrement;
        }

        public void DefaultKey(string name, bool autoIncrement = true) {
            KeyUtil.DefaultName = name;
            KeyUtil.DefaultAutoIncrement = autoIncrement;
        }

    }

}

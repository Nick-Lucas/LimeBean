using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

using LimeBean.Interfaces;

namespace LimeBean {

    public partial class BeanApi : IBeanApi {
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
        /// configuration (ie. columns) which you are trying to interact with on the Database
        /// </summary>
        public void EnterFluidMode() {
            Storage.EnterFluidMode();
        }

        /// <summary>
        /// Dispose of any fully managed Database Connections. 
        /// Connections created outside of BeanAPI and passed in need to be manually disposed
        /// </summary>
        public void Dispose() {
            _connectionContainer.Dispose();
        }


        // IBeanCrud

        /// <summary>
        /// Gets or Sets whether changes to a Bean are tracked per column. Default true. 
        /// When true, only columns which are changed will be updated on Store(). Otherwise all columns are updated
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
        /// Create an empty Bean of a given Kind
        /// </summary>
        /// <param name="kind">The name of a table to create a Bean for</param>
        /// <returns>A Bean representing the requested Kind</returns>
        public Bean Dispense(string kind) {
            return Crud.Dispense(kind);
        }

        /// <summary>
        /// Create an empty Bean of a given Bean subclass
        /// </summary>
        /// <typeparam name="T">A subclass of Bean representing a Bean Kind</typeparam>
        /// <returns>A Bean representing the requested Bean Kind</returns>
        public T Dispense<T>() where T : Bean, new() {
            return Crud.Dispense<T>();
        }

        /// <summary>
        /// Create a new Bean of a given Kind and populate it with a given data set
        /// </summary>
        /// <param name="kind">The name of a table to create the Bean for</param>
        /// <param name="row">The data to populate the Bean with</param>
        /// <returns>A Bean of the given Kind populated with the given data</returns>
        public Bean RowToBean(string kind, IDictionary<string, object> row) {
            return Crud.RowToBean(kind, row);
        }

        /// <summary>
        /// Create a new Bean of a given subclass representing a given Kind, and populate it with a given data set
        /// </summary>
        /// <typeparam name="T">A subclass of Bean representing a Bean Kind</typeparam>
        /// <param name="row">The data to populate the Bean with</param>
        /// <returns>A Bean of the given subclass populated with the given data</returns>
        public T RowToBean<T>(IDictionary<string, object> row) where T : Bean, new() {
            return Crud.RowToBean<T>(row);
        }

        /// <summary>
        /// Query a Bean (row) from the Database
        /// </summary>
        /// <param name="kind">Name of the table to query</param>
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
        /// <param name="kind">Name of the table to query</param>
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
        /// Query the database for one or more Beans (rows) which match the given filter conditions. Prefer FindIterator() for large data sets. 
        /// </summary>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans which meet the given query conditions</returns>
        public Bean[] Find(bool useCache, string kind, string expr = null, params object[] parameters) {
            return Finder.Find(useCache, kind, expr, parameters);
        }

        /// <summary>
        /// Query the database for one or more Beans (rows) of the given subclass which match the given filter conditions. Prefer FindIterator() for large data sets. 
        /// </summary>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans of the given subclass which meet the given query conditions</returns>
        public T[] Find<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new() {
            return Finder.Find<T>(useCache, expr, parameters);
        }

        /// <summary>
        /// Query the database for one or more Beans (rows) which match the given filter conditions. Prefer FindIterator() for large data sets. Uses caching.
        /// </summary>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans which meet the given query conditions</returns>
        public Bean[] Find(string kind, string expr = null, params object[] parameters) {
            return Find(true, kind, expr, parameters);
        }

        /// <summary>
        /// Query the database for one or more Beans (rows) of the given subclass which match the given filter conditions. Prefer FindIterator() for large data sets. Uses caching.
        /// </summary>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans of the given subclass which meet the given query conditions</returns>
        public T[] Find<T>(string expr = null, params object[] parameters) where T : Bean, new() {
            return Find<T>(true, expr, parameters);
        }

        /// <summary>
        /// Query the database for the first Bean (row) which matches the given filter conditions
        /// </summary>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans which meet the given query conditions</returns>
        public Bean FindOne(bool useCache, string kind, string expr = null, params object[] parameters) {
            return Finder.FindOne(useCache, kind, expr, parameters);
        }

        /// <summary>
        /// Query the database for the first Bean (rows) of the given subclass which matches the given filter conditions
        /// </summary>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans of the given subclass which meet the given query conditions</returns>
        public T FindOne<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new() {
            return Finder.FindOne<T>(useCache, expr, parameters);
        }

        /// <summary>
        /// Query the database for the first Bean (row) which matches the given filter conditions. Uses caching.
        /// </summary>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans which meet the given query conditions</returns>
        public Bean FindOne(string kind, string expr = null, params object[] parameters) {
            return FindOne(true, kind, expr, parameters);
        }

        /// <summary>
        /// Query the database for the first Bean (rows) of the given subclass which matches the given filter conditions. Uses caching
        /// </summary>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans of the given subclass which meet the given query conditions</returns>
        public T FindOne<T>(string expr = null, params object[] parameters) where T : Bean, new() {
            return FindOne<T>(true, expr, parameters);
        }

        /// <summary>
        /// Query the database for one or more Beans (rows) which match the given filter conditions. Recommended for large data sets
        /// </summary>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An IEnumerable of Beans which meet the given query conditions</returns>
        public IEnumerable<Bean> FindIterator(string kind, string expr = null, params object[] parameters) {
            return Finder.FindIterator(kind, expr, parameters);
        }

        /// <summary>
        /// Query the database for one or more Beans (rows) of a given subclass, which match the given filter conditions
        /// </summary>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An IEnumerable of the given Bean subclass which meet the given query conditions</returns>
        public IEnumerable<T> FindIterator<T>(string expr = null, params object[] parameters) where T : Bean, new() {
            return Finder.FindIterator<T>(expr, parameters);
        }

        /// <summary>
        /// Count the number of rows which match the given expression on the given Kind
        /// </summary>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>A count of the number of rows matching the given conditions</returns>
        public long Count(bool useCache, string kind, string expr = null, params object[] parameters) {
            return Finder.Count(useCache, kind, expr, parameters);            
        }

        /// <summary>
        /// Count the number of rows which match the given filter conditions on the Kind of the given Bean subclass
        /// </summary>
        /// <typeparam name="T">The Bean subclass which contains information of what Kind to Count on</typeparam>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>A count of the number of rows matching the given conditions</returns>
        public long Count<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new() {
            return Finder.Count<T>(useCache, expr, parameters);
        }

        /// <summary>
        /// Count the number of rows which match the given filter conditions on the given Kind. Uses caching
        /// </summary>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>A count of the number of rows matching the given conditions</returns>
        public long Count(string kind, string expr = null, params object[] parameters) {
            return Count(true, kind, expr, parameters);
        }

        /// <summary>
        /// Count the number of rows which match the given filter conditions on the Kind of the given Bean subclass. Uses caching
        /// </summary>
        /// <typeparam name="T">The Bean subclass which contains information of what Kind to Count on</typeparam>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>A count of the number of rows matching the given conditions</returns>
        public long Count<T>(string expr = null, params object[] parameters) where T : Bean, new() {
            return Count<T>(true, expr, parameters);
        }


        // IDatabaseAccess

        /// <summary>
        /// Event which fires at the point of execution of any database query
        /// </summary>
        public event Action<DbCommand> QueryExecuting {
            add { Db.QueryExecuting += value; }
            remove { Db.QueryExecuting -= value; }
        }

        /// <summary>
        /// Gets or sets the number of recent queries which have their results cached
        /// </summary>
        public int CacheCapacity {
            get { return Db.CacheCapacity; }
            set { Db.CacheCapacity = value; }
        }

        /// <summary>
        /// Execute a given SQL 'Non Query' on the database
        /// </summary>
        /// <param name="sql">The SQL to execute, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>The number of rows affected if applicable, otherwise -1</returns>
        public int Exec(string sql, params object[] parameters) {
            return Db.Exec(sql, parameters);
        }

        /// <summary>
        /// Execute a SQL Query and return the first column as the specified type. Lazy loads each row when iterated on. 
        /// </summary>
        /// <typeparam name="T">The Type to return each value as</typeparam>
        /// <param name="sql">A SQL Query ideally returning a single column, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>The value in the first returned column, as the specified type</returns>
        public IEnumerable<T> ColIterator<T>(string sql, params object[] parameters) {
            return Db.ColIterator<T>(sql, parameters);
        }

        /// <summary>
        /// Execute a SQL Query and return the first column as an object. Lazy loads each row when iterated on. 
        /// </summary>
        /// <typeparam name="T">The Type to return each value as</typeparam>
        /// <param name="sql">A SQL Query ideally returning a single column, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>The value in the first returned column, as the specified type</returns>
        public IEnumerable<object> ColIterator(string sql, params object[] parameters) {
            return ColIterator<object>(sql, parameters);
        }

        /// <summary>
        /// Execute a SQL Query and return each row as a Dictionary. Lazy loads each row when iterated on. 
        /// </summary>
        /// <param name="sql">A SQL Query, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters"></param>
        /// <returns>A Dictionary representing a single row at a time</returns>
        public IEnumerable<IDictionary<string, object>> RowsIterator(string sql, params object[] parameters) {
            return Db.RowsIterator(sql, parameters);
        }

        /// <summary>
        /// Execute a SQL Query returning a single value, such as a Concat() or Sum(). Uses caching.
        /// </summary>
        /// <typeparam name="T">The type of the value to return</typeparam>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="sql">A SQL Query ideally returning a single column/row, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>A single value of the specified type</returns>
        public T Cell<T>(bool useCache, string sql, params object[] parameters) {
            return Db.Cell<T>(useCache, sql, parameters);
        }

        /// <summary>
        /// Execute a SQL Query returning a single value, such as a Concat() or Sum(). Uses caching
        /// </summary>
        /// <typeparam name="T">The type of the value to return</typeparam>
        /// <param name="sql">A SQL Query ideally returning a single column/row, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>A single value of the specified type</returns>
        public T Cell<T>(string sql, params object[] parameters) {
            return Cell<T>(true, sql, parameters);
        }

        /// <summary>
        /// Execute a SQL Query returning a single value as an object, such as a Concat() or Sum(). Uses caching
        /// </summary>
        /// <param name="sql">A SQL Query ideally returning a single column/row, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>A single value as an object</returns>
        public object Cell(string sql, params object[] parameters) {
            return Cell<object>(sql, parameters);
        }

        /// <summary>
        /// Execute a SQL Query returning a single column of values as the specified type
        /// </summary>
        /// <typeparam name="T">The type to return the column as</typeparam>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="sql">A SQL Query ideally returning a single column, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of values representing a column of the specified type</returns>
        public T[] Col<T>(bool useCache, string sql, params object[] parameters) {
            return Db.Col<T>(useCache, sql, parameters);
        }

        /// <summary>
        /// Execute a SQL Query returning a single column of values as the specified type. Uses caching
        /// </summary>
        /// <typeparam name="T">The type to return the column as</typeparam>
        /// <param name="sql">A SQL Query ideally returning a single column, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of values representing a column of the specified type</returns>
        public T[] Col<T>(string sql, params object[] parameters) {
            return Col<T>(true, sql, parameters);
        }

        /// <summary>
        /// Execute a SQL Query returning a single column of values as objects. Uses caching
        /// </summary>
        /// <param name="sql">A SQL Query ideally returning a single column, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of values representing a column of the specified type</returns>
        public object[] Col(string sql, params object[] parameters) {
            return Col<object>(true, sql, parameters);
        }


        /// <summary>
        /// Execute a SQL Query returning a single row of values as a Dictionary
        /// </summary>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="sql">A SQL Query ideally returning a single row, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An dictionary representing a row of data</returns>
        public IDictionary<string, object> Row(bool useCache, string sql, params object[] parameters) {
            return Db.Row(useCache, sql, parameters);
        }

        /// <summary>
        /// Execute a SQL Query returning a single row of values as a Dictionary. Uses cachine
        /// </summary>
        /// <param name="sql">A SQL Query ideally returning a single row, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An dictionary representing a row of data</returns>
        public IDictionary<string, object> Row(string sql, params object[] parameters) {
            return Row(true, sql, parameters);
        }

        /// <summary>
        /// Execute a SQL Query returning multiple rows of values as Dictionarys
        /// </summary>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="sql">A SQL Query return multiple rows, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of dictionarys, each representing a row of data</returns>
        public IDictionary<string, object>[] Rows(bool useCache, string sql, params object[] parameters) {
            return Db.Rows(useCache, sql, parameters);
        }

        /// <summary>
        /// Execute a SQL Query returning multiple rows of values as Dictionarys. Uses caching
        /// </summary>
        /// <param name="sql">A SQL Query return multiple rows, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of dictionarys, each representing a row of data</returns>
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

        /// <summary>
        /// Gets or Sets whether string values being stored to the database have any trailing whitespace trimmed
        /// </summary>
        public bool TrimStrings {
            get { return Storage.TrimStrings; }
            set { Storage.TrimStrings = value; }
        }

        /// <summary>
        /// Gets or Sets whether string values being stored to the database are converted to nulls if empty
        /// </summary>
        public bool ConvertEmptyStringToNull {
            get { return Storage.ConvertEmptyStringToNull; }
            set { Storage.ConvertEmptyStringToNull = value; }
        }

        /// <summary>
        /// Gets or Sets whether integers are detected and converted from Double/Single/String variables
        /// when storing to the database. This allows fluid mode to guide your use of the schema
        /// </summary>
        public bool RecognizeIntegers {
            get { return Storage.RecognizeIntegers; }
            set { Storage.RecognizeIntegers = value; }
        }


        // Custom keys

        /// <summary>
        /// Registers a new Primary Key on the given Kind
        /// </summary>
        /// <param name="kind">The table name</param>
        /// <param name="name">The name of the primary key field</param>
        /// <param name="autoIncrement">Whether the key should auto-increment</param>
        public void Key(string kind, string name, bool autoIncrement) {
            KeyUtil.RegisterKey(kind, new[] { name }, autoIncrement);
        }

        /// <summary>
        /// Registers a new multi-column Key on the given Kind
        /// </summary>
        /// <param name="kind">The table name</param>
        /// <param name="name">The names of the primary key fields</param>
        public void Key(string kind, params string[] names) {
            KeyUtil.RegisterKey(kind, names, null);
        }

        /// <summary>
        /// Registers a new Primary Key on the given Bean subtype's Kind
        /// </summary>
        /// <param name="name">The name of the primary key field</param>
        /// <param name="autoIncrement">Whether the key should auto-increment</param>
        public void Key<T>(string name, bool autoIncrement) where T : Bean, new() {
            Key(Bean.GetKind<T>(), name, autoIncrement);
        }

        /// <summary>
        /// Registers a new Primary Key on the given Bean subtype's Kind
        /// </summary>
        /// <param name="name">The names of the primary key fields</param>
        public void Key<T>(params string[] names) where T : Bean, new() {
            Key(Bean.GetKind<T>(), names);
        }

        /// <summary>
        /// Sets whether default Primary Keys auto-increment
        /// </summary>
        /// <param name="autoIncrement">Whether a new Key should auto-increment</param>
        public void DefaultKey(bool autoIncrement) {
            KeyUtil.DefaultAutoIncrement = autoIncrement;
        }

        /// <summary>
        /// Sets the default field name for a Primary Key, and whether it should auto-increment
        /// </summary>
        /// <param name="name">The default field name for a Primary Key</param>
        /// <param name="autoIncrement">Whether a default Primary Key should auto-increment</param>
        public void DefaultKey(string name, bool autoIncrement = true) {
            KeyUtil.DefaultName = name;
            KeyUtil.DefaultAutoIncrement = autoIncrement;
        }

    }

}

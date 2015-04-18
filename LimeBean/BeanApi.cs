using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace LimeBean {

    public class BeanApi : IDisposable, IBeanCrud, IBeanFinder, IDatabaseAccess, IValueRelaxations {
        DbProviderFactory _provider;
        string _connectionString;

        bool _sharedConnection;
        IDbConnection _connection;
        IDatabaseDetails _details;
        IDatabaseAccess _db;
        KeyUtil _keyUtil;
        DatabaseStorage _storage;
        IBeanCrud _crud;
        IBeanFinder _finder;

        public BeanApi(string connectionString, string providerName)
            : this(connectionString, DbProviderFactories.GetFactory(providerName)) {
        }

        public BeanApi(string connectionString, DbProviderFactory provider) {
            _connectionString = connectionString;
            _provider = provider;
        }

        public BeanApi(IDbConnection connection) {
            _sharedConnection = true;
            _connection = connection;
        }

        // Properties

        public IDbConnection Connection {
            get {
                if(_connection == null) {
                    var c = _provider.CreateConnection();
                    c.ConnectionString = _connectionString;
                    c.Open();

                    _provider = null;
                    _connectionString = null;

                    _connection = c;
                }
                return _connection;
            }
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
                if(_crud == null)
                    _crud = new BeanCrud(Storage, Db, KeyUtil);
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
                    return new SQLiteDetails();
    
                case "MySql.Data.MySqlClient.MySqlConnection":
                    return new MariaDbDetails();

                case "System.Data.SqlClient.SqlConnection":
                    return new MsSqlDetails();
            }

            throw new NotSupportedException();
        }

        // Methods

#if !DEBUG
        [Obsolete("Use Fluid Mode in DEBUG mode only!")]
#endif
        public void EnterFluidMode() {
            Storage.EnterFluidMode();
        }

        public void Dispose() {
            if(!_sharedConnection && _connection != null)
                _connection.Dispose();
        }

        // IBeanCrud

        public void AddObserver(BeanObserver observer) {
            Crud.AddObserver(observer);
        }

        public void RemoveObserver(BeanObserver observer) {
            Crud.RemoveObserver(observer);
        }

        public Bean Dispense(string kind) {
            return Crud.Dispense(kind);
        }

        public T Dispense<T>() where T : Bean, new() {
            return Crud.Dispense<T>();
        }

        public Bean RowToBean(string kind, IDictionary<string, IConvertible> row) {
            return Crud.RowToBean(kind, row);
        }

        public T RowToBean<T>(IDictionary<string, IConvertible> row) where T : Bean, new() {
            return Crud.RowToBean<T>(row);
        }

        public Bean Load(string kind, IConvertible key) {
            return Crud.Load(kind, key);
        }

        public T Load<T>(IConvertible key) where T : Bean, new() {
            return Crud.Load<T>(key);
        }

        public IConvertible Store(Bean bean) {
            return Crud.Store(bean);
        }

        public void Trash(Bean bean) {
            Crud.Trash(bean);
        }

        // IBeanFinder

        public Bean[] Find(bool useCache, string kind, string expr = null, params object[] parameters) {
            return Finder.Find(useCache, kind, expr, parameters);
        }

        public T[] Find<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new() {
            return Finder.Find<T>(useCache, expr, parameters);
        }

        public Bean FindOne(bool useCache, string kind, string expr = null, params object[] parameters) {
            return Finder.FindOne(useCache, kind, expr, parameters);
        }

        public T FindOne<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new() {
            return Finder.FindOne<T>(useCache, expr, parameters);
        }

        public IEnumerable<Bean> FindIterator(string kind, string expr = null, params object[] parameters) {
            return Finder.FindIterator(kind, expr, parameters);
        }

        public IEnumerable<T> FindIterator<T>(string expr = null, params object[] parameters) where T : Bean, new() {
            return Finder.FindIterator<T>(expr, parameters);
        }

        public long Count(bool useCache, string kind, string expr = null, params object[] parameters) {
            return Finder.Count(useCache, kind, expr, parameters);            
        }

        public long Count<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new() {
            return Finder.Count<T>(useCache, expr, parameters);            
        }

        // IDatabaseAccess

        public event Action<IDbCommand> QueryExecuting {
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

        public IEnumerable<T> ColIterator<T>(string sql, params object[] parameters) where T : IConvertible {
            return Db.ColIterator<T>(sql, parameters);
        }

        public IEnumerable<IDictionary<string, IConvertible>> RowsIterator(string sql, params object[] parameters) {
            return Db.RowsIterator(sql, parameters);
        }

        public T Cell<T>(bool useCache, string sql, params object[] parameters) where T : IConvertible {
            return Db.Cell<T>(useCache, sql, parameters);
        }

        public T[] Col<T>(bool useCache, string sql, params object[] parameters) where T : IConvertible {
            return Db.Col<T>(useCache, sql, parameters);
        }

        public IDictionary<string, IConvertible> Row(bool useCache, string sql, params object[] parameters) {
            return Db.Row(useCache, sql, parameters);
        }

        public IDictionary<string, IConvertible>[] Rows(bool useCache, string sql, params object[] parameters) {
            return Db.Rows(useCache, sql, parameters);
        }

        // ITransactionSupport

        public bool ImplicitTransactions {
            get { return Db.ImplicitTransactions; }
            set { Db.ImplicitTransactions = value; }
        }

        public bool InTransaction {
            get { return Db.InTransaction; }
        }

        public void Transaction(Func<bool> action) {
            Db.Transaction(action);
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

        // Shortcuts

        public Bean Load(string kind, params IConvertible[] compoundKey){
            return Load(kind, KeyUtil.PackCompoundKey(kind, compoundKey));
        }

        public T Load<T>(params IConvertible[] compoundKey) where T : Bean, new() {
            return Load<T>(KeyUtil.PackCompoundKey(Bean.GetKind<T>(), compoundKey));
        }

        public Bean[] Find(string kind, string expr = null, params object[] parameters) {
            return Find(true, kind, expr, parameters);
        }

        public T[] Find<T>(string expr = null, params object[] parameters) where T : Bean, new() {
            return Find<T>(true, expr, parameters);
        }

        public Bean FindOne(string kind, string expr = null, params object[] parameters) {
            return FindOne(true, kind, expr, parameters);
        }

        public T FindOne<T>(string expr = null, params object[] parameters) where T : Bean, new() {
            return FindOne<T>(true, expr, parameters);
        }

        public long Count(string kind, string expr = null, params object[] parameters) {
            return Count(true, kind, expr, parameters);
        }

        public long Count<T>(string expr = null, params object[] parameters) where T : Bean, new() {
            return Count<T>(true, expr, parameters);
        }

        public T Cell<T>(string sql, params object[] parameters) where T : IConvertible {
            return Cell<T>(true, sql, parameters);
        }

        public T[] Col<T>(string sql, params object[] parameters) where T : IConvertible {
            return Col<T>(true, sql, parameters);
        }

        public IDictionary<string, IConvertible> Row(string sql, params object[] parameters) {
            return Row(true, sql, parameters);
        }

        public IDictionary<string, IConvertible>[] Rows(string sql, params object[] parameters) {
            return Rows(true, sql, parameters);
        }

        public void Transaction(Action action) {
            Transaction(delegate() {
                action();
                return true;
            });
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

        public void Key(bool autoIncrementByDefault) {
            KeyUtil.AutoIncrementByDefault = autoIncrementByDefault;
        }

    }

}

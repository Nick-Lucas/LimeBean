using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LimeBean {

    class DatabaseAccess : IDatabaseAccess {
        static readonly Regex READONLY_SQL_RE = new Regex(@"^\s*(select|pragma|show)\s", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        IDbConnection _connection;
        int _txLevel;
        Cache<DbCommandDescriptor, object> _cache = new Cache<DbCommandDescriptor, object>();

        public DatabaseAccess(IDbConnection connection) {
            _connection = connection;
        }

        public bool InTransaction { get { return _txLevel > 0; } }
        public event Action<IDbCommand> QueryExecuting;

        public int CacheCapacity {
            get { return _cache.Capacity; }
            set { _cache.Capacity = value; }
        }

        public int Exec(string sql, object[] parameters) {
            using(var cmd = new DbCommandDescriptor(sql, parameters).ToCommand(_connection)) {
                QueryWillExecute(cmd);
                return cmd.ExecuteNonQuery();
            }
        }

        // Iterators

        public IEnumerable<T> ColIterator<T>(string sql, object[] parameters) where T : IConvertible {
            return EnumerateRecords(new DbCommandDescriptor(sql, parameters), GetFirstCellValue<T>);
        }

        public IEnumerable<IDictionary<string, IConvertible>> RowsIterator(string sql, object[] parameters) {
            return EnumerateRecords(new DbCommandDescriptor(sql, parameters), RecordToDict).ToArray();
        }

        // Cell

        public T Cell<T>(bool useCache, string sql, object[] parameters) where T : IConvertible {
            return CacheableRead(true, true, useCache, sql, parameters, Cell<T>);
        }

        T Cell<T>(DbCommandDescriptor descriptor) where T : IConvertible {
            return EnumerateRecords(descriptor, GetFirstCellValue<T>).FirstOrDefault();
        }


        // Col

        public T[] Col<T>(bool useCache, string sql, object[] parameters) where T : IConvertible {
            return CacheableRead(true, false, useCache, sql, parameters, Col<T>);
        }

        T[] Col<T>(DbCommandDescriptor descriptor) where T : IConvertible {
            return EnumerateRecords(descriptor, GetFirstCellValue<T>).ToArray();
        }

        // Row

        public IDictionary<string, IConvertible> Row(bool useCache, string sql, object[] parameters) {
            return CacheableRead(false, true, useCache, sql, parameters, Row);
        }

        IDictionary<string, IConvertible> Row(DbCommandDescriptor descriptor) {
            return EnumerateRecords(descriptor, RecordToDict).FirstOrDefault();
        }

        // Rows

        public IDictionary<string, IConvertible>[] Rows(bool useCache, string sql, object[] parameters) {
            return CacheableRead(false, false, useCache, sql, parameters, Rows);
        }

        IDictionary<string, IConvertible>[] Rows(DbCommandDescriptor descriptor) {
            return EnumerateRecords(descriptor, RecordToDict).ToArray();
        }

        // Transactions

        public void Transaction(Func<bool> action) {
            using(var tx = _connection.BeginTransaction()) {
                var shouldRollback = false;

                _txLevel++;
                try {
                    shouldRollback = !action();
                } catch {
                    shouldRollback = true;
                    throw;
                } finally {
                    if(shouldRollback) {
                        _cache.Clear();
                        tx.Rollback();
                    } else {
                        tx.Commit();
                    }
                    _txLevel--;
                }
            }
        }


        // Internals

        IEnumerable<T> EnumerateRecords<T>(DbCommandDescriptor descriptor, Func<IDataRecord, T> converter) {
            using(var cmd = descriptor.ToCommand(_connection)) {
                QueryWillExecute(cmd);
                using(var reader = cmd.ExecuteReader()) {
                    while(reader.Read())
                        yield return converter(reader);
                }
            }
        }

        static IDictionary<string, IConvertible> RecordToDict(IDataRecord record) {
            var count = record.FieldCount;
            var result = new Dictionary<string, IConvertible>(count);

            for(var i = 0; i < count; i++)
                result[record.GetName(i)] = GetCellValue<IConvertible>(record, i);

            return result;
        }

        static T GetFirstCellValue<T>(IDataRecord record) where T : IConvertible {
            return GetCellValue<T>(record, 0);
        }

        static T GetCellValue<T>(IDataRecord record, int index) where T : IConvertible {
            var value = record.GetValue(index) as IConvertible;
            if(value == null || value is DBNull)
                return default(T);

            if(value is T)
                return (T)value;

            return (T)value.ToType(typeof(T), CultureInfo.InvariantCulture);
        }

        void QueryWillExecute(IDbCommand cmd) {
            if(!READONLY_SQL_RE.IsMatch(cmd.CommandText))
                _cache.Clear();

            if(QueryExecuting != null)
                QueryExecuting(cmd);
            
            // Console.WriteLine(cmd.CommandText);
        }

        T CacheableRead<T>(bool singleCell, bool singleRow, bool useCache, string sql, object[] parameters, Func<DbCommandDescriptor, T> factory) {
            var descriptor = new DbCommandDescriptor((singleCell ? 1 : 0) + (singleRow ? 2 : 0), sql, parameters);

            if(useCache && _cache.Contains(descriptor))
                return (T)_cache.Get(descriptor);

            var fresh = factory(descriptor);

            if(useCache)
                _cache.Put(descriptor, fresh);
            else
                _cache.Remove(descriptor);

            return fresh;
        }

    }

}

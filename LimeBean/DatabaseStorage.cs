using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LimeBean {

    using Schema = Dictionary<string, IDictionary<string, int>>;

    class DatabaseStorage : IStorage, IValueRelaxations {
        Schema _schema;
        bool _isFluidMode;
        IDatabaseDetails _details;
        IDatabaseAccess _db;
        IKeyAccess _keyAccess;

        public DatabaseStorage(IDatabaseDetails details, IDatabaseAccess db, IKeyAccess keys) {
            _details = details;
            _db = db;
            _keyAccess = keys;

            TrimStrings = true;
            ConvertEmptyStringToNull = true;
            RecognizeIntegers = true;

            _details.ExecInitCommands(_db);
        }

        public bool TrimStrings { get; set; }
        public bool ConvertEmptyStringToNull { get; set; }
        public bool RecognizeIntegers { get; set; }

        public void EnterFluidMode() {
            _isFluidMode = true;
        }

        internal Schema GetSchema() {
            if(_schema == null)
                _schema = LoadSchema();
            return _schema;
        }

        internal void InvalidateSchema() {
            _schema = null;
        }

        Schema LoadSchema() {
            var result = new Schema();
            foreach(var tableName in _details.GetTableNames(_db)) {
                var autoIncrementName = _keyAccess.GetAutoIncrementName(tableName);
                var columns = new Dictionary<string, int>();
                foreach(var col in _details.GetColumns(_db, tableName)) {
                    var name = _details.GetColumnName(col);
                    if(name == autoIncrementName)
                        continue;

                    columns[name] = !_details.IsNullableColumn(col) || _details.GetColumnDefaultValue(col) != null
                        ? CommonDatabaseDetails.RANK_CUSTOM
                        : _details.GetRankFromSqlType(_details.GetColumnType(col));
                }
                result[tableName] = columns;
            }
            return result;
        }

        bool IsKnownKind(string kind) {
            return GetSchema().ContainsKey(kind);
        }

        IDictionary<string, int> GetColumnsFromData(IDictionary<string, object> data) {
            var result = new Dictionary<string, int>();
            foreach(var entry in data)
                result[entry.Key] = _details.GetRankFromValue(entry.Value);
            return result;
        }

        object ConvertValue(object value) {
            if(value == null)
                return null;

            if(value is UInt64) {
                var number = (ulong)value;
                value = number <= Int64.MaxValue ? (long)number : (decimal)number;
            }                

            if(value is Boolean) {
                if(_details.SupportsBoolean)
                    return value;
                value = (bool)value ? 1 : 0;
            } else if(value is Decimal) {
                if(_details.SupportsDecimal)
                    return value;
                value = Convert.ToString(value, CultureInfo.InvariantCulture);
            }

            if(value is Int32 || value is Int64 || value is Byte || value is SByte || value is Int16 || value is UInt16 || value is UInt32 || value is Enum || value is Enum || value is Enum || value is Enum)
                return _details.ConvertLongValue(Convert.ToInt64(value));

            if(value is Double || value is Single) {
                var number = Convert.ToDouble(value);
                if(RecognizeIntegers && number.IsSafeInteger())
                    return _details.ConvertLongValue(Convert.ToInt64(number));

                return number;        
            }

            if(value is String) {
                var text = (string)value;

                if(TrimStrings)
                    text = text.Trim();

                if(ConvertEmptyStringToNull && text.Length < 1)
                    return null;

                if(RecognizeIntegers && text.Length > 0 && text.Length < 21 && !Char.IsLetter(text, 0)) {
                    long number;
                    if(Int64.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out number)) {
                        if(number.ToString(CultureInfo.InvariantCulture) == text)
                            return _details.ConvertLongValue(number);
                    }
                }

                return text;
            }

            return value;
        }

        public object Store(string kind, IDictionary<string, object> data) {
            return Store(kind, data, null);
        }

        public object Store(string kind, IDictionary<string, object> data, ICollection<string> dirtyNames) {            
            var key = _keyAccess.GetKey(kind, data);
            var autoIncrement = _keyAccess.IsAutoIncrement(kind);

            if(!autoIncrement && key == null)
                throw new InvalidOperationException("Missing key value");

            var isNew = !autoIncrement ? !IsKnownKey(kind, key) : key == null;

            if(!isNew && key != null) {
                data = new Dictionary<string, object>(data);
                _keyAccess.SetKey(kind, data, null);
            }

            data = data
                .Where(e => dirtyNames == null || dirtyNames.Contains(e.Key))
                .ToDictionary(e => e.Key, e => ConvertValue(e.Value));

            if(_isFluidMode) {
                data = DropNulls(kind, data);
                CheckSchema(kind, data);
            }

            if(isNew) {
                var insertResult = _details.ExecInsert(_db, kind, _keyAccess.GetAutoIncrementName(kind), data);
                if(autoIncrement)
                    return insertResult;
            } else if(data.Count > 0) {
                ExecUpdate(kind, key, data);
            }

            return key;
        }

        public IDictionary<string, object> Load(string kind, object key) {
            if(_isFluidMode && !IsKnownKind(kind))
                return null;

            var args = CreateSimpleByKeyArguments("select *", kind, key);
            return _db.Row(true, args.Item1, args.Item2);
        }

        public void Trash(string kind, object key) {
            if(_isFluidMode && !IsKnownKind(kind))
                return;

            var args = CreateSimpleByKeyArguments("delete", kind, key);
            _db.Exec(args.Item1, args.Item2);
        }

        bool IsKnownKey(string kind, object key) {
            if(_isFluidMode && !IsKnownKind(kind))
                return false;

            var args = CreateSimpleByKeyArguments("select count(*)", kind, key);
            return _db.Cell<int>(false, args.Item1, args.Item2) > 0;
        }

        void ExecUpdate(string kind, object key, IDictionary<string, object> data) {
            var propValues = new List<object>();
            var sql = new StringBuilder();

            sql
                .Append("update ")
                .Append(QuoteName(kind))
                .Append(" set ");

            var index = 0;
            foreach(var entry in data) {
                if(index > 0)
                    sql.Append(", ");

                propValues.Add(entry.Value);

                sql
                    .Append(QuoteName(entry.Key))
                    .Append(" = ")
                    .Append("{").Append(index).Append("}");

                index++;
            }

            AppendKeyCriteria(kind, key, sql, propValues);

            if(_db.Exec(sql.ToString(), propValues.ToArray()) < 1)
                throw new Exception("Row not found");
        }

        Tuple<string, object[]> CreateSimpleByKeyArguments(string prefix, string kind, object key) {
            var parameters = new List<object>();
            var sql = new StringBuilder(prefix)
                .Append(" from ")
                .Append(QuoteName(kind));
            
            AppendKeyCriteria(kind, key, sql, parameters);
            
            return Tuple.Create(sql.ToString(), parameters.ToArray());
        }

        IDictionary<string, object> DropNulls(string kind, IDictionary<string, object> data) {
            var schema = GetSchema();
            var result = new Dictionary<string, object>();

            foreach(var entry in data) {
                if(entry.Value != null || schema.ContainsKey(kind) && schema[kind].ContainsKey(entry.Key))                    
                    result[entry.Key] = entry.Value;
            }

            return result;
        }

        void CheckSchema(string kind, IDictionary<string, object> data) {
            var newColumns = GetColumnsFromData(data);
            var autoIncrementName = _keyAccess.GetAutoIncrementName(kind);

            if(autoIncrementName != null)
                newColumns.Remove(autoIncrementName);

            if(!IsKnownKind(kind)) {
                foreach(var name in newColumns.Keys)
                    ValidateNewColumnRank(name, newColumns[name], data[name]);                
                _db.Exec(CommonDatabaseDetails.FormatCreateTableCommand(_details, kind, autoIncrementName, newColumns));
                InvalidateSchema();
            } else {
                var oldColumns = GetSchema()[kind];
                var changedColumns = new Dictionary<string, int>();
                var addedColumns = new Dictionary<string, int>();

                foreach(var name in newColumns.Keys) {
                    var newRank = newColumns[name];

                    if(!oldColumns.ContainsKey(name)) {
                        ValidateNewColumnRank(name, newRank, data[name]);
                        addedColumns[name] = newRank;
                    } else {
                        var oldRank = oldColumns[name];
                        if(newRank > oldRank && Math.Max(oldRank, newRank) < CommonDatabaseDetails.RANK_STATIC_BASE)
                            changedColumns[name] = newRank;                        
                    }
                }

                if(changedColumns.Count > 0 || addedColumns.Count > 0) {
                    _details.UpdateSchema(_db, kind, _keyAccess.GetAutoIncrementName(kind), oldColumns, changedColumns, addedColumns);
                    InvalidateSchema();
                }
            }
        }

        void ValidateNewColumnRank(string columnName, int rank, object value) {
            if(rank < CommonDatabaseDetails.RANK_CUSTOM)
                return;

            var text = String.Format("Cannot automatically add column for property '{0}' of type '{1}'", columnName, value.GetType());
            throw new InvalidOperationException(text);
        }

        void AppendKeyCriteria(string kind, object key, StringBuilder sql, ICollection<object> parameters) {
            sql.Append(" where ");

            var compound = key as CompoundKey;
            var names = _keyAccess.GetKeyNames(kind);

            if(names.Count > 1 ^ compound != null)
                throw new InvalidOperationException();

            var first = true;
            foreach(var name in names) {
                if(!first)
                    sql.Append(" and ");                
            
                sql.Append(QuoteName(name)).Append(" = {").Append(parameters.Count).Append("}");

                if(compound != null)
                    parameters.Add(compound[name]);
                else
                    parameters.Add(key);

                first = false;
            }
            
        }

        string QuoteName(string name) {
            return _details.QuoteName(name);
        }

    }

}

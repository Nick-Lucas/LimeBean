using System;
using System.Collections.Generic;
using System.Data;
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

        IDictionary<string, int> GetColumnsFromData(IDictionary<string, IConvertible> data) {
            var result = new Dictionary<string, int>();
            foreach(var entry in data)
                result[entry.Key] = _details.GetRankFromValue(entry.Value);
            return result;
        }

        IConvertible ConvertValue(IConvertible value) {
            if(value == null)
                return null;

            switch(value.GetTypeCode()) {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                    return _details.ConvertLongValue(value.ToInt64(CultureInfo.InvariantCulture));

                case TypeCode.Single:
                case TypeCode.Double:
                    var number = value.ToDouble(CultureInfo.InvariantCulture);

                    if(RecognizeIntegers) {
                        const double
                            minSafeInteger = -0x1fffffffffffff,
                            maxSafeInteger = 0x1fffffffffffff;

                        if(Math.Truncate(number) == number && number >= minSafeInteger && number <= maxSafeInteger)
                            return _details.ConvertLongValue(Convert.ToInt64(number));
                    }

                    return number;
            }

            var text = value.ToString(CultureInfo.InvariantCulture);

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

        public IConvertible Store(string kind, IDictionary<string, IConvertible> data) {
            return Store(kind, data, null);
        }

        public IConvertible Store(string kind, IDictionary<string, IConvertible> data, ICollection<string> dirtyNames) {            
            var key = _keyAccess.GetKey(kind, data);
            var autoIncrement = _keyAccess.IsAutoIncrement(kind);

            if(!autoIncrement && key == null)
                throw new InvalidOperationException("Missing key value");

            var isNew = !autoIncrement ? !IsKnownKey(kind, key) : key == null;

            if(!isNew && key != null) {
                data = new Dictionary<string, IConvertible>(data);
                _keyAccess.SetKey(kind, data, null);
            }

            data = data
                .Where(e => dirtyNames == null || dirtyNames.Contains(e.Key))
                .ToDictionary(e => e.Key, e => ConvertValue(e.Value));

            if(_isFluidMode) {
                data = DropNulls(kind, data);
                CheckSchema(kind, data);
            }

            if(isNew)
                ExecInsert(kind, data);
            else if(data.Count > 0)
                ExecUpdate(kind, key, data);

            if(isNew && autoIncrement) {
                // NOTE on concurrency
                // "last insert id" is connection-aware, but not thread-safe
                // http://stackoverflow.com/q/21185666
                // http://stackoverflow.com/q/9313205
                // http://www.sqlite.org/lang_corefunc.html

                return _details.GetLastInsertID(_db);
            }

            return key;
        }

        public IDictionary<string, IConvertible> Load(string kind, IConvertible key) {
            if(_isFluidMode && !IsKnownKind(kind))
                return null;

            var args = CreateSimpleByKeyArguments("select *", kind, key);
            return _db.Row(true, args.Item1, args.Item2);
        }

        public void Trash(string kind, IConvertible key) {
            if(_isFluidMode && !IsKnownKind(kind))
                return;

            var args = CreateSimpleByKeyArguments("delete", kind, key);
            _db.Exec(args.Item1, args.Item2);
        }

        bool IsKnownKey(string kind, IConvertible key) {
            if(_isFluidMode && !IsKnownKind(kind))
                return false;

            var args = CreateSimpleByKeyArguments("select count(*)", kind, key);
            return _db.Cell<int>(false, args.Item1, args.Item2) > 0;
        }

        void ExecInsert(string kind, IDictionary<string, IConvertible> data) {
            var propNames = new string[data.Count];
            var propValues = new object[data.Count];
            var placeholders = new string[data.Count];

            var index = 0;
            foreach(var entry in data) {
                propNames[index] = QuoteName(entry.Key);
                propValues[index] = entry.Value;
                placeholders[index] = "{" + index + "}";
                index++;
            }

            var sql = "insert into " + QuoteName(kind) + " ";
            if(data.Count > 0)
                sql += "(" + String.Join(", ", propNames) + ") values (" + String.Join(", ", placeholders) + ")";
            else
                sql += _details.GetInsertDefaultsPostfix();

            _db.Exec(sql, propValues);
        }

        void ExecUpdate(string kind, IConvertible key, IDictionary<string, IConvertible> data) {
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

        Tuple<string, object[]> CreateSimpleByKeyArguments(string prefix, string kind, IConvertible key) {
            var parameters = new List<object>();
            var sql = new StringBuilder(prefix)
                .Append(" from ")
                .Append(QuoteName(kind));
            
            AppendKeyCriteria(kind, key, sql, parameters);
            
            return Tuple.Create(sql.ToString(), parameters.ToArray());
        }

        IDictionary<string, IConvertible> DropNulls(string kind, IDictionary<string, IConvertible> data) {
            var schema = GetSchema();
            var result = new Dictionary<string, IConvertible>();

            foreach(var entry in data) {
                if(entry.Value != null || schema.ContainsKey(kind) && schema[kind].ContainsKey(entry.Key))                    
                    result[entry.Key] = entry.Value;
            }

            return result;
        }

        void CheckSchema(string kind, IDictionary<string, IConvertible> data) {
            var newColumns = GetColumnsFromData(data);
            var autoIncrementName = _keyAccess.GetAutoIncrementName(kind);

            if(autoIncrementName != null)
                newColumns.Remove(autoIncrementName);

            if(!IsKnownKind(kind)) {
                _db.Exec(CommonDatabaseDetails.FormatCreateTableCommand(_details, kind, autoIncrementName, newColumns));
                InvalidateSchema();
            } else {
                var oldColumns = GetSchema()[kind];
                var changedColumns = new Dictionary<string, int>();
                var addedColumns = new Dictionary<string, int>();

                foreach(var name in newColumns.Keys) {
                    if(!oldColumns.ContainsKey(name))
                        addedColumns[name] = newColumns[name];
                    else if(newColumns[name] > oldColumns[name])
                        changedColumns[name] = newColumns[name];
                }

                if(changedColumns.Count > 0 || addedColumns.Count > 0) {
                    _details.UpdateSchema(_db, kind, _keyAccess.GetAutoIncrementName(kind), oldColumns, changedColumns, addedColumns);
                    InvalidateSchema();
                }
            }
        }

        void AppendKeyCriteria(string kind, IConvertible key, StringBuilder sql, ICollection<object> parameters) {
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

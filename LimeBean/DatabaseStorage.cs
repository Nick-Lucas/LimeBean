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

        public DatabaseStorage(IDatabaseDetails details, IDatabaseAccess db) {
            _details = details;
            _db = db;

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
                var columns = new Dictionary<string, int>();
                foreach(var col in _details.GetColumns(_db, tableName)) {
                    var name = _details.GetColumnName(col);
                    if(name == Bean.ID_PROP_NAME)
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
            var result = new Dictionary<string, int>(data.Count);
            foreach(var entry in data)
                result[entry.Key] = _details.GetRankFromValue(ConvertValue(entry.Value));
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

        public long Store(string kind, IDictionary<string, IConvertible> data) {
            var id = new Nullable<long>();

            if(data.ContainsKey(Bean.ID_PROP_NAME)) {
                id = data[Bean.ID_PROP_NAME].ToInt64(CultureInfo.InvariantCulture);
                data = new Dictionary<string, IConvertible>(data);
                data.Remove(Bean.ID_PROP_NAME);
            }

            if(_isFluidMode)
                CheckSchema(kind, data);

            if(id == null) {
                ExecInsert(kind, data);

                // NOTE on concurrency
                // "last insert id" is connection-aware, but not thread-safe
                // http://stackoverflow.com/q/21185666
                // http://stackoverflow.com/q/9313205
                // http://www.sqlite.org/lang_corefunc.html

                return _details.GetLastInsertID(_db);
            }

            if(data.Count > 0)
                ExecUpdate(kind, id.Value, data);

            return id.Value;
        }

        public IDictionary<string, IConvertible> Load(string kind, long id) {
            if(_isFluidMode && !IsKnownKind(kind))
                return null;

            return _db.Row(true, "select * from " + QuoteName(kind) + WhereId(id));
        }

        public void Trash(string kind, long id) {
            if(_isFluidMode && !IsKnownKind(kind))
                return;

            _db.Exec("delete from " + QuoteName(kind) + WhereId(id));
        }

        void ExecInsert(string kind, IDictionary<string, IConvertible> data) {
            if(data.Count < 1)
                data = new Dictionary<string, IConvertible> { { Bean.ID_PROP_NAME, null } };

            var propNames = new string[data.Count];
            var propValues = new object[data.Count];
            var placeholders = new string[data.Count];

            var index = 0;
            foreach(var entry in data) {
                propNames[index] = QuoteName(entry.Key);
                propValues[index] = ConvertValue(entry.Value);
                placeholders[index] = "{" + index + "}";
                index++;
            }

            var sql = "insert into " + QuoteName(kind) + " ("
                + String.Join(", ", propNames) + ") values ("
                + String.Join(", ", placeholders) + ")";

            _db.Exec(sql, propValues);
        }

        void ExecUpdate(string kind, long id, IDictionary<string, IConvertible> data) {
            var propValues = new object[data.Count];
            var sql = new StringBuilder();

            sql
                .Append("update ")
                .Append(QuoteName(kind))
                .Append(" set ");

            var index = 0;
            foreach(var entry in data) {
                if(index > 0)
                    sql.Append(", ");

                propValues[index] = ConvertValue(entry.Value);

                sql
                    .Append(QuoteName(entry.Key))
                    .Append(" = ")
                    .Append("{").Append(index).Append("}");

                index++;
            }

            sql.Append(WhereId(id));

            if(_db.Exec(sql.ToString(), propValues) < 1)
                throw new Exception("Row not found");
        }


        void CheckSchema(string kind, IDictionary<string, IConvertible> data) {
            var newColumns = GetColumnsFromData(data);

            if(!IsKnownKind(kind)) {
                _db.Exec(CommonDatabaseDetails.FormatCreateTableCommand(_details, kind, newColumns));
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
                    _details.UpdateSchema(_db, kind, oldColumns, changedColumns, addedColumns);
                    InvalidateSchema();
                }
            }
        }

        string WhereId(long id) { 
            return " where " + QuoteName(Bean.ID_PROP_NAME) + " = " + id;
        }

        string QuoteName(string name) {
            return _details.QuoteName(name);
        }

    }

}

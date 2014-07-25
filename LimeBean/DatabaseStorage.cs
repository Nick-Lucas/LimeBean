using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LimeBean {

    using TableColumns = Dictionary<string, int>;
    using Schema = Dictionary<string, Dictionary<string, int>>;

    abstract class DatabaseStorage : IStorage, IDatabaseSpecifics {
        protected internal const int RANK_CUSTOM = Int32.MaxValue;

        Schema _schema;
        bool _isFluidMode;

        public DatabaseStorage(IDatabaseAccess db) {
            Db = db;
            TrimStrings = true;
            ConvertEmptyStringToNull = true;
            RecognizeIntegers = true;
        }

        public bool TrimStrings { get; set; }
        public bool ConvertEmptyStringToNull { get; set; }
        public bool RecognizeIntegers { get; set; }

        protected IDatabaseAccess Db { get; private set; }

        internal Schema GetSchema() {
            if(_schema == null)
                _schema = LoadSchema();
            return _schema;
        }

        internal void InvalidateSchema() {
            _schema = null;
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
                    return ConvertLongValue(value.ToInt64(CultureInfo.InvariantCulture));

                case TypeCode.Single:
                case TypeCode.Double:
                    var number = value.ToDouble(CultureInfo.InvariantCulture);

                    if(RecognizeIntegers) {
                        const double
                            minSafeInteger = -0x1fffffffffffff,
                            maxSafeInteger = 0x1fffffffffffff;

                        if(Math.Truncate(number) == number && number >= minSafeInteger && number <= maxSafeInteger)
                            return ConvertLongValue(Convert.ToInt64(number));
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
                        return ConvertLongValue(number);
                }
            }

            return text;
        }

        protected virtual IConvertible ConvertLongValue(long value) {
            return value;
        }

        protected abstract int GetRankFromValue(IConvertible value);
        protected abstract int GetRankFromSqlType(string sqlType);
        protected abstract string GetSqlTypeFromRank(int rank);        
        protected abstract Schema LoadSchema();
        protected abstract void UpdateSchema(string kind, TableColumns oldColumns, TableColumns changedColumns, TableColumns addedColumns);

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
                return GetLastInsertID();
            }

            if(data.Count > 0)
                ExecUpdate(kind, id.Value, data);

            return id.Value;
        }

        public IDictionary<string, IConvertible> Load(string kind, long id) {
            if(_isFluidMode && !IsKnownKind(kind))
                return null;

            return Db.Row(true, "select * from " + QuoteName(kind) + " where " + QuoteName(Bean.ID_PROP_NAME) + " = " + id);
        }

        public void Trash(string kind, long id) {
            if(_isFluidMode && !IsKnownKind(kind))
                return;

            Db.Exec("delete from " + QuoteName(kind) + " where " + QuoteName(Bean.ID_PROP_NAME) + " = " + id);
        }

        void ExecInsert(string kind, IDictionary<string, IConvertible> data) {
            if(data.Count < 1)
                data = new Dictionary<string, IConvertible> { { Bean.ID_PROP_NAME, null } }; 

            var propNames = new List<string>(data.Count);
            var paramNames = new List<string>(data.Count);
            var paramValues = new Dictionary<string, object>(data.Count);

            var index = 0;
            foreach(var propName in data.Keys) {
                var paramName = "p" + index++;
                propNames.Add(QuoteName(propName));
                paramNames.Add(FormatParam(paramName));
                paramValues[paramName] = ConvertValue(data[propName]);
            }

            var sql = "insert into " + QuoteName(kind) + " ("
                + String.Join(", ", propNames) + ") values ("
                + String.Join(", ", paramNames) + ")";

            Db.Exec(sql, paramValues);
        }

        void ExecUpdate(string kind, long id, IDictionary<string, IConvertible> data) {
            const string keyParamName = "pk";

            var paramValues = new Dictionary<string, object>(1 + data.Count) { 
                { keyParamName, id }
            };

            var pairs = new StringBuilder();

            var index = 0;
            foreach(var propName in data.Keys) {
                if(pairs.Length > 0)
                    pairs.Append(", ");

                var paramName = "p" + index++;                
                paramValues[paramName] = ConvertValue(data[propName]);

                pairs
                    .Append(QuoteName(propName))
                    .Append(" = ")
                    .Append(FormatParam(paramName));
            }

            var sql = "update " + QuoteName(kind)
                + " set " + pairs
                + " where " + QuoteName(Bean.ID_PROP_NAME) + " = " + FormatParam(keyParamName);

            if(Db.Exec(sql, paramValues) < 1)
                throw new Exception("Row not found");
        }

        TableColumns GetColumnsFromData(IDictionary<string, IConvertible> data) {
            var result = new TableColumns(data.Count);
            foreach(var entry in data)
                result[entry.Key] = GetRankFromValue(ConvertValue(entry.Value));
            return result;
        }

        bool IsKnownKind(string kind) {
            return GetSchema().ContainsKey(kind);
        }

        void CheckSchema(string kind, IDictionary<string, IConvertible> data) {
            var newColumns = GetColumnsFromData(data);

            if(!IsKnownKind(kind)) {
                ExecCreateTable(kind, newColumns);
                InvalidateSchema();
            } else {
                var oldColumns = GetSchema()[kind];
                var changedColumns = new TableColumns();
                var addedColumns = new TableColumns();

                foreach(var name in newColumns.Keys) {
                    if(!oldColumns.ContainsKey(name))
                        addedColumns[name] = newColumns[name];
                    else if(newColumns[name] > oldColumns[name])
                        changedColumns[name] = newColumns[name];
                }

                if(changedColumns.Count > 0 || addedColumns.Count > 0) {
                    UpdateSchema(kind, oldColumns, changedColumns, addedColumns);
                    InvalidateSchema();
                }
            }
        }

        protected abstract string GetPrimaryKeySqlType();

        protected string GetCreateTableStatementSuffix() {
            return String.Empty;
        }

        protected void ExecCreateTable(string kind, TableColumns columns) {
            var sql = new StringBuilder()
                .Append("create table ")
                .Append(QuoteName(kind))
                .Append(" (")
                .Append(QuoteName(Bean.ID_PROP_NAME))
                .Append(" ")
                .Append(GetPrimaryKeySqlType());

            foreach(var name in columns.Keys) {
                sql
                    .Append(", ")
                    .Append(QuoteName(name))
                    .Append(" ")
                    .Append(GetSqlTypeFromRank(columns[name]));
            }

            sql
                .Append(") ")
                .Append(GetCreateTableStatementSuffix());

            Db.Exec(sql.ToString());
        }

        public void EnterFluidMode() {
            _isFluidMode = true;
        }

        public virtual string QuoteName(string name) {
            if(name.Contains("`"))
                throw new ArgumentException();

            return "`" + name + "`";
        }

        public abstract long GetLastInsertID();

        protected virtual string FormatParam(string name) {
            return "@" + name;
        }

    }

}

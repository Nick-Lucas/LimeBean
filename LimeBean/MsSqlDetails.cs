#if !NO_MSSQL
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LimeBean {

    class MsSqlDetails : IDatabaseDetails {

        public const int
            RANK_BYTE = 0,
            RANK_INT32 = 1,
            RANK_INT64 = 2,
            RANK_DOUBLE = 3,
            RANK_TEXT_32 = 4,
            RANK_TEXT_4000 = 5,
            RANK_TEXT_MAX = 6;

        public string DbName {
            get { return "MsSql"; }
        }

        public string AutoIncrementSqlType {
            get { return "bigint identity(1,1) primary key"; }
        }

        public bool SupportsBoolean {
            get { return false; }
        }

        public bool SupportsDecimal {
            get { return false; }
        }

        public bool SupportsDateTime {
            get { return false; }
        }

        public string GetParamName(int index) {
            return "@p" + index;
        }

        public string QuoteName(string name) {
            if(name.Contains("]"))
                throw new ArgumentException();

            return "[" + name + "]";
        }

        public void ExecInitCommands(IDatabaseAccess db) {
        }

        public IConvertible ExecInsert(IDatabaseAccess db, string tableName, string autoIncrementName, IDictionary<string, IConvertible> data) {
            var hasAutoIncrement = !String.IsNullOrEmpty(autoIncrementName);

            var valuesPrefix = hasAutoIncrement
                ? "output inserted." + QuoteName(autoIncrementName)
                : null;

            var sql = CommonDatabaseDetails.FormatInsertCommand(this, tableName, data.Keys, valuesPrefix: valuesPrefix);
            var values = data.Values.ToArray();

            if(hasAutoIncrement)
                return db.Cell<IConvertible>(false, sql, values);

            db.Exec(sql, values);
            return null;
        }

        public string GetCreateTableStatementPostfix() {
            return null;
        }

        public int GetRankFromValue(IConvertible value) {
            if(value == null)
                return CommonDatabaseDetails.RANK_NULL;

            switch(value.GetTypeCode()) {
                case TypeCode.Byte:
                    return RANK_BYTE;

                case TypeCode.Int32:
                    return RANK_INT32;

                case TypeCode.Int64:
                    return RANK_INT64;

                case TypeCode.Double:
                    return RANK_DOUBLE;

                case TypeCode.String:
                    var len = (value as String).Length;

                    if(len <= 32)
                        return RANK_TEXT_32;

                    if(len <= 4000)
                        return RANK_TEXT_4000;

                    return RANK_TEXT_MAX;
            }

            throw new NotSupportedException();
        }

        public int GetRankFromSqlType(string sqlType) {
            if(sqlType.StartsWith("48:"))
                return RANK_BYTE;

            if(sqlType.StartsWith("56:"))
                return RANK_INT32;

            if(sqlType.StartsWith("127:"))
                return RANK_INT64;

            switch(sqlType) { 
                case "62:8":
                    return RANK_DOUBLE;

                case "231:64":
                    return RANK_TEXT_32;

                case "231:8000":
                    return RANK_TEXT_4000;

                case "231:-1":
                    return RANK_TEXT_MAX;
            }

            return CommonDatabaseDetails.RANK_CUSTOM;
        }

        public string GetSqlTypeFromRank(int rank) {
            switch(rank) { 
                case RANK_BYTE:
                    return "tinyint";

                case RANK_INT32:
                    return "int";

                case RANK_INT64:
                    return "bigint";

                case RANK_DOUBLE:
                    return "float(53)";

                case RANK_TEXT_32:
                    return "nvarchar(32)";

                case RANK_TEXT_4000:
                    return "nvarchar(4000)";

                case RANK_TEXT_MAX:
                    return "nvarchar(MAX)";
            }

            throw new NotSupportedException();
        }

        public IConvertible ConvertLongValue(long value) {
            if(value.IsUnsignedByteRange())
                return (byte)value;

            if(value.IsInt32Range())
                return (int)value;

            return value;
        }

        public string[] GetTableNames(IDatabaseAccess db) {
            return db.Col<string>(false, "select name from sys.objects where type='U'");
        }

        public IDictionary<string, IConvertible>[] GetColumns(IDatabaseAccess db, string tableName) {
            return db.Rows(false, "select name, system_type_id, max_length, is_nullable, object_definition(default_object_id) [default] from sys.columns where object_id = object_id({0})", tableName);
        }

        public bool IsNullableColumn(IDictionary<string, IConvertible> column) {
            return (bool)column["is_nullable"];
        }

        public IConvertible GetColumnDefaultValue(IDictionary<string, IConvertible> column) {
            return column["default"];
        }

        public string GetColumnName(IDictionary<string, IConvertible> column) {
            return (string)column["name"];
        }

        public string GetColumnType(IDictionary<string, IConvertible> column) {
            return String.Concat(column["system_type_id"], ":", column["max_length"]);
        }

        public void UpdateSchema(IDatabaseAccess db, string tableName, string autoIncrementName, IDictionary<string, int> oldColumns, IDictionary<string, int> changedColumns, IDictionary<string, int> addedColumns) {
            tableName = QuoteName(tableName);

            foreach(var entry in changedColumns)
                db.Exec(String.Format("alter table {0} alter column {1} {2}", tableName, QuoteName(entry.Key), GetSqlTypeFromRank(entry.Value)));

            foreach(var entry in addedColumns)
                db.Exec(String.Format("alter table {0} add {1} {2}", tableName, QuoteName(entry.Key), GetSqlTypeFromRank(entry.Value)));
        }

        public bool IsReadOnlyCommand(string text) {
            return Regex.IsMatch(text, @"^\s*select\W", RegexOptions.IgnoreCase);
        }
    }

}
#endif
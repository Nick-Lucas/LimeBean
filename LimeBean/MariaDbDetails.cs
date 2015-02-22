using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LimeBean {

    class MariaDbDetails : IDatabaseDetails {

        public const int
            RANK_INT8 = 0,
            RANK_INT32 = 1,
            RANK_INT64 = 2,
            RANK_DOUBLE = 3,
            RANK_TEXT5 = 4,
            RANK_TEXT8 = 5,
            RANK_TEXT16 = 6,
            RANK_TEXT24 = 7;

        public string DbName {
            get { return "MariaDB"; }
        }

        public string AutoIncrementSqlType {
            get { return "bigint not null auto_increment primary key"; }
        }

        public string GetParamName(int index) {
            return "@p" + index;
        }

        public string QuoteName(string name) {
            return CommonDatabaseDetails.QuoteWithBackticks(name);
        }

        public void ExecInitCommands(IDatabaseAccess db) {
            db.Exec("set names utf8");
        }

        public long GetLastInsertID(IDatabaseAccess db) {
            return db.Cell<long>(false, "select last_insert_id()");
        }

        public string GetCreateTableStatementPostfix() {
            return "engine=InnoDB default charset=utf8 collate=utf8_unicode_ci";
        }

        public string GetInsertDefaultsPostfix() {
            return "values ()";
        }

        public int GetRankFromValue(IConvertible value) {
            if(value == null)
                return CommonDatabaseDetails.RANK_NULL;

            switch(value.GetTypeCode()) {
                case TypeCode.SByte:
                    return RANK_INT8;

                case TypeCode.Int32:
                    return RANK_INT32;

                case TypeCode.Int64:
                    return RANK_INT64;

                case TypeCode.Double:
                    return RANK_DOUBLE;

                case TypeCode.String:
                    var len = (value as String).Length;

                    if(len <= 32)
                        return RANK_TEXT5;

                    if(len <= 255)
                        return RANK_TEXT8;

                    if(len <= 65535)
                        return RANK_TEXT16;

                    return RANK_TEXT24;
            }

            throw new NotSupportedException();
        }

        public int GetRankFromSqlType(string sqlType) {
            if(sqlType.Contains("unsigned"))
                return CommonDatabaseDetails.RANK_CUSTOM;

            if(sqlType.StartsWith("tinyint("))
                return RANK_INT8;

            if(sqlType.StartsWith("int("))
                return RANK_INT32;

            if(sqlType.StartsWith("bigint("))
                return RANK_INT64;

            if(sqlType == "double")
                return RANK_DOUBLE;

            if(sqlType == "varchar(32)")
                return RANK_TEXT5;

            if(sqlType == "varchar(255)" || sqlType == "tinytext")
                return RANK_TEXT8;

            if(sqlType == "text")
                return RANK_TEXT16;

            if(sqlType == "mediumtext")
                return RANK_TEXT24;

            return CommonDatabaseDetails.RANK_CUSTOM;
        }

        public string GetSqlTypeFromRank(int rank) {
            switch(rank) {
                case RANK_INT8:
                    return "tinyint";

                case RANK_INT32:
                    return "int";

                case RANK_INT64:
                    return "bigint";

                case RANK_DOUBLE:
                    return "double";

                case RANK_TEXT5:
                    return "varchar(32)";

                case RANK_TEXT8:
                    return "varchar(255)";

                case RANK_TEXT16:
                    return "text";

                case RANK_TEXT24:
                    return "mediumtext";
            }

            throw new NotSupportedException();
        }

        public IConvertible ConvertLongValue(long value) {
            if(-128L <= value && value <= 127L)
                return Convert.ToSByte(value);

            if(-0x80000000L <= value && value <= 0x7FFFFFFFL)
                return Convert.ToInt32(value);

            return value;
        }

        public string[] GetTableNames(IDatabaseAccess db) {
            return db.Col<string>(false, "show tables");
        }

        public IDictionary<string, IConvertible>[] GetColumns(IDatabaseAccess db, string tableName) {
            return db.Rows(false, "show columns from " + QuoteName(tableName));
        }

        public bool IsNullableColumn(IDictionary<string, IConvertible> column) {
            return "YES".Equals(column["Null"]);
        }

        public IConvertible GetColumnDefaultValue(IDictionary<string, IConvertible> column) {
            return column["Default"];
        }

        public string GetColumnName(IDictionary<string, IConvertible> column) {
            return column["Field"].ToString(CultureInfo.InvariantCulture);
        }

        public string GetColumnType(IDictionary<string, IConvertible> column) {
            return column["Type"].ToString(CultureInfo.InvariantCulture);
        }

        public void UpdateSchema(IDatabaseAccess db, string tableName, string autoIncrementName, IDictionary<string, int> oldColumns, IDictionary<string, int> changedColumns, IDictionary<string, int> addedColumns) {
            var operations = new List<string>();

            foreach(var entry in changedColumns)
                operations.Add(String.Format("change {0} {0} {1}", QuoteName(entry.Key), GetSqlTypeFromRank(entry.Value)));

            foreach(var entry in addedColumns)
                operations.Add(String.Format("add {0} {1}", QuoteName(entry.Key), GetSqlTypeFromRank(entry.Value)));

            db.Exec("alter table " + QuoteName(tableName) + " " + String.Join(", ", operations));
        }

        public bool IsReadOnlyCommand(string text) {
            return Regex.IsMatch(text, @"^\s*(select|show)\s", RegexOptions.IgnoreCase);
        }
    }

}

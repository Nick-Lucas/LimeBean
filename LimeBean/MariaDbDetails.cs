#if !NO_MARIADB
using System;
using System.Collections.Generic;
using System.Data.Common;
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
            RANK_TEXT24 = 7,
            RANK_STATIC_DATETIME = CommonDatabaseDetails.RANK_STATIC_BASE + 1;

        public string DbName {
            get { return "MariaDB"; }
        }

        public string AutoIncrementSqlType {
            get { return "bigint not null auto_increment primary key"; }
        }

        public bool SupportsBoolean {
            get { return false; }
        }

        public bool SupportsDecimal {
            get { return false; }
        }

        public string GetParamName(int index) {
            return "@p" + index;
        }

        public void CustomizeParam(DbParameter p) { 
        }

        public string QuoteName(string name) {
            return CommonDatabaseDetails.QuoteWithBackticks(name);
        }

        public void ExecInitCommands(IDatabaseAccess db) {
            db.Exec("set names utf8");
        }

        public object ExecInsert(IDatabaseAccess db, string tableName, string autoIncrementName, IDictionary<string, object> data) {
            db.Exec(
                CommonDatabaseDetails.FormatInsertCommand(this, tableName, data.Keys, defaultsExpr: "values ()"),
                data.Values.ToArray()
            );

            if(String.IsNullOrEmpty(autoIncrementName))
                return null;

            // per-connection, http://stackoverflow.com/q/21185666
            // robust to triggers, http://dba.stackexchange.com/a/25141

            return db.Cell<object>(false, "select last_insert_id()");
        }

        public string GetCreateTableStatementPostfix() {
            return "engine=InnoDB default charset=utf8 collate=utf8_unicode_ci";
        }

        public int GetRankFromValue(object value) {
            if(value == null)
                return CommonDatabaseDetails.RANK_NULL;

            if(value is SByte)
                return RANK_INT8;

            if(value is Int32)
                return RANK_INT32;

            if(value is Int64)
                return RANK_INT64;

            if(value is Double)
                return RANK_DOUBLE;

            if(value is String) {
                var len = (value as String).Length;

                if(len <= 32)
                    return RANK_TEXT5;

                if(len <= 255)
                    return RANK_TEXT8;

                if(len <= 65535)
                    return RANK_TEXT16;

                return RANK_TEXT24;
            }

            if(value is DateTime)
                return RANK_STATIC_DATETIME;

            return CommonDatabaseDetails.RANK_CUSTOM;
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

            if(sqlType == "datetime")
                return RANK_STATIC_DATETIME;

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

                case RANK_STATIC_DATETIME:
                    return "datetime";
            }

            throw new NotSupportedException();
        }

        public object ConvertLongValue(long value) {
            if(value.IsSignedByteRange())
                return (sbyte)value;

            if(value.IsInt32Range())
                return (int)value;

            return value;
        }

        public string[] GetTableNames(IDatabaseAccess db) {
            return db.Col<string>(false, "show tables");
        }

        public IDictionary<string, object>[] GetColumns(IDatabaseAccess db, string tableName) {
            return db.Rows(false, "show columns from " + QuoteName(tableName));
        }

        public bool IsNullableColumn(IDictionary<string, object> column) {
            return "YES".Equals(column["Null"]);
        }

        public object GetColumnDefaultValue(IDictionary<string, object> column) {
            return column["Default"];
        }

        public string GetColumnName(IDictionary<string, object> column) {
            return (string)column["Field"];
        }

        public string GetColumnType(IDictionary<string, object> column) {
            return (string)column["Type"];
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
            return Regex.IsMatch(text, @"^\s*(select|show)\W", RegexOptions.IgnoreCase);
        }
    }

}
#endif
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

            // http://stackoverflow.com/a/19472829
            // TODO is it worth adding varchar(1024)?
            RANK_TEXT_36 = 4,   // up to Guid len
            RANK_TEXT_191 = 5,  // 767-byte index limit with 4 bytes per char
            RANK_TEXT_MAX = 6,

            RANK_STATIC_DATETIME = CommonDatabaseDetails.RANK_STATIC_BASE + 1,
            RANK_STATIC_BLOB = CommonDatabaseDetails.RANK_STATIC_BASE + 2;

        string _charset;

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

        public string QuoteName(string name) {
            return CommonDatabaseDetails.QuoteWithBackticks(name);
        }

        public void ExecInitCommands(IDatabaseAccess db) {
            _charset = db.Cell<string>(false, "show charset like 'utf8mb4'") != null ? "utf8mb4" : "utf8";
            db.Exec("set names " + _charset);
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
            return String.Format("engine=InnoDB default charset={0} collate={0}_unicode_ci", _charset);
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

                if(len <= 36)
                    return RANK_TEXT_36;

                if(len <= 191)
                    return RANK_TEXT_191;

                return RANK_TEXT_MAX;
            }

            if(value is DateTime)
                return RANK_STATIC_DATETIME;

            if(value is Guid)
                return RANK_TEXT_36;

            if(value is byte[])
                return RANK_STATIC_BLOB;

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

            if(sqlType == "varchar(36)")
                return RANK_TEXT_36;

            if(sqlType == "varchar(191)")
                return RANK_TEXT_191;

            if(sqlType == "longtext")
                return RANK_TEXT_MAX;

            if(sqlType == "datetime")
                return RANK_STATIC_DATETIME;

            if(sqlType == "longblob")
                return RANK_STATIC_BLOB;

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

                case RANK_TEXT_36:
                    return "varchar(36)";

                case RANK_TEXT_191:
                    return "varchar(191)";

                case RANK_TEXT_MAX:
                    return "longtext";

                case RANK_STATIC_DATETIME:
                    return "datetime";

                case RANK_STATIC_BLOB:
                    return "longblob";
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

             CommonDatabaseDetails.FixLongToDoubleUpgrade(this, db, tableName, oldColumns, changedColumns, RANK_INT64, RANK_DOUBLE, RANK_TEXT_36);

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
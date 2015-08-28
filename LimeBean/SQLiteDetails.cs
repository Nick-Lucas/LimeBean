using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LimeBean {

    class SQLiteDetails : IDatabaseDetails {

        public const int RANK_ANY = 0;

        public string DbName {
            get { return "SQLite"; }
        }

        public string AutoIncrementSqlType {
            get { return "integer primary key"; }
        }

        public bool SupportsBoolean {
            get { return false; }
        }

        public bool SupportsDecimal {
            get { return false; }
        }

        public string GetParamName(int index) {
            return ":p" + index;
        }

        public string QuoteName(string name) {
            return CommonDatabaseDetails.QuoteWithBackticks(name);
        }

        public void ExecInitCommands(IDatabaseAccess db) {            
        }

        public object ExecInsert(IDatabaseAccess db, string tableName, string autoIncrementName, IDictionary<string, object> data) {
            db.Exec(
                CommonDatabaseDetails.FormatInsertCommand(this, tableName, data.Keys),
                data.Values.ToArray()
            );

            if(String.IsNullOrEmpty(autoIncrementName))
                return null;

            // per-connection, robust to triggers
            // http://www.sqlite.org/c3ref/last_insert_rowid.html

            return db.Cell<long>(false, "select last_insert_rowid()");
        }

        public string GetCreateTableStatementPostfix() {
            return null;
        }

        public int GetRankFromValue(object value) {
            if(value == null)
                return CommonDatabaseDetails.RANK_NULL;

            return RANK_ANY;
        }

        public int GetRankFromSqlType(string sqlType) {
            return RANK_ANY;
        }

        public string GetSqlTypeFromRank(int rank) {
            return null;
        }

        public object ConvertLongValue(long value) {
            return value;
        }

        public string[] GetTableNames(IDatabaseAccess db) {
            return db.Col<string>(false, "select name from sqlite_master where type = 'table' and name <> 'sqlite_sequence'");
        }

        public IDictionary<string, object>[] GetColumns(IDatabaseAccess db, string tableName) {
            return db.Rows(false, "pragma table_info(" + QuoteName(tableName) + ")");
        }

        public bool IsNullableColumn(IDictionary<string, object> column) {
            return true;
        }

        public object GetColumnDefaultValue(IDictionary<string, object> column) {
            return null;
        }

        public string GetColumnName(IDictionary<string, object> column) {
            return (string)column["name"];
        }

        public string GetColumnType(IDictionary<string, object> column) {
            return null;
        }

        public void UpdateSchema(IDatabaseAccess db, string tableName, string autoIncrementName, IDictionary<string, int> oldColumns, IDictionary<string, int> changedColumns, IDictionary<string, int> addedColumns) {
            if(changedColumns.Count > 0)
                throw new NotSupportedException();

            foreach(var entry in addedColumns)
                db.Exec("alter table " + QuoteName(tableName) + " add " + QuoteName(entry.Key) + " " + GetSqlTypeFromRank(entry.Value));
        }

        public bool IsReadOnlyCommand(string text) {
            return Regex.IsMatch(text, @"^\s*(select\W|pragma table_info)", RegexOptions.IgnoreCase);            
        }
    }

}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LimeBean {

    class SQLiteDetails : IDatabaseDetails {

        // NOTE using NUMERIC affinity does not work because driver forces conversion to Decimal
        public const int
            RANK_ANY = 0,
            RANK_TEXT = 1;

        public string DbName {
            get { return "SQLite"; }
        }

        public string AutoIncrementSqlType {
            get { return "integer primary key"; }
        }

        public string GetParamName(int index) {
            return ":p" + index;
        }

        public string QuoteName(string name) {
            return CommonDatabaseDetails.QuoteWithBackticks(name);
        }

        public void ExecInitCommands(IDatabaseAccess db) {            
        }

        public IConvertible ExecInsert(IDatabaseAccess db, string tableName, string autoIncrementName, IDictionary<string, IConvertible> data) {
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

        public int GetRankFromValue(IConvertible value) {
            if(value == null)
                return CommonDatabaseDetails.RANK_NULL;

            switch(value.GetTypeCode()) {
                case TypeCode.Int64:
                case TypeCode.Double:
                    return RANK_ANY;

                case TypeCode.String:
                    return RANK_TEXT;
            }

            throw new NotSupportedException();
        }

        public int GetRankFromSqlType(string sqlType) {
            // according to http://www.sqlite.org/datatype3.html#affname

            if(String.IsNullOrEmpty(sqlType))
                return RANK_ANY;

            sqlType = sqlType.ToLower();

            if(!sqlType.Contains("int")) {
                if(sqlType.Contains("char") || sqlType.Contains("clob") || sqlType.Contains("text"))
                    return RANK_TEXT;

                if(sqlType.Contains("blob"))
                    return RANK_ANY;
            }

            return CommonDatabaseDetails.RANK_CUSTOM;
        }

        public string GetSqlTypeFromRank(int rank) {
            switch(rank) {
                case RANK_ANY:
                    return null;

                case RANK_TEXT:
                    return "text";
            }

            throw new NotSupportedException();
        }

        public IConvertible ConvertLongValue(long value) {
            return value;
        }

        public string[] GetTableNames(IDatabaseAccess db) {
            return db.Col<string>(false, "select name from sqlite_master where type = 'table' and name <> 'sqlite_sequence'");
        }

        public IDictionary<string, IConvertible>[] GetColumns(IDatabaseAccess db, string tableName) {
            return db.Rows(false, "pragma table_info(" + QuoteName(tableName) + ")");
        }

        public bool IsNullableColumn(IDictionary<string, IConvertible> column) {
            return !column["notnull"].ToBoolean(CultureInfo.InvariantCulture);            
        }

        public IConvertible GetColumnDefaultValue(IDictionary<string, IConvertible> column) {
            return column["dflt_value"];
        }

        public string GetColumnName(IDictionary<string, IConvertible> column) {
            return column["name"].ToString(CultureInfo.InvariantCulture);
        }

        public string GetColumnType(IDictionary<string, IConvertible> column) {
            return column["type"].ToString(CultureInfo.InvariantCulture);
        }

        public void UpdateSchema(IDatabaseAccess db, string tableName, string autoIncrementName, IDictionary<string, int> oldColumns, IDictionary<string, int> changedColumns, IDictionary<string, int> addedColumns) {
            var quotedTableName = QuoteName(tableName);

            if(changedColumns.Count > 0) {
                var tmpName = "lime_tmp_" + Guid.NewGuid().ToString("N");

                var orderedOldColumns = new List<KeyValuePair<string, int>>(oldColumns.Count);
                var orderedNewColumns = new List<KeyValuePair<string, int>>(oldColumns.Count);

                foreach(var pair in oldColumns) {
                    orderedOldColumns.Add(pair);

                    var name = pair.Key;
                    if(changedColumns.ContainsKey(name))
                        orderedNewColumns.Add(new KeyValuePair<string, int>(name, changedColumns[name]));                        
                    else
                        orderedNewColumns.Add(pair);
                }               

                db.Exec("drop table if exists " + tmpName);
                db.Exec(CommonDatabaseDetails.FormatCreateTableCommand(this, tmpName, autoIncrementName, orderedOldColumns));
                db.Exec("insert into " + tmpName + " select * from " + quotedTableName);
                db.Exec("drop table " + quotedTableName);
                db.Exec(CommonDatabaseDetails.FormatCreateTableCommand(this, tableName, autoIncrementName, orderedNewColumns));
                db.Exec("insert into " + quotedTableName + " select * from " + tmpName);
                db.Exec("drop table " + tmpName);
            }

            foreach(var entry in addedColumns)
                db.Exec("alter table " + quotedTableName + " add " + QuoteName(entry.Key) + " " + GetSqlTypeFromRank(entry.Value));
        }

        public bool IsReadOnlyCommand(string text) {
            return Regex.IsMatch(text, @"^\s*(select\s|pragma table_info)", RegexOptions.IgnoreCase);            
        }
    }

}

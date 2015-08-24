#if !NO_PGSQL
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LimeBean {

    class PgSqlDetails : IDatabaseDetails {

        public const int
            RANK_BOOLEAN = 0,
            RANK_INT32 = 1,
            RANK_INT64 = 2,
            RANK_DOUBLE = 3,
            RANK_NUMERIC = 4,
            RANK_TEXT = 5,
            RANK_STATIC_DATETIME = CommonDatabaseDetails.RANK_STATIC_BASE + 1;

        public string DbName {
            get { return "PgSql"; }
        }

        public string AutoIncrementSqlType {
            get { return "bigserial"; }
        }

        public bool SupportsBoolean {
            get { return true; }
        }

        public bool SupportsDecimal {
            get { return true; }
        }

        public bool SupportsDateTime {
            get { return true; }
        }

        public string GetParamName(int index) {
            return ":p" + index;
        }

        public string QuoteName(string name) {
            return '"' + name + '"';
        }

        public void ExecInitCommands(IDatabaseAccess db) {
            // set names?
        }

        public IConvertible ExecInsert(IDatabaseAccess db, string tableName, string autoIncrementName, IDictionary<string, IConvertible> data) {
            var hasAutoIncrement = !String.IsNullOrEmpty(autoIncrementName);

            var postfix = hasAutoIncrement
                ? "returning " + QuoteName(autoIncrementName)
                : null;

            var sql = CommonDatabaseDetails.FormatInsertCommand(this, tableName, data.Keys, postfix: postfix);
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
                case TypeCode.Boolean:
                    return RANK_BOOLEAN;

                case TypeCode.Int32:
                    return RANK_INT32;

                case TypeCode.Int64:
                    return RANK_INT64;

                case TypeCode.Double:
                    return RANK_DOUBLE;

                case TypeCode.Decimal:
                    return RANK_NUMERIC;

                case TypeCode.String:
                    return RANK_TEXT;

                case TypeCode.DateTime:
                    return RANK_STATIC_DATETIME;
            }

            throw new NotSupportedException();
        }

        public int GetRankFromSqlType(string sqlType) {
            switch(sqlType) {
                case "boolean":
                    return RANK_BOOLEAN;

                case "integer 32":
                    return RANK_INT32;

                case "bigint 64":
                    return RANK_INT64;

                case "double precision 53":
                    return RANK_DOUBLE;

                case "numeric":
                    return RANK_NUMERIC;

                case "text":
                    return RANK_TEXT;

                case "timestamp without time zone":
                    return RANK_STATIC_DATETIME;
            }

            return CommonDatabaseDetails.RANK_CUSTOM;
        }

        public string GetSqlTypeFromRank(int rank) {
            switch(rank) {
                case RANK_BOOLEAN:
                    return "boolean";

                case RANK_INT32:
                    return "integer";

                case RANK_INT64:
                    return "bigint";

                case RANK_DOUBLE:
                    return "double precision";

                case RANK_NUMERIC:
                    return "numeric";

                case RANK_TEXT:
                    return "text";

                case RANK_STATIC_DATETIME:
                    return "timestamp without time zone";
            }

            throw new NotSupportedException();
        }

        public IConvertible ConvertLongValue(long value) {
            if(value.IsInt32Range())
                return (int)value;

            return value;
        }

        public string[] GetTableNames(IDatabaseAccess db) {
            return db.Col<string>(false, "select table_name from information_schema.tables where table_schema = 'public'");
        }

        public IDictionary<string, IConvertible>[] GetColumns(IDatabaseAccess db, string tableName) {
            return db.Rows(false, "select * from information_schema.columns where table_name = {0}", tableName);
        }

        public bool IsNullableColumn(IDictionary<string, IConvertible> column) {
            return "YES".Equals(column["is_nullable"]);
        }

        public IConvertible GetColumnDefaultValue(IDictionary<string, IConvertible> column) {
            return column["column_default"];
        }

        public string GetColumnName(IDictionary<string, IConvertible> column) {
            return (string)column["column_name"];
        }

        public string GetColumnType(IDictionary<string, IConvertible> column) {
            var type = (string)column["data_type"];
            var prec = column["numeric_precision"];
            if(prec != null)
                type += " " + prec;
            return type;
        }

        public void UpdateSchema(IDatabaseAccess db, string tableName, string autoIncrementName, IDictionary<string, int> oldColumns, IDictionary<string, int> changedColumns, IDictionary<string, int> addedColumns) {
            var operations = new List<string>();

            foreach(var entry in changedColumns) {
                var name = entry.Key;
                var quotedName = QuoteName(name);

                var oldRank = oldColumns[name];
                var newRank = entry.Value;

                if(newRank == RANK_DOUBLE && oldRank == RANK_INT64)
                    newRank = RANK_NUMERIC;

                var op = String.Format("alter {0} type {1}", quotedName, GetSqlTypeFromRank(newRank));

                if(oldRank == RANK_BOOLEAN) {
                    op += " using case"
                        + " when " + quotedName + " is null then null"
                        + " when " + quotedName + " = false then 0" 
                        + " else 1" 
                        + " end";
                }

                operations.Add(op);
            }

            foreach(var entry in addedColumns)
                operations.Add(String.Format("add {0} {1}", QuoteName(entry.Key), GetSqlTypeFromRank(entry.Value)));

            db.Exec("alter table " + QuoteName(tableName) + " " + String.Join(", ", operations));
        }

        public bool IsReadOnlyCommand(string text) {
            return Regex.IsMatch(text, @"^\s*select\W", RegexOptions.IgnoreCase);
        }
    }

}
#endif
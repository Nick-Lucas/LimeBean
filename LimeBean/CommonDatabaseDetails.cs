using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LimeBean.Interfaces;

namespace LimeBean {

    static class CommonDatabaseDetails {
        public const int 
            RANK_NULL = Int32.MinValue,
            RANK_STATIC_BASE = 100,
            RANK_CUSTOM = Int32.MaxValue;

        public static string QuoteWithBackticks(string text) {
            if(text.Contains("`"))
                throw new ArgumentException();

            return "`" + text + "`";
        }

        public static string FormatCreateTableCommand(IDatabaseDetails details, string tableName, string autoIncrementName, ICollection<KeyValuePair<string, int>> columns) {
            var sql = new StringBuilder()
                .Append("create table ")
                .Append(details.QuoteName(tableName))
                .Append(" (");

            var colSpecs = new List<string>(1 + columns.Count);

            if(!String.IsNullOrEmpty(autoIncrementName))
                colSpecs.Add(details.QuoteName(autoIncrementName) + " " + details.AutoIncrementSqlType);

            foreach(var pair in columns)
                colSpecs.Add(details.QuoteName(pair.Key) + " " + details.GetSqlTypeFromRank(pair.Value));            

            sql
                .Append(String.Join(", ", colSpecs))
                .Append(")");

            var postfix = details.GetCreateTableStatementPostfix();
            if(!String.IsNullOrEmpty(postfix))
                sql.Append(" ").Append(postfix);

            return sql.ToString();
        }

        public static string FormatInsertCommand(IDatabaseDetails details, string tableName, ICollection<string> fieldNames, string valuesPrefix = null, string defaultsExpr = "default values", string postfix = null) {
            var builder = new StringBuilder("insert into ")
                .Append(details.QuoteName(tableName))
                .Append(" ");

            if(fieldNames.Count > 0) {
                builder
                    .Append("(")
                    .Append(String.Join(", ", fieldNames.Select(details.QuoteName)))
                    .Append(") ");
            }
                
            if(!String.IsNullOrEmpty(valuesPrefix))
                builder.Append(valuesPrefix).Append(" ");

            if(fieldNames.Count > 0) {
                builder.Append("values (");
                for(var i = 0; i < fieldNames.Count; i++) {
                    if(i > 0)
                        builder.Append(", ");
                    builder.Append("{").Append(i).Append("}");
                }
                builder.Append(")");
            } else {
                builder.Append(defaultsExpr);
            }

            if(!String.IsNullOrEmpty(postfix))
                builder.Append(" ").Append(postfix);

            return builder.ToString();
        }

        public static void FixLongToDoubleUpgrade(IDatabaseDetails details, IDatabaseAccess db, string tableName, IDictionary<string, int> oldColumns, IDictionary<string, int> changedColumns, int longRank, int doubleRank, int safeRank) {
            var names = new List<string>(changedColumns.Keys);
            var quotedTableName = details.QuoteName(tableName);

            foreach(var name in names) {
                var oldRank = oldColumns[name];
                var newRank = changedColumns[name];

                if(oldRank == longRank && newRank == doubleRank) {
                    var quotedName = details.QuoteName(name);
                    var min = db.Cell<long>(false, "select min(" + quotedName + ") from " + quotedTableName);
                    var max = db.Cell<long>(false, "select max(" + quotedName + ") from " + quotedTableName);
                    if(!min.IsInt53Range() || !max.IsInt53Range())
                        changedColumns[name] = safeRank;
                }
            }
        }


    }

}

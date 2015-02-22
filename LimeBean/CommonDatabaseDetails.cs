using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {

    static class CommonDatabaseDetails {
        public const int 
            RANK_NULL = Int32.MinValue,
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

    }

}

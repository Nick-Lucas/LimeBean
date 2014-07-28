using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {

    static class CommonDatabaseDetails {
        public const int RANK_CUSTOM = Int32.MaxValue;

        public static string QuoteWithBackticks(string text) {
            if(text.Contains("`"))
                throw new ArgumentException();

            return "`" + text + "`";
        }

        public static string FormatCreateTableCommand(IDatabaseDetails details, string tableName, IEnumerable<KeyValuePair<string, int>> columns) {
            var sql = new StringBuilder()
                .Append("create table ")
                .Append(details.QuoteName(tableName))
                .Append(" (")
                .Append(details.QuoteName(Bean.ID_PROP_NAME))
                .Append(" ")
                .Append(details.PrimaryKeySqlType);

            foreach(var pair in columns) {
                sql
                    .Append(", ")
                    .Append(details.QuoteName(pair.Key))
                    .Append(" ")
                    .Append(details.GetSqlTypeFromRank(pair.Value));
            }

            sql.Append(")");

            var postfix = details.GetCreateTableStatementPostfix();
            if(!String.IsNullOrEmpty(postfix))
                sql.Append(" ").Append(postfix);

            return sql.ToString();
        }


    }

}

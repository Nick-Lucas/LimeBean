using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace LimeBean {

    interface IDatabaseDetails {
        string DbName { get; }
        string AutoIncrementSqlType { get; }
        bool SupportsBoolean { get; }
        bool SupportsDecimal { get; }

        string GetParamName(int index);
        string QuoteName(string name);

        void ExecInitCommands(IDatabaseAccess db);
        object ExecInsert(IDatabaseAccess db, string tableName, string autoIncrementName, IDictionary<string, object> data);
        string GetCreateTableStatementPostfix();

        int GetRankFromValue(object value);
        int GetRankFromSqlType(string sqlType);
        string GetSqlTypeFromRank(int rank);

        object ConvertLongValue(long value);

        string[] GetTableNames(IDatabaseAccess db);
        IDictionary<string, object>[] GetColumns(IDatabaseAccess db, string tableName);
        bool IsNullableColumn(IDictionary<string, object> column);
        object GetColumnDefaultValue(IDictionary<string, object> column);
        string GetColumnName(IDictionary<string, object> column);
        string GetColumnType(IDictionary<string, object> column);

        void UpdateSchema(
            IDatabaseAccess db, 
            string tableName, 
            string autoIncrementName,
            IDictionary<string, int> oldColumns, 
            IDictionary<string, int> changedColumns, 
            IDictionary<string, int> addedColumns
        );

        bool IsReadOnlyCommand(string text);
    }

}

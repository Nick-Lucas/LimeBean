using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {

    interface IDatabaseDetails {
        string DbName { get; }
        string PrimaryKeySqlType { get; }

        string GetParamName(int index);
        string QuoteName(string name);

        void ExecInitCommands(IDatabaseAccess db);
        long GetLastInsertID(IDatabaseAccess db);
        string GetCreateTableStatementPostfix();
        string GetInsertDefaultsPostfix();

        int GetRankFromValue(IConvertible value);
        int GetRankFromSqlType(string sqlType);
        string GetSqlTypeFromRank(int rank);

        IConvertible ConvertLongValue(long value);

        string[] GetTableNames(IDatabaseAccess db);
        IDictionary<string, IConvertible>[] GetColumns(IDatabaseAccess db, string tableName);
        bool IsNullableColumn(IDictionary<string, IConvertible> column);
        IConvertible GetColumnDefaultValue(IDictionary<string, IConvertible> column);
        string GetColumnName(IDictionary<string, IConvertible> column);
        string GetColumnType(IDictionary<string, IConvertible> column);

        void UpdateSchema(
            IDatabaseAccess db, 
            string tableName, 
            IDictionary<string, int> oldColumns, 
            IDictionary<string, int> changedColumns, 
            IDictionary<string, int> addedColumns
        );

        bool IsReadOnlyCommand(string text);
    }

}

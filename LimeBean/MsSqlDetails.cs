using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LimeBean {

    class MsSqlDetails : IDatabaseDetails {

        public const int
            RANK_BYTE = 0,
            RANK_INT32 = 1,
            RANK_INT64 = 2,
            RANK_DOUBLE = 3,
            RANK_TEXT_32 = 4,
            RANK_TEXT_4000 = 5,
            RANK_TEXT_MAX = 6;

        public string DbName {
            get { return "MsSql"; }
        }

        public string PrimaryKeySqlType {
            get { return "bigint identity(1,1) primary key"; }
        }

        public string GetParamName(int index) {
            return "@p" + index;
        }

        public string QuoteName(string name) {
            if(name.Contains("]"))
                throw new ArgumentException();

            return "[" + name + "]";
        }

        public void ExecInitCommands(IDatabaseAccess db) {
        }

        public long GetLastInsertID(IDatabaseAccess db) {
            return db.Cell<long>(false, "select @@IDENTITY");
        }

        public string GetCreateTableStatementPostfix() {
            return null;
        }

        public int GetRankFromValue(IConvertible value) {
            throw new NotImplementedException();
        }

        public int GetRankFromSqlType(string sqlType) {
            if(sqlType.StartsWith("48:"))
                return RANK_BYTE;

            if(sqlType.StartsWith("56:"))
                return RANK_INT32;

            if(sqlType.StartsWith("127:"))
                return RANK_INT64;

            switch(sqlType) { 
                case "62:8":
                    return RANK_DOUBLE;

                case "231:64":
                    return RANK_TEXT_32;

                case "231:8000":
                    return RANK_TEXT_4000;

                case "231:-1":
                    return RANK_TEXT_MAX;
            }

            return CommonDatabaseDetails.RANK_CUSTOM;
        }

        public string GetSqlTypeFromRank(int rank) {
            throw new NotImplementedException();
        }

        public IConvertible ConvertLongValue(long value) {
            if(0L <= value && value <= 255L)
                return Convert.ToByte(value);

            if(-0x80000000L <= value && value <= 0x7FFFFFFFL)
                return Convert.ToInt32(value);

            return value;
        }

        public string[] GetTableNames(IDatabaseAccess db) {
            return db.Col<string>(false, "select name from sys.objects where type='U'");
        }

        public IDictionary<string, IConvertible>[] GetColumns(IDatabaseAccess db, string tableName) {
            return db.Rows(false, "select name, system_type_id, max_length, is_nullable, object_definition(default_object_id) [default] from sys.columns where object_id = object_id({0})", tableName);
        }

        public bool IsNullableColumn(IDictionary<string, IConvertible> column) {
            return column["is_nullable"].ToBoolean(CultureInfo.InvariantCulture);
        }

        public IConvertible GetColumnDefaultValue(IDictionary<string, IConvertible> column) {
            return column["default"];
        }

        public string GetColumnName(IDictionary<string, IConvertible> column) {
            return column["name"].ToString(CultureInfo.InvariantCulture);
        }

        public string GetColumnType(IDictionary<string, IConvertible> column) {
            return String.Concat(column["system_type_id"], ":", column["max_length"]);
        }

        public void UpdateSchema(IDatabaseAccess db, string tableName, IDictionary<string, int> oldColumns, IDictionary<string, int> changedColumns, IDictionary<string, int> addedColumns) {
            throw new NotImplementedException();
        }

        public bool IsReadOnlyCommand(string text) {
            return Regex.IsMatch(text, @"^\s*select\s", RegexOptions.IgnoreCase);
        }
    }

}

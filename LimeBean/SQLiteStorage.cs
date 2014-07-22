using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LimeBean {

    using TableColumns = Dictionary<string, int>;
    using Schema = Dictionary<string, Dictionary<string, int>>;

    class SQLiteStorage : DatabaseStorage {

        // NOTE using NUMERIC affinity does not work because driver forces conversion to Decimal
        internal const int
            RANK_NONE = 0,
            RANK_TEXT = 1;

        public SQLiteStorage(IDatabaseAccess db)
            : base(db) {
        }

        protected override IConvertible ConvertPropertyValue(IConvertible value) {
            if(value == null)
                return null;

            switch(value.GetTypeCode()) {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                    return value.ToInt64(CultureInfo.InvariantCulture);

                case TypeCode.Single:
                case TypeCode.Double:
                    return value.ToDouble(CultureInfo.InvariantCulture);
            }

            return value.ToString(CultureInfo.InvariantCulture);
        }

        protected override int GetRankFromValue(IConvertible value) {
            if(value == null)
                return RANK_NONE;

            switch(value.GetTypeCode()) {
                case TypeCode.Int64:
                case TypeCode.Double:
                    return RANK_NONE;

                case TypeCode.String:
                    return RANK_TEXT;
            }

            throw new NotSupportedException();
        }

        protected override int GetRankFromSqlType(string sqlType) {
            switch(sqlType.ToLower()) { 
                case "none":
                    return RANK_NONE;

                case "text":
                    return RANK_TEXT;
            }

            return RANK_MAX;
        }

        protected override string GetSqlTypeFromRank(int rank) {
            switch(rank) { 
                case RANK_NONE:
                    return "none";

                case RANK_TEXT:
                    return "text";
            }

            throw new NotSupportedException();
        }

        protected override Schema LoadSchema() {
            var result = new Schema();
            var tables = Db.Col<string>("select name from sqlite_master where type = 'table' and name <> 'sqlite_sequence'");
            foreach(var tableName in tables) {
                var columns = new TableColumns();
                foreach(var row in Db.Rows("pragma table_info(" + QuoteName(tableName) + ")")) {
                    var isKey = Convert.ToBoolean(row["pk"]);
                    if(isKey)
                        continue;
                    columns[Convert.ToString(row["name"])] = GetRankFromSqlType(Convert.ToString(row["type"]));
                }
                result[tableName] = columns;
            }

            return result;
        }

        protected override void UpdateSchema(string kind, TableColumns oldColumns, TableColumns changedColumns, TableColumns addedColumns) {
            if(changedColumns.Count > 0) {
                var tmpName = "lime_tmp_" + Guid.NewGuid().ToString("N");
                var quotedKind = QuoteName(kind);

                var newColumns = new TableColumns(oldColumns);
                foreach(var entry in changedColumns)
                    newColumns[entry.Key] = entry.Value;

                Db.Exec("drop table if exists " + tmpName);
                ExecCreateTable(tmpName, oldColumns);
                Db.Exec("insert into " + tmpName + " select * from " + quotedKind);
                Db.Exec("drop table " + quotedKind);
                ExecCreateTable(kind, newColumns);
                Db.Exec("insert into " + quotedKind + " select * from " + tmpName);
                Db.Exec("drop table " + tmpName);
            }
            
            foreach(var entry in addedColumns)
                Db.Exec("alter table " + QuoteName(kind) + " add " + QuoteName(entry.Key) + " " + GetSqlTypeFromRank(entry.Value));
        }

        protected override string GetPrimaryKeySqlType() {
            return "integer primary key";
        }

        public override long GetLastInsertID() {
            return Db.Cell<long>("select last_insert_rowid()");
        }

    }

}

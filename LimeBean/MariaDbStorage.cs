using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {

    using TableColumns = Dictionary<string, int>;
    using Schema = Dictionary<string, Dictionary<string, int>>;

    class MariaDbStorage : DatabaseStorage {
        internal const int
            RANK_INT8 = 0,
            RANK_INT32 = 1,
            RANK_INT64 = 2,
            RANK_DOUBLE = 3, // TODO 
            RANK_TEXT5 = 4,
            RANK_TEXT8 = 5,
            RANK_TEXT16 = 6,
            RANK_TEXT24 = 7;

        public MariaDbStorage(IDatabaseAccess db)
            : base(db) {
            db.Exec("set names utf8");
        }

        public override string DbName {
            get { return "MariaDB"; }
        }

        protected override int GetRankFromValue(IConvertible value) {
            throw new NotImplementedException();
        }

        protected override int GetRankFromSqlType(string sqlType) {
            if(sqlType.Contains("unsigned"))
                return RANK_CUSTOM;

            if(sqlType.StartsWith("tinyint("))
                return RANK_INT8;

            if(sqlType.StartsWith("int("))
                return RANK_INT32;

            if(sqlType.StartsWith("bigint("))
                return RANK_INT64;

            if(sqlType == "double")
                return RANK_DOUBLE;

            if(sqlType == "varchar(32)")
                return RANK_TEXT5;

            if(sqlType == "varchar(255)" || sqlType == "tinytext")
                return RANK_TEXT8;

            if(sqlType == "text")
                return RANK_TEXT16;

            if(sqlType == "mediumtext")
                return RANK_TEXT24;

            return RANK_CUSTOM;
        }

        protected override string GetSqlTypeFromRank(int rank) {
            throw new NotImplementedException();
        }

        protected override Schema LoadSchema() {
            var result = new Schema();

            foreach(var tableName in Db.Col<string>(false, "show tables")) {
                var columns = new TableColumns();
                foreach(var row in Db.Rows(false, "show columns from " + QuoteName(tableName))) {
                    if("PRI".Equals(row["Key"]))
                        continue;

                    var allowsNull = "YES".Equals(row["Null"]);
                    var hasDefault = row["Default"] != null;

                    columns[Convert.ToString(row["Field"])] = !allowsNull || hasDefault
                        ? RANK_CUSTOM
                        : GetRankFromSqlType(Convert.ToString(row["Type"]));
                }
                result[tableName] = columns;
            }

            return result;
        }

        protected override void UpdateSchema(string kind, TableColumns oldColumns, TableColumns changedColumns, TableColumns addedColumns) {
            throw new NotImplementedException();
        }

        protected override string GetPrimaryKeySqlType() {
            throw new NotImplementedException();
        }

        public override long GetLastInsertID() {
            throw new NotImplementedException();
        }
    }

}

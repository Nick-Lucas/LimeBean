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

        protected override IConvertible ConvertLongValue(long value) {
            if(-128L <= value && value <= 127L)
                return Convert.ToSByte(value);

            if(-0x80000000L <= value && value <= 0x7FFFFFFFL)
                return Convert.ToInt32(value);

            return value;
        }

        protected override int GetRankFromValue(IConvertible value) {
            if(value == null)
                return RANK_INT8;

            switch(value.GetTypeCode()) { 
                case TypeCode.SByte:
                    return RANK_INT8;

                case TypeCode.Int32:
                    return RANK_INT32;

                case TypeCode.Int64:
                    return RANK_INT64;

                case TypeCode.Double:
                    return RANK_DOUBLE;

                case TypeCode.String:
                    var len = (value as String).Length;
                    
                    if(len <= 32)
                        return RANK_TEXT5;

                    if(len <= 255)
                        return RANK_TEXT8;

                    if(len <= 65535)
                        return RANK_TEXT16;

                    return RANK_TEXT24;
            }

            throw new NotSupportedException();
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
            switch(rank) { 
                case RANK_INT8:
                    return "tinyint";

                case RANK_INT32:
                    return "int";

                case RANK_INT64:
                    return "bigint";

                case RANK_DOUBLE:
                    return "double";

                case RANK_TEXT5:
                    return "varchar(32)";

                case RANK_TEXT8:
                    return "varchar(255)";

                case RANK_TEXT16:
                    return "text";

                case RANK_TEXT24:
                    return "mediumtext";
            }

            throw new NotSupportedException();
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
            return "bigint not null auto_increment primary key";
        }

        public override long GetLastInsertID() {
            return Db.Cell<long>(false, "select last_insert_id()");
        }
    }

}

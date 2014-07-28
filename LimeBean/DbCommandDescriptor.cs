using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {

    struct DbCommandDescriptor : IEquatable<DbCommandDescriptor> {
        public readonly int Tag;
        public readonly string Sql;
        public readonly object[] Parameters;        

        public DbCommandDescriptor(string sql, params object[] parameters)
            : this(0, sql, parameters) {
        }

        public DbCommandDescriptor(int tag, string sql, params object[] parameters) {
            Tag = tag;
            Sql = sql;
            Parameters = parameters ?? new object[] { null };
        }

#if DEBUG
        public override string ToString() {
            var text = "[" + Tag + "] " + Sql;
            if(Parameters.Any())
                text += " with " + String.Join(", ", Parameters);
            return text;
        }
#endif

        public bool Equals(DbCommandDescriptor other) {
            return Tag == other.Tag
                && Sql == other.Sql
                && ArraysEqual(Parameters, other.Parameters);
        }


        public override bool Equals(object obj) {
            return obj is DbCommandDescriptor && Equals((DbCommandDescriptor)obj);
        }

        public override int GetHashCode() {
            var hash = CombineHashCodes(Tag, Sql.GetHashCode());

            foreach(var value in Parameters)
                hash = CombineHashCodes(hash, EqualityComparer<object>.Default.GetHashCode(value));

            return hash;
        }

        static int CombineHashCodes(int h1, int h2) {
            // from System.Web.Util.HashCodeCombiner
            return (h1 << 5) + h1 ^ h2;
        }

        static bool ArraysEqual<T>(T[] x, T[] y) {
            if(ReferenceEquals(x, y))
                return true;

            if(ReferenceEquals(x, null) || ReferenceEquals(y, null))
                return false;

            return x.SequenceEqual(y);
        }

    }

}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LimeBean {
    using Row = IDictionary<string, IConvertible>;

    interface IDatabaseAccess : ITransactionSupport {
        event Action<IDbCommand> QueryExecuting;
        int CacheCapacity { get; set; }        

        int Exec(string sql, params object[] parameters);

        IEnumerable<T> ColIterator<T>(string sql, params object[] parameters) where T : IConvertible;
        IEnumerable<Row> RowsIterator(string sql, params object[] parameters);

        T Cell<T>(string sql, params object[] parameters) where T : IConvertible;
        T Cell<T>(bool useCache, string sql, params object[] parameters) where T : IConvertible;

        T[] Col<T>(string sql, params object[] parameters) where T : IConvertible;
        T[] Col<T>(bool useCache, string sql, params object[] parameters) where T : IConvertible;

        Row Row(string sql, params object[] parameters);
        Row Row(bool useCache, string sql, params object[] parameters);

        Row[] Rows(string sql, params object[] parameters);
        Row[] Rows(bool useCache, string sql, params object[] parameters);
    }

}

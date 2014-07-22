using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {

    class DatabaseBeanFinder : IBeanFinder {
        IDatabaseSpecifics _specifics;
        IDatabaseAccess _db;
        IBeanCrud _crud;

        public DatabaseBeanFinder(IDatabaseSpecifics specifics, IDatabaseAccess db, IBeanCrud crud) {
            _specifics = specifics;
            _db = db;
            _crud = crud;
        }        

        // Find

        public Bean[] Find(bool useCache, string kind, string expr = null, params object[] parameters) {
            return Rows(useCache, kind, expr, parameters).Select(row => _crud.Load(kind, row)).ToArray();
        }

        public T[] Find<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new() {
            return Rows(useCache, Bean.GetKind<T>(), expr, parameters).Select(_crud.Load<T>).ToArray();
        }

        // FindOne

        public Bean FindOne(bool useCache, string kind, string expr = null, params object[] parameters) {
            return _crud.Load(kind, Row(useCache, kind, expr, parameters));
        }

        public T FindOne<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new() {
            return _crud.Load<T>(Row(useCache, Bean.GetKind<T>(), expr, parameters));
        }

        // Iterators

        public IEnumerable<Bean> FindIterator(string kind, string expr = null, params object[] parameters) {
            return RowsIterator(kind, expr, parameters).Select(row => _crud.Load(kind, row));
        }

        public IEnumerable<T> FindIterator<T>(string expr = null, params object[] parameters) where T : Bean, new() {
            return RowsIterator(Bean.GetKind<T>(), expr, parameters).Select(_crud.Load<T>);
        }

        // Count

        public long Count(bool useCache, string kind, string expr = null, params object[] parameters) {
            return _db.Cell<long>(useCache, FormatSelectQuery(kind, expr, true), parameters);
        }

        public long Count<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new() {
            return Count(useCache, Bean.GetKind<T>(), expr, parameters);
        }


        // Internals

        IDictionary<string, IConvertible> Row(bool useCache, string kind, string expr, object[] parameters) {
            return _db.Row(useCache, FormatSelectQuery(kind, expr), parameters);
        }

        IDictionary<string, IConvertible>[] Rows(bool useCache, string kind, string expr, object[] parameters) {
            return _db.Rows(useCache, FormatSelectQuery(kind, expr), parameters);
        }

        IEnumerable<IDictionary<string, IConvertible>> RowsIterator(string kind, string expr, object[] parameters) {
            return _db.RowsIterator(FormatSelectQuery(kind, expr), parameters);
        }

        string FormatSelectQuery(string kind, string expr, bool countOnly = false) {
            var sql = "select " + (countOnly ? "count(*)" : "*") + " from " + _specifics.QuoteName(kind);
            if(!String.IsNullOrEmpty(expr))
                sql += " " + expr;
            return sql;
        }
    }

}

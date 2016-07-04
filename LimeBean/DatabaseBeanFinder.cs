using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LimeBean.Interfaces;

namespace LimeBean {

    class DatabaseBeanFinder : IBeanFinder {
        IDatabaseDetails _details;
        IDatabaseAccess _db;
        IBeanCrud _crud;

        public DatabaseBeanFinder(IDatabaseDetails details, IDatabaseAccess db, IBeanCrud crud) {
            _details = details;
            _db = db;
            _crud = crud;
        }

        // Find

        public Bean[] Find(bool useCache, string kind, string expr = null, params object[] parameters) {
            return Rows(useCache, kind, expr, parameters).Select(row => _crud.RowToBean(kind, row)).ToArray();
        }

        public T[] Find<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new() {
            return Rows(useCache, Bean.GetKind<T>(), expr, parameters).Select(_crud.RowToBean<T>).ToArray();
        }

        // FindOne

        public Bean FindOne(bool useCache, string kind, string expr = null, params object[] parameters) {
            return _crud.RowToBean(kind, Row(useCache, kind, expr, parameters));
        }

        public T FindOne<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new() {
            return _crud.RowToBean<T>(Row(useCache, Bean.GetKind<T>(), expr, parameters));
        }

        // Iterators

        public IEnumerable<Bean> FindIterator(string kind, string expr = null, params object[] parameters) {
            return RowsIterator(kind, expr, parameters).Select(row => _crud.RowToBean(kind, row));
        }

        public IEnumerable<T> FindIterator<T>(string expr = null, params object[] parameters) where T : Bean, new() {
            return RowsIterator(Bean.GetKind<T>(), expr, parameters).Select(_crud.RowToBean<T>);
        }

        // Count

        public long Count(bool useCache, string kind, string expr = null, params object[] parameters) {
            return _db.Cell<long>(useCache, FormatSelectQuery(kind, expr, true), parameters);
        }

        public long Count<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new() {
            return Count(useCache, Bean.GetKind<T>(), expr, parameters);
        }


        // Internals

        IDictionary<string, object> Row(bool useCache, string kind, string expr, object[] parameters) {
            return _db.Row(useCache, FormatSelectQuery(kind, expr), parameters);
        }

        IDictionary<string, object>[] Rows(bool useCache, string kind, string expr, object[] parameters) {
            return _db.Rows(useCache, FormatSelectQuery(kind, expr), parameters);
        }

        IEnumerable<IDictionary<string, object>> RowsIterator(string kind, string expr, object[] parameters) {
            return _db.RowsIterator(FormatSelectQuery(kind, expr), parameters);
        }

        string FormatSelectQuery(string kind, string expr, bool countOnly = false) {
            var sql = "select " + (countOnly ? "count(*)" : "*") + " from " + _details.QuoteName(kind);
            if(!String.IsNullOrEmpty(expr))
                sql += " " + expr;
            return sql;
        }
    }

}

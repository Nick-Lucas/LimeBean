using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {

    interface IBeanFinder {
        Bean[] Find(string kind, string expr = null, params object[] parameters);
        Bean[] Find(bool useCache, string kind, string expr = null, params object[] parameters);
        T[] Find<T>(string expr = null, params object[] parameters) where T : Bean, new();
        T[] Find<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new();

        Bean FindOne(string kind, string expr = null, params object[] parameters);
        Bean FindOne(bool useCache, string kind, string expr = null, params object[] parameters);
        T FindOne<T>(string expr = null, params object[] parameters) where T : Bean, new();
        T FindOne<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new();

        IEnumerable<Bean> FindIterator(string kind, string expr = null, params object[] parameters);
        IEnumerable<T> FindIterator<T>(string expr = null, params object[] parameters) where T : Bean, new();

        long Count(string kind, string expr = null, params object[] parameters);
        long Count(bool useCache, string kind, string expr = null, params object[] parameters);
        long Count<T>(string expr = null, params object[] parameters) where T : Bean, new();
        long Count<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new();
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean.Interfaces {

    public interface IBeanFinder {
        Bean[] Find(bool useCache, string kind, string expr = null, params object[] parameters);
        T[] Find<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new();

        Bean FindOne(bool useCache, string kind, string expr = null, params object[] parameters);
        T FindOne<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new();

        IEnumerable<Bean> FindIterator(string kind, string expr = null, params object[] parameters);
        IEnumerable<T> FindIterator<T>(string expr = null, params object[] parameters) where T : Bean, new();

        long Count(bool useCache, string kind, string expr = null, params object[] parameters);
        long Count<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new();
    }

}

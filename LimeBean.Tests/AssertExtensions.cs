using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Xunit.Sdk;

namespace LimeBean.Tests {

    static class AssertExtensions {

        public static void Equivalent<T>(IEnumerable<T> expected, IEnumerable<T> actual) {
            if(!new HashSet<T>(actual).SetEquals(expected))
                throw new EqualException(expected, actual);
        }

        public static void WithCulture(string culture, Action action) {
            var backup = CultureInfo.CurrentCulture;
            SetThreadCulture(new CultureInfo(culture));
            try {
                action();
            } finally {
                SetThreadCulture(backup);
            }
        }

        static void SetThreadCulture(CultureInfo value) {
#if DNXCORE50
            CultureInfo.CurrentCulture = value;
#elif WINDOWS_PHONE_APP || WINDOWS_APP
            // No reliable way to change culture in runtime.
            // Instead, default project language is set to "it".
#else
            Thread.CurrentThread.CurrentCulture = value; 
#endif
        }

    }

}

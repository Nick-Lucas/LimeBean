using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Web;

namespace LimeBean.Tests.Examples.ApsNet {

	// This is your Global.asax file
    public class Global : HttpApplication {

		// For each request you use a separate instance of BeanApi
		// (object construction is very lightweight, initialization is lazy: on first usage)

        static readonly string RKey = new Guid().ToString();

        public static BeanApi R {
            get { return (BeanApi)HttpContext.Current.Items[RKey]; }
            set { HttpContext.Current.Items[RKey] = value; }
        }
				
        protected void Application_BeginRequest(object sender, EventArgs e) {
            R = new BeanApi("data source=c:/db1; pooling=true", SQLiteFactory.Instance);
        }

        protected void Application_EndRequest(object sender, EventArgs e) {
            R.Dispose();
        }

    }

    // You may use this auxiliary subclass for shorthand access to BeanApi from the inside
    abstract class WebBean : Bean {
        public WebBean(string kind)
            : base(kind) {
        }

        protected static BeanApi R {
            get { return Global.R; }
        }
    }

}

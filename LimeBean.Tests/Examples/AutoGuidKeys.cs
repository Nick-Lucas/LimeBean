using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace LimeBean.Tests.Examples {

    public class AutoGuidKeys {
        
        public void Scenario() {
            using(var api = new BeanApi("data source=:memory:", SQLiteFactory.Instance)) {
                api.EnterFluidMode();
                api.Key(false);
                api.AddObserver(new GuidKeyObserver());

                var bean = api.Dispense("foo");
                var key = api.Store(bean);
                Console.WriteLine("Key is: " + key);
            }
        }


        class GuidKeyObserver : BeanObserver {
            public override void BeforeStore(Bean bean) {
                if(bean[Bean.ID_PROP_NAME] == null)
                    bean[Bean.ID_PROP_NAME] = GenerateGuid();
            }

            static string GenerateGuid() {
                // NOTE 
                // Guid primary keys can be more complicated than you expect
                // See http://codeproject.com/articles/388157/

                return Guid.NewGuid().ToString();
            }
        }
    }

}

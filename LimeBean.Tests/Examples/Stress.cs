using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace LimeBean.Tests.Examples {

    public class Stress : IDisposable {
        const string DB_PATH = "c:/temp/lime-stress.db";
        BeanApi R;
        Random _rand = new Random();

        public Stress() {
            R = new BeanApi("data source=" + DB_PATH, SQLiteFactory.Instance);            
        }

        public void Dispose() {
            R.Dispose();
        }


        public void Fill() {
            R.Exec("drop table if exists foo");
            R.EnterFluidMode();

            R.Transaction(delegate() {
                for(var i = 0; i < 10 * 1000; i++) {
                    var bean = R.Dispense("foo");
                    for(var c = 0; c < 12; c++)
                        bean["c" + c] = GenerateLongString();

                    R.Store(bean);
                }
            });
        }

        public void IteratorRead() {
            foreach(var bean in R.FindIterator("foo")) {
                var id = bean[Bean.ID_PROP_NAME];
                // Console.WriteLine(id);
            }        
        }


        string GenerateLongString() {
            var builder = new StringBuilder();
            var n = _rand.Next(1, 255);
            for(var i = 0; i < n; i++)
                builder.Append(Guid.NewGuid().ToString("N"));
            return builder.ToString();
        }

    }

}

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace LimeBean.Tests.Examples {

    public class Crud {

        public void Scenario() {
            // Based on https://code.google.com/p/orange-bean/#CRUD

            // Tell about your database
            var R = new BeanApi("data source=:memory:", SQLiteFactory.Instance);

            // Enable automatic schema update
            R.EnterFluidMode();

            // create a bean 
            var bean = R.Dispense("person");

            // it's of kind "person"
            Console.WriteLine(bean.GetKind());

            // fill it
            bean["name"] = "Alex";
            bean["year"] = 1984;
            bean["smart"] = true;

            // store it
            var id = R.Store(bean);

            // Database schema will be updated automatically for you

            // Now the bean has an id
            Console.WriteLine(bean["id"]);

            // load a bean
            bean = R.Load("person", id);

            // change it
            bean["name"] = "Lexa";
            bean["new_prop"] = 123;

            // commit changes
            R.Store(bean);

            // or delete it
            R.Trash(bean);

            // close the connection
            R.Dispose();                
        }

    }

}

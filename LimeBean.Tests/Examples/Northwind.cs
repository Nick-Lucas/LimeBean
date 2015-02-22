using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace LimeBean.Tests.Examples {

    [TestFixture, Explicit]
    public class Northwind {
        // NOTE in web apps BeanApi must have per-request life-time
        // For example, in ASP.NET it should live in HttpContext.Items
        public static BeanApi R { get; set; }

        [SetUp]
        public void SetUp() {
            R = new BeanApi("data source=:memory:", SQLiteFactory.Instance);
            R.Key<Category>("CategoryID");
            R.Key<Product>("ProductID");            
            R.EnterFluidMode();
        }

        [TearDown]
        public void TearDown() {
            R.Dispose();
        }

        class Category : Bean {

            public Category()
                : base("Category") {
            }

            // Typed property accessors

            public int? CategoryID {
                get { return GetNullable<int>("CategoryID"); }
                set { Put("CategoryID", value); }
            }

            public string CategoryName {
                get { return Get<string>("CategoryName"); }
                set { Put("CategoryName", value); }
            }

            public string Description {
                get { return Get<string>("Description"); }
                set { Put("Description", value); }
            }

            bool HasName { get { return !String.IsNullOrWhiteSpace(CategoryName); } }

            // Helper method to find all products in this category
            // NOTE internal LRU cache is used, so DB is not hit every time
            public Product[] GetProducts() {
                return R.Find<Product>("where CategoryID = ?", CategoryID);
            }

            // Validation rules prevent storing of unnamed categories
            protected internal override void BeforeStore() {
                if(!HasName)
                    throw new Exception("Category name cannot be empty");
            }

            // Cascading delete of all its products 
            // NOTE deletion is wrapped in an implicit transaction
            // so that consistency is maintained
            protected internal override void BeforeTrash() {
                foreach(var p in GetProducts())
                    R.Trash(p);
            }

            public override string ToString() {
                if(HasName)
                    return CategoryName;

                return base.ToString();
            }
        }

        class Product : Bean {

            public Product()
                : base("Product") {
            }

            // Typed accessors

            public int? ProductID {
                get { return GetNullable<int>("ProductID"); }
                set { Put("ProductID", value); }
            }

            public string Name {
                get { return Get<string>("Name"); }
                set { Put("Name", value); }
            }

            public int? CategoryID {
                get { return GetNullable<int>("CategoryID"); }
                set { Put("CategoryID", value); }
            }

            public decimal UnitPrice {
                get { return Get<decimal>("UnitPrice"); }
                set { Put("UnitPrice", value); }
            }

            public bool Discontinued {
                get { return Get<bool>("Discontinued"); }
                set { Put("Discontinued", value); }
            }

            // Example of a referenced bean
            // NOTE internal LRU cache is used during loading
            public Category Category {
                get { return R.Load<Category>(CategoryID); }
                set { CategoryID = value.CategoryID; }
            }

            // A set of validation checks
            protected internal override void BeforeStore() {
                if(String.IsNullOrWhiteSpace(Name))
                    throw new Exception("Product name cannot be empty");

                if(UnitPrice <= 0)
                    throw new Exception("Price must be a non-negative number");

                if(Category == null)
                    throw new Exception("Product must belong to an existing category");
            }
        }


        [Test]
        public void Scenario() {
            var beverages = R.Dispense<Category>();
            beverages.CategoryName = "Beverages";
            beverages.Description = "Soft drinks, coffees, teas, beers, and ales";

            var condiments = R.Dispense<Category>();
            condiments.CategoryName = "Condiments";
            condiments.Description = "Sweet and savory sauces, relishes, spreads, and seasonings";

            R.Store(beverages);
            R.Store(condiments);


            var chai = R.Dispense<Product>();
            chai.Name = "Chai";
            chai.UnitPrice = 18;
            chai.Category = beverages;

            var chang = R.Dispense<Product>();
            chang.Name = "Chang";
            chang.UnitPrice = 19;
            chang.Category = beverages;

            var syrup = R.Dispense<Product>();
            syrup.Name = "Aniseed Syrup";
            syrup.UnitPrice = 9.95M;
            syrup.Category = condiments;
            syrup.Discontinued = true;

            R.Store(chai);
            R.Store(chang);
            R.Store(syrup);

            Console.WriteLine("Number of known beverages: {0}", beverages.GetProducts().Length);

            Console.WriteLine("Deleting all the beverages...");
            R.Trash(beverages);

            Console.WriteLine("Products remained: {0}", R.Count<Product>());
        }

    }

}

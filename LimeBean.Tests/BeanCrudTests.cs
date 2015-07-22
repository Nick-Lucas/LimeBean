using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace LimeBean.Tests {

    public class BeanCrudTests {

        [Fact]
        public void Dispense_Default() {
            var crud = new BeanCrud(null, null, null);
            var bean = crud.Dispense("test");
            Assert.Equal("test", bean.GetKind());
            Assert.Equal(typeof(Bean), bean.GetType());
        }

        [Fact]
        public void Dispense_Hooks() {
            var crud = new BeanCrud(null, null, null);
            var observer = new TracingObserver();
            crud.AddObserver(observer);

            var bean = crud.Dispense<Tracer>();
            Assert.Equal("tracer", bean.GetKind());

            Assert.Equal("ad:", bean.TraceLog);
            Assert.Equal("ad:", observer.TraceLog);
            Assert.Same(bean, observer.LastBean);
        }

        [Fact]
        public void Store() {
            var crud = new BeanCrud(new InMemoryStorage(), null, new KeyUtil());
            var observer = new TracingObserver();
            crud.AddObserver(observer);

            var bean = crud.Dispense<Tracer>();

            var id = crud.Store(bean);
            Assert.Equal(0L, id);
            Assert.Equal(0L, bean[Bean.ID_PROP_NAME]);
            Assert.Equal("ad: bs: as:" + id, bean.TraceLog);
            Assert.Equal("ad: bs: as:" + id, observer.TraceLog);
        }


        [Fact]
        public void Load() {
            var crud = new BeanCrud(new InMemoryStorage(), null, new KeyUtil());
            var observer = new TracingObserver();
            crud.AddObserver(observer);

            // Load non-existing bean
            Assert.Null(crud.Load("any", 123));
            Assert.Empty(observer.TraceLog);

            var bean = crud.Dispense<Tracer>();
            bean.Put("p1", "test");

            var id = crud.Store(bean);
            observer.TraceLog = "";

            bean = crud.Load<Tracer>(id);
            Assert.Equal("ad: bl: al:" + id, bean.TraceLog);
            Assert.Equal("ad: bl: al:" + id, observer.TraceLog);
            Assert.NotNull(bean[Bean.ID_PROP_NAME]);
            Assert.Equal(id, bean[Bean.ID_PROP_NAME]);
            Assert.Equal("test", bean["p1"]);
        }

        [Fact]
        public void Trash() {
            var crud = new BeanCrud(new InMemoryStorage(), null, new KeyUtil());
            var observer = new TracingObserver();
            crud.AddObserver(observer);

            var bean = crud.Dispense<Tracer>();

            observer.TraceLog = bean.TraceLog = "";
            crud.Trash(bean);
            Assert.Empty(bean.TraceLog);
            Assert.Empty(observer.TraceLog);

            var id = crud.Store(bean);

            observer.TraceLog = bean.TraceLog = "";
            crud.Trash(bean);
            Assert.Equal("bt:" + id + " at:" + id, bean.TraceLog);
            Assert.Equal("bt:" + id + " at:" + id, observer.TraceLog);
            Assert.Equal(id, bean[Bean.ID_PROP_NAME]);

            Assert.Null(crud.Load<Tracer>(id));
        }

        [Fact]
        public void RowToBean() {
            var crud = new BeanCrud(new InMemoryStorage(), null, null);
            var observer = new TracingObserver();
            crud.AddObserver(observer);

            var bean = crud.RowToBean<Tracer>(new Dictionary<string, IConvertible> { 
                { "s", "hello" }
            });

            Assert.Null(bean[Bean.ID_PROP_NAME]);
            Assert.Equal("hello", bean["s"]);
            Assert.Equal("ad: bl: al:", bean.TraceLog);
            Assert.Equal("ad: bl: al:", observer.TraceLog);

            observer.TraceLog = "";

            bean = crud.RowToBean<Tracer>(new Dictionary<string, IConvertible> { 
                { Bean.ID_PROP_NAME, 123 },
                { "s", "see you" }
            });            

            Assert.Equal(123, bean[Bean.ID_PROP_NAME]);
            Assert.Equal("see you", bean["s"]);
            Assert.Equal("ad: bl: al:123", bean.TraceLog);
            Assert.Equal("ad: bl: al:123", observer.TraceLog);

            Assert.Null(crud.Load("temp",  (IConvertible)null));
        }

        [Fact]
        public void PreventDirectInstantiation() {
            var crud = new BeanCrud(null, null, null);
            
            Assert.Throws<InvalidOperationException>(delegate() {
                crud.Store(new Tracer());    
            });

            Assert.Throws<InvalidOperationException>(delegate() {
                crud.Trash(new Tracer());
            });
        }


        class Tracer : Bean {

            public Tracer()
                : base("tracer") {
            }

            public string TraceLog = "";

            void Trace(string subject) {
                if(TraceLog.Length > 0)
                    TraceLog += " ";
                TraceLog += subject + ":" + this[Bean.ID_PROP_NAME];
            }

            protected internal override void AfterDispense() {
                Trace("ad");
            }

            protected internal override void BeforeLoad() {
                Trace("bl");
            }

            protected internal override void AfterLoad() {
                Trace("al");
            }

            protected internal override void BeforeStore() {
                Trace("bs");
            }

            protected internal override void AfterStore() {
                Trace("as");
            }

            protected internal override void BeforeTrash() {
                Trace("bt");
            }

            protected internal override void AfterTrash() {
                Trace("at");
            }
        
        }
    }

}

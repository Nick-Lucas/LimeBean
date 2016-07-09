using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

using LimeBean.Interfaces;

namespace LimeBean.Tests {

    public class BeanCrudTests {

        [Fact]
        public void Dispense_Default() {
            IBeanFactory factory = new BeanFactory();
            var crud = new BeanCrud(null, null, null, factory);
            var bean = crud.Dispense("test");
            Assert.Equal("test", bean.GetKind());
            Assert.Equal(typeof(Bean), bean.GetType());
        }

        [Fact]
        public void Dispense_Hooks() {
            IBeanFactory factory = new BeanFactory();
            factory.Config.ValidateGetColumns = false;
            var crud = new BeanCrud(null, null, null, factory);
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
            IBeanFactory factory = new BeanFactory();
            factory.Config.ValidateGetColumns = false;
            var crud = new BeanCrud(new InMemoryStorage(), null, new KeyUtil(), factory);
            var observer = new TracingObserver();
            crud.AddObserver(observer);

            var bean = crud.Dispense<Tracer>();

            var id = crud.Store(bean);
            Assert.Equal(0L, id);
            Assert.Equal(0L, bean["id"]);
            Assert.Equal("ad: bs: as:" + id, bean.TraceLog);
            Assert.Equal("ad: bs: as:" + id, observer.TraceLog);
        }


        [Fact]
        public void Load() {
            IBeanFactory factory = new BeanFactory();
            factory.Config.ValidateGetColumns = false;
            var crud = new BeanCrud(new InMemoryStorage(), null, new KeyUtil(), factory);
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
            Assert.NotNull(bean["id"]);
            Assert.Equal(id, bean["id"]);
            Assert.Equal("test", bean["p1"]);
        }

        [Fact]
        public void Trash() {
            IBeanFactory factory = new BeanFactory();
            factory.Config.ValidateGetColumns = false;
            var crud = new BeanCrud(new InMemoryStorage(), null, new KeyUtil(), factory);
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
            Assert.Equal(id, bean["id"]);

            Assert.Null(crud.Load<Tracer>(id));
        }

        [Fact]
        public void RowToBean() {
            IBeanFactory factory = new BeanFactory();
            factory.Config.ValidateGetColumns = false;
            var crud = new BeanCrud(new InMemoryStorage(), null, null, factory);
            var observer = new TracingObserver();
            crud.AddObserver(observer);

            var bean = crud.RowToBean<Tracer>(new Dictionary<string, object> { 
                { "s", "hello" }
            });

            Assert.Null(bean["id"]);
            Assert.Equal("hello", bean["s"]);
            Assert.Equal("ad: bl: al:", bean.TraceLog);
            Assert.Equal("ad: bl: al:", observer.TraceLog);

            observer.TraceLog = "";

            bean = crud.RowToBean<Tracer>(new Dictionary<string, object> { 
                { "id", 123 },
                { "s", "see you" }
            });

            Assert.Equal(123, bean["id"]);
            Assert.Equal("see you", bean["s"]);
            Assert.Equal("ad: bl: al:123", bean.TraceLog);
            Assert.Equal("ad: bl: al:123", observer.TraceLog);

            Assert.Null(crud.Load("temp", null));
        }

        [Fact]
        public void PreventDirectInstantiation() {
            IBeanFactory factory = new BeanFactory();
            var crud = new BeanCrud(null, null, null, factory);
            
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
                TraceLog += subject + ":" + this["id"];
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

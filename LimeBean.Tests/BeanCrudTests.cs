using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {

    [TestFixture]
    public class BeanCrudTests {

        [Test]
        public void Dispense_Default() {
            var crud = new BeanCrud(null, null);
            var bean = crud.Dispense("test");
            Assert.AreEqual("test", bean.GetKind());
            Assert.AreEqual(typeof(Bean), bean.GetType());
        }

        [Test]
        public void Dispense_Hooks() {
            var crud = new BeanCrud(null, null);
            var observer = new TracingObserver();
            crud.AddObserver(observer);

            var bean = crud.Dispense<Tracer>();
            Assert.AreEqual("tracer", bean.GetKind());

            Assert.AreEqual("ad:", bean.TraceLog);
            Assert.AreEqual("ad:", observer.TraceLog);
            Assert.AreSame(bean, observer.LastBean);
        }

        [Test]
        public void Store() {
            var crud = new BeanCrud(new InMemoryStorage(), null);
            var observer = new TracingObserver();
            crud.AddObserver(observer);

            var bean = crud.Dispense<Tracer>();

            var id = crud.Store(bean);
            Assert.AreEqual(0, id);
            Assert.AreEqual(0, bean.ID);
            Assert.AreEqual("ad: bs: as:" + id, bean.TraceLog);
            Assert.AreEqual("ad: bs: as:" + id, observer.TraceLog);
        }


        [Test]
        public void Load() {
            var crud = new BeanCrud(new InMemoryStorage(), null);
            var observer = new TracingObserver();
            crud.AddObserver(observer);

            // Load non-existing bean
            Assert.IsNull(crud.Load("any", 123));
            Assert.IsEmpty(observer.TraceLog);

            var bean = crud.Dispense<Tracer>();
            bean.Put("p1", "test");

            var id = crud.Store(bean);
            observer.TraceLog = "";

            bean = crud.Load<Tracer>(id);
            Assert.AreEqual("ad: bl: al:" + id, bean.TraceLog);
            Assert.AreEqual("ad: bl: al:" + id, observer.TraceLog);
            Assert.IsNotNull(bean.ID);
            Assert.AreEqual(id, bean.ID);
            Assert.AreEqual("test", bean["p1"]);
        }

        [Test]
        public void Trash() {
            var crud = new BeanCrud(new InMemoryStorage(), null);
            var observer = new TracingObserver();
            crud.AddObserver(observer);

            var bean = crud.Dispense<Tracer>();

            observer.TraceLog = bean.TraceLog = "";
            crud.Trash(bean);
            Assert.IsEmpty(bean.TraceLog);
            Assert.IsEmpty(observer.TraceLog);

            var id = crud.Store(bean);

            observer.TraceLog = bean.TraceLog = "";
            crud.Trash(bean);
            Assert.AreEqual("bt:" + id + " at:" + id, bean.TraceLog);
            Assert.AreEqual("bt:" + id + " at:" + id, observer.TraceLog);
            Assert.AreEqual(id, bean.ID);

            Assert.IsNull(crud.Load<Tracer>(id));
        }

        [Test]
        public void LoadFromDict() {
            var crud = new BeanCrud(new InMemoryStorage(), null);
            var observer = new TracingObserver();
            crud.AddObserver(observer);

            var bean = crud.Load<Tracer>(new Dictionary<string, IConvertible> { 
                { "s", "hello" }
            });

            Assert.IsNull(bean.ID);
            Assert.AreEqual("hello", bean["s"]);
            Assert.AreEqual("ad: bl: al:", bean.TraceLog);
            Assert.AreEqual("ad: bl: al:", observer.TraceLog);

            observer.TraceLog = "";

            bean = crud.Load<Tracer>(new Dictionary<string, IConvertible> { 
                { Bean.ID_PROP_NAME, "123" },
                { "s", "see you" }
            });            

            Assert.AreEqual(123, bean.ID);
            Assert.AreEqual("see you", bean["s"]);
            Assert.AreEqual("ad: bl: al:123", bean.TraceLog);
            Assert.AreEqual("ad: bl: al:123", observer.TraceLog);

            Assert.IsNull(crud.Load("temp", null));
        }


        class Tracer : Bean {

            public Tracer()
                : base("tracer") {
            }

            public string TraceLog = "";

            void Trace(string subject) {
                if(TraceLog.Length > 0)
                    TraceLog += " ";
                TraceLog += subject + ":" + ID;
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IStruct {
    class Program {
        static List<MyStruct> structs = new List<MyStruct>();
        static List<IMyInterface> interfaces = new List<IMyInterface>();

        static void Main(string[] args) {
            var ls = new MyStruct();
            
            structs.Add(ls);
            structs[0].increment();
            
            interfaces.Add(ls);
            interfaces[0].increment();
            var st = new MyStruct();
            var ob = new MyObject();
            var ii = st as IMyInterface;

            var sc = st.copy();
            var oc = ob.copy();
            var ic = ii.copy();

            doInc(st);      // st stays at 0
            doInc(ob);      // ob goes to 1
            doInc(ii);      // ii goes to 1

            doInc(sc);      // sc goes to 1
            doInc(oc);      // oc goes to 2
            doInc(ic);      // ic goes to 1

            st.increment(); // st goes to 1
            return;
        }

        static void doIncRef(ref MyStruct s) {
            s.increment();
        }

        static void doInc(IMyInterface i) {
            i.increment();
        }
    }
    interface IMyInterface { 
        void increment();
        IMyInterface copy();
    }
    class MyObject: IMyInterface {
        int value;
        public void increment() => value++;
        public IMyInterface copy() => this;
    }
    struct MyStruct : IMyInterface {
        int value;
        public void increment() {
            this.value++;
        }
        public IMyInterface copy() => this;
    }
}

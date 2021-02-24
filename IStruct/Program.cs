using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IStruct {
    class Program {
        static void Main(string[] args) {
            var st = new MyStruct();
            var ob = new MyObject();

            doInc(st);// st stays at 0
            doInc(ob);// ob goes to 1
            st.increment();// st goes to 1
            return;
        }

        static void doInc(IMyInterface i) {
            i.increment();
        }
    }
    interface IMyInterface { void increment(); }
    class MyObject: IMyInterface {
        int value;
        public void increment() => value++;
    }
    struct MyStruct : IMyInterface {
        int value;
        public void increment() {
            value++;
        }
    }
}

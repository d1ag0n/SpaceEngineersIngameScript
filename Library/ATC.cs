using System;
using System.Collections.Generic;
using System.Text;

namespace IngameScript
{
    class ATC
    {
        readonly BoxMap map = new BoxMap();

        public ATCMsg processMessage(ATCMsg msg) {
            switch (msg.msg) {
                case ATCMsgs.DropReservtion:
            }
        }
    }
    struct ATCMsg {
        public ATCMsgs msg;
        public long sender;
        public BoxInfo box;
    }
    enum ATCMsgs
    {
        Info,
        Reserve,
        DropReservtion
    }
}

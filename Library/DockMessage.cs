using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRageMath;

namespace IngameScript
{
    class DockMessage
    {
        public long DockId {
            get; private set;
        }
        public string Command {
            get; private set;
        }
        public Vector3D Position {
            get; private set;
        }
        public DockMessage(long aDockId, string aCommand, Vector3D aPosition) {
            DockId = aDockId;
            Command = aCommand;
            Position = aPosition;
        }
        public MyTuple<long, string, Vector3D> Data() => new MyTuple<long, string, Vector3D>(DockId, Command, Position);
    }
}

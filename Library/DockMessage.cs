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
        public DockMessage(object aData) {
            var data = (MyTuple<long, string, Vector3D>)aData;
            DockId = data.Item1;
            Command = data.Item2;
            Position = data.Item3;
        }
        public DockMessage(long aDockId, string aCommand, Vector3D aPosition) {
            DockId = aDockId;
            Command = aCommand;
            Position = aPosition;
        }
        public MyTuple<long, string, Vector3D> Data() => new MyTuple<long, string, Vector3D>(DockId, Command, Position);
    }
}

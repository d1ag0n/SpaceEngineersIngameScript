using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    public delegate void SaveHandler(Serialize s);
    public delegate void LoadHandler(Serialize s, string aData);
    public delegate List<MenuItem> PaginationHandler(int page);
    public delegate Vector3D VectorHandler();
    public delegate void IGCHandler(MyIGCMessage m);
}
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    public delegate void SaveHandler(Serialize s);
    public delegate void LoadHandler(Serialize s, string aData);
    public delegate List<MenuItem> PaginationHandler(int page);
    delegate Vector3D VectorHandler();
    public delegate void IGCHandler(IGC.Envelope m);
    delegate void BoxInfoHandler(BoxInfo b);
    public delegate void UpdateHandler(double time);
}

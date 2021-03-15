using System.Collections.Generic;

namespace IngameScript {
    public delegate void SaveHandler(Serialize s);
    public delegate void LoadHandler(Serialize s, string aData);
    public delegate List<MenuItem> PaginationHandler(int page); 
}
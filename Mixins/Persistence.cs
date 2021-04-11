using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using System.Xml.Serialization;

namespace IngameScript {
    public abstract class Persistence {
        public string Key { get; protected set; }
        public abstract void Save(Serialize s);
        public abstract void Load(Serialize s, string data);
    }
}
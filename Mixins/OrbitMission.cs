using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
namespace IngameScript {
    public  class OrbitMission : MissionBase {
        public OrbitMission(ShipControllerModule aController, BoundingSphereD aSphere) : base(aController, aSphere) {
            
        }
    }
}

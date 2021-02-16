using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    class Mission
    {
        public Connector Connector;

        public Details Detail;
        public double Altitude;
        public double Distance;

        public int Step;
        public int SubStep;
        
        public Vector3D Start;
        public Vector3D Objective;
        public Vector3D Translation;
        public Vector3D Direction;

        public enum Details
        {
            damp,
            navigate,
            dock,
            patrol,
            test,
            thrust,
            rotate,
            map,
            scan,
            calibrate,
            follow,
            none
        }

        
    }
}

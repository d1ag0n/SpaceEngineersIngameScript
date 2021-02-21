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
        
        /// <summary>
        /// where the mission started
        /// </summary>
        public Vector3D Start;
        /// <summary>
        /// currently flying to this position
        /// </summary>
        public Vector3D Objective;
        /// <summary>
        /// where the mission stops
        /// </summary>
        public Vector3D Translation;
        /// <summary>
        /// arbitrary direction valie
        /// </summary>
        public Vector3D PendingDirection;
        /// <summary>
        /// arbitrary 
        /// </summary>
        public Vector3D PendingPosition;

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
            boxnav,
            none
        }

        
    }
}

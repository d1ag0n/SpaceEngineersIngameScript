using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PBExtra {
    class MyMotorStator {
        public void SetMotorEnabled(HkConstraint constraint, bool enabled) {
            HkLimitedHingeConstraintData.HkLimitedHingeConstraintData_SetMotorEnabled(this.NativeObject, constraint.NativeObject, enabled);
        }
    }
}

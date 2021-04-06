using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Text;
using VRageMath;
using VRage.Game;
using System;

namespace IngameScript {
    public delegate void SaveHandler(Serialize s);
    public delegate void LoadHandler(Serialize s, string aData);
    public class Serialize {

        // found these acsii codes in a post somewhere

        /// <summary>
        /// Use is reserved only for ModuleManager
        /// ASCII 28 (0x1C) File Separator - Used to indicate separation between files on a data input stream.
        /// use at end of mod data
        /// </summary>
        public const char MODSEP = (char)28;

        /// <summary>
        /// Use is reserved only for ModuleManager
        /// ASCII 29 (0x1D) Group Separator - Used to indicate separation between tables on a data input stream(called groups back then).
        /// use between mod name and data
        /// </summary>
        public const char GRPSEP = (char)29;

        public readonly string[] NL = new string[] { Environment.NewLine };

        /// <summary>
        /// Maybe used by modules
        /// ASCII 30 (0x1E) Record Separator - Used to indicate separation between records within a table(within a group). 
        /// </summary>
        public const char RECSEP = (char)30;

        /// <summary>
        /// Maybe used by modules
        /// ASCII 31 (0x1F) Unit Separator - Used to indicate separation between units within a record.
        /// </summary>
        public const char UNTSEP = (char)31;

        readonly StringBuilder sb = new StringBuilder();
        /*
         * # example view of stored data I'm using # to indicate a comment
         * # [] and {} will not be used, they're only for visualization
         * # newlines will not be used, unless notes, they're only for visualization
         * 
         * ModuleName[GRPSEP]{ModuleData}[MODSEP] 
         * ModuleName[GRPSEP]{ModuleData}         
         * 
         * # mod manager will request data to save from each module, they can optionally use [RECSEP] and [UNTSEP]
         * 
         * # {ModuleData} will look like
         * # dont use manager reserved separators(SANITIZE USER INPUT) there is no validation
         * # if user input is stored newlines should be replaced with an escape sequence or whatever
         * # the following format is designed to work with this class
         * ArbitraryNameOfData[UNTSEP]{Data}[RECSEP] # name may repeat
         * ArbitraryNameOfData[UNTSEP]{Data}
         * 
         * # {Data} will be one line for each data element
         */

        // todo check existing tostring mothods for newlines and replace as necessary

        public void mod() => sb.Append(MODSEP);
        public void rec() => sb.Append(RECSEP);
        public void grp(string s) {
            sb.Append(s);
            sb.Append(GRPSEP);
        }
        public void unt(string s) {
            sb.Append(s); 
            sb.Append(UNTSEP);
        }
        public string Clear() {
            var s = sb.ToString();
            sb.Clear();
            return s;
        }
        public void str(ThyDetectedEntityInfo e) {
            str(e.EntityId);
            str(e.Name);
            str(e.Type);
            if (e.HitPosition.HasValue) {
                str(e.HitPosition.Value);
            } else {
                str();
            }
            str(e.Orientation);
            str(e.Velocity);
            str(e.Relationship);
            str(e.TimeStamp);
            str(e.WorldVolume);
        }
        public ThyDetectedEntityInfo objThyDetectedEntityInfo(IEnumerator<string> e) => new ThyDetectedEntityInfo(
            objlong(e), objstring(e), objThyDetectedEntityType(e), objVector3D_(e), objMatrixD(e),
            objVector3D(e), objMyRelationsBetweenPlayerAndBlock(e), objDateTime(e), objBoundingSphereD(e)
        );

        public void str(DateTime t) => str(t.ToOADate());
        public DateTime objDateTime(IEnumerator<string> e) => DateTime.FromOADate(objdouble(e));
        public void str(MyRelationsBetweenPlayerAndBlock r) => str((int)r);
        public MyRelationsBetweenPlayerAndBlock objMyRelationsBetweenPlayerAndBlock(IEnumerator<string> e) {
            var s = e.Current;
            e.MoveNext();
            int i = int.Parse(s);
            return (MyRelationsBetweenPlayerAndBlock)i;
        }
        public void str(Vector3D v) => sb.AppendLine(v.ToString());

        public Vector3D objVector3D(IEnumerator<string> e) {
            Vector3D result;
            Vector3D.TryParse(e.Current, out result);
            e.MoveNext();
            return result;
        }
        public Vector3D? objVector3D_(IEnumerator<string> e) {
            if (e.Current == string.Empty) {
                e.MoveNext();
                return null;
            }
            return objVector3D(e);
        }

        public void str(BoundingBoxD b) {
            str(b.Min);
            str(b.Max);
        }
        public BoundingBoxD objBoundingBoxD(IEnumerator<string> e) {
            return new BoundingBoxD(objVector3D(e), objVector3D(e));
        }
        public void str(BoundingSphereD b) {
            str(b.Center);
            str(b.Radius);
        }
        public BoundingSphereD objBoundingSphereD(IEnumerator<string> e) {
            return new BoundingSphereD(objVector3D(e), objdouble(e));
        }
        public void str(long l) => sb.AppendLine(l.ToString());
        public long objlong(IEnumerator<string> e) {
            long result = objlong(e.Current);
            e.MoveNext();
            return result;
        }
        public long objlong(string s) {
            long result;
            long.TryParse(s, out result);
            return result;
        }

        public void str(double d) => sb.AppendLine(d.ToString());
        public double objdouble(IEnumerator<string> e) {
            double result;
            double.TryParse(e.Current, out result);
            e.MoveNext();
            return result;
        }

        public void str() => sb.AppendLine();
        public void str(string s) => sb.AppendLine(s);
        public string objstring(IEnumerator<string> e) {
            var result = e.Current;
            e.MoveNext();
            return result;
        }
        public void str(MatrixD m) {
            str(m.Translation);
            str(m.Forward);
            str(m.Up);
        }
        public MatrixD objMatrixD(IEnumerator<string> e) {
            return MatrixD.CreateWorld(objVector3D(e), objVector3D(e), objVector3D(e));
        }
        public void str(ThyDetectedEntityType t) => str((int)t);
        public ThyDetectedEntityType objThyDetectedEntityType(IEnumerator<string> e) {
            var s = e.Current;
            e.MoveNext();
            int i = int.Parse(s);
            return (ThyDetectedEntityType)i;
        }
    }
}

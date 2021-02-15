using IngameScript;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRageMath;

namespace commandline
{
    class Program
    {

        static void dicTest() {
            var d = new Dictionary<int, string>();
            d.Add(1, "");
            d.Add(2, "");
            d.Add(3, "");
            d.Add(4, "");
            d.Add(5, "");
            foreach (var key in d.Keys) {
                d.Remove(key);
            }
            Console.WriteLine("dicTestCOmplete");
        }
        static Random r = new Random(7);
        static Vector3D rd() {
            var x = r.NextDouble() - 0.5;
            var y = r.NextDouble() - 0.5;
            var z = r.NextDouble() - 0.5;
            return Vector3D.Normalize(new Vector3D(x, y, z));
        }
        static Vector3D rv() {
            var x = (r.NextDouble() - 0.5) * 1000000.0;
            var y = (r.NextDouble() - 0.5) * 1000000.0;
            var z = (r.NextDouble() - 0.5) * 1000000.0;
            return new Vector3D(x, y, z);
        }
        static void cindex() {
            for (int i = 0; i < 10000; i++) {
                var v = box.toVector(i);
                var index = box.toIndex(v);
                if (i != index) {
                    Console.WriteLine("index mismatch");
                }
            }
            for (int i = 0; i < 10000; i++) {
                var v = rv();
                var c = box.c(v);
                var cc = box.c(c.Center);
                if (c.Center != cc.Center) {
                    Console.WriteLine("c mismatch");
                    Console.WriteLine(c.Center);
                    Console.WriteLine(cc.Center);
                }
            }
            Console.WriteLine("cindex complete");
        }
        static void cwork() {
            for (int i = 0; i < 10000; i++) {
                var v = rv();
                var cbox = box.c(v);
                var dbox = box.c(cbox.Center);
                if (cbox.Center != dbox.Center) {
                    Console.WriteLine("mismatch");
                    Console.WriteLine(v);
                    Console.WriteLine(cbox.Center);
                    Console.WriteLine(dbox.Center);
                }
            }
            Console.WriteLine("kwork complete");
        }
        static void kwork() {
            
            for (int i = 0; i < 10000; i++) {
                var v = rv();
                var kbox = box.k(v);
                
                var dbox = box.k(kbox.Center);
                if (kbox.Center != dbox.Center) {
                    Console.WriteLine("mismatch");
                    Console.WriteLine(v);
                    Console.WriteLine(kbox.Center);
                    Console.WriteLine(dbox.Center);
                }
            }

            Console.WriteLine("kwork complete");

        }
        const int miInterval = 10;
        const double mdTickTime = 1.0 / 60.0;
        const double mdTimeFactor = mdTickTime * miInterval;
        //const double mdTimeFactor = 0.2;
        //const double mdMass = 15327;
        const double mdNewtons = 196800;
        const double mdVelocity = 1.71;
        const double mdGravity = 2.45;
        static void sphere() {

            int nx = 4;
            int ny = 5;
            for (int x = 0; x < nx; x++) {
                var lon = 360 * ((x + 0.5) / nx);
                for (int y = 0; y < ny; y++) {
                    //var midpt = (y + 0.5) / ny;
                    var lat = 180 * Math.Asin(2 * ((y + 0.5) / ny - 0.5));
                    Console.WriteLine($"{lon}, {lat}");
                }
            }
        }

        // F=MA=MG=N
        // M=F/A
        // A=F/M
        // G=A
        // F=N


        
        // calculate Force required to cancel Gravity at specific angle b= 
        static void trig1() {
            op("trig1");
            
            // given
            var B = Math.PI / 2.0; // gravity plane angle is always pi/2
            var C = 35.0 * deg; // angle between thrust vector and gravity direction is 35deg
            var G = 1.0; // gravity

            // calculate
            var A = Math.PI - B - C; // remaining angle
            var b = G * Math.Sin(B) / Math.Sin(A);// length of thrust vector to gravity plane
            
            op($"deg = {deg}");
            op($"A = {A}");
            op($"B = {B}");
            op($"C = {C}");
            op($"G = {G}");
            op($"sin(A) = {Math.Sin(A)}");
            op($"sin(B) = {Math.Sin(B)}");
            op($"b = {b}");   
        }
        
        const double deg = Math.PI / 180.0;
        const double rad = 180.0 / Math.PI;
        // orthogonal projection is vector rejection?
        static Vector3D project(Vector3D aTarget, Vector3D aPlane, Vector3D aNormal) =>
            aTarget - (Vector3D.Dot(aTarget - aPlane, aNormal) * aNormal);
        static double angleBetween(Vector3D a, Vector3D b) {
            var dot = a.Dot(b);
            if (dot < -1.0) {
                dot = -1.0;
            } else if (dot > 1.0) {
                dot = 1.0;
            }
            var result = Math.Acos(dot);
            //log("angleBetween ", result);
            return result;
        }
        // projection? (a*b/b*b)*b
        static void zcalculateThrustVector(Vector3D aTarget, Vector3D aCoM, Vector3D aGravity, Vector3D aThrustVector, double aMass, double aNewtons) {
            // get gravity direction
            var gravityDirection = aGravity;
            var gravityMagnitude = gravityDirection.Normalize();
            
            // project the target onto the plane defined by centerOfMass and gravity normal
            var targetProjection = project(aTarget, aCoM, gravityDirection);

            // get the angle between the target and it's projected position
            var dir2target = Vector3D.Normalize(aTarget - aCoM);
            var dir2targetProjection = Vector3D.Normalize(targetProjection - aCoM);
            var angle = angleBetween(dir2targetProjection, dir2target);
            
            var dot = gravityDirection.Dot(Vector3D.Normalize(aTarget - aCoM));
            // - is above here
            if (dot > 0) {
                angle = -angle;
            }

            var leanMax = maxLean(gravityMagnitude, aMass, aNewtons, angle);
            var t = thrustAtAngle(aGravity, gravityDirection, angle, aThrustVector, aMass);
            op($"thrust = {t}");
        }
        // ship says 1.19
        // static double maxLean(double aGravity, double aMass, double aNewtons) => Math.PI - Math.PI / 2.0 - Math.Asin((aGravity * aMass) * Math.Sin(Math.PI / 2.0) / aNewtons);
        static double maxLean(double aGravity, double aMass, double aNewtons, double aAngle) => Math.PI - (Math.PI / 2.0 + aAngle) - Math.Asin((aGravity * aMass) * Math.Sin(Math.PI / 2.0) / aNewtons);

        static double thrustAtAngle(Vector3D aGravity, Vector3D aGravityDirection, double aAngle, Vector3D aThrustVector, double aMass) {
            var B = Math.PI / 2.0 + aAngle; // gravity plane
            var C = angleBetween(aGravityDirection, aThrustVector);
            var G = aGravity * aMass;
            var A = Math.PI - B - C; // remaining angle
            var b = G * Math.Sin(B) / Math.Sin(A);// length of thrust vector to gravity plane
            var t = b.Length();
            return t;
        }
        // calculate maximum angle where we can still cancel gravity
        /*static void trig2() {
            op("trig2");
            
            // given
            var a = 1.0; // gravity
            var b = 2.0; // max length of thrust vector to gravity plane, maximum based on what engines are capable of
            var B = Math.PI / 2.0; // gravity plane angle is always pi/2
            op($"B = {B}");
            // calculate
            var A = Math.Asin(a * Math.Sin(B) / b);
            var C = Math.PI - B - A;
            op($"A = {A}");
            op($"C = {C * rad}");
            op($"maxLean = {maxLean(a, b) * rad}");
            op();
        }*/
        
        static void stoppingTime() {
            op("stoppingTime");
            var u = 1000.0; // initial velocity
            var a = 10.0; // acceleration
            var t = u / a;
            var vavg = u / 2.0; // average velocity

            var d = vavg * t;
            op($"t = {t}");
            op($"d = {d}");
            d = (u / 2.0) * (u / a);
            op($"d = {d}");
            d = (u * u) / (a * 2.0);
            op($"d = {d}");
        }
        
        static void velocityAwayFromDirectionOfGravity() {
            op($"Down = {Vector3D.Down}");
            op($"Forward = {Vector3D.Forward}");

            var u = new Vector3D(0, 0.5, 0.5);
            op($"u = {u}");
            var g = new Vector3D(0, 9.8, 0);

            op($"g = {g}");
            var gcross = g.Cross(-g);
            op($"gcross = {gcross}");
            var v = new Vector3D(10);
            var MatrixD = new MatrixD();
        }

        static string nl => Environment.NewLine;
        static void rejection() {
            op($"{nl}rejection");
            var g = Vector3D.Down * 7.0; // gravity
            var a = (Vector3D.Up + Vector3D.Right) / 2.0; // velocity
            a *= 3.0;
            var b = g.Cross(a); // cross gravity
            var a1 = ((a.Dot(b) / b.Dot(b)) * b);
            var a2 = a - a1;
            op($"g  = {g}");
            op($"a  = {a}");
            op($"b  = {b}");
            op($"a1 = {a1}");
            op($"a2 = {a2}");
            var ra = project(new Vector3D(1.5, 1.5, 0.0), Vector3D.Zero, Vector3D.Up);
            op($"ra = {ra}");
        }
        static void upComponent() {
            var up = Vector3D.Up;
            var velo = (Vector3D.Up + Vector3D.Right) / 2.0;
            var dot = up.Dot(Vector3D.Right);
            op($"upComponent = {dot}");
        }
        // if my altitude is 80 and I want to be at 100
        // traveling up at the same velocity as the acceleration of gravity
        // i need to stop thrusting up when I am 100 - G 
        static void op() => Console.WriteLine(nl);
        static void op<T>(T a) => Console.WriteLine(a);
        /// <summary>
        /// Rejects vector a on vector b,,,a is target b is grav
        /// </summary>
        public static Vector3D Rejection(Vector3D a, Vector3D b) //reject a on b
        {
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                return Vector3D.Zero;

            return a - a.Dot(b) / b.LengthSquared() * b;
        }

        Vector3D reject(Vector3D aTarget, Vector3D aPlane, Vector3D aNormal) =>
            aTarget - (Vector3D.Dot(aTarget - aPlane, aNormal) * aNormal);

        static Vector3D VectorProjection(Vector3D a, Vector3D b) { // project a onto b 
            if (Vector3D.IsZero(b))
                return new Vector3D(0, 0, 0);
            Vector3D projection = a.Dot(b) / b.LengthSquared() * b;
            return projection;
        }

        static Vector3D calcddt(double m, Vector3D v, Vector3D dv, Vector3D g) {
            return m * (2 * (v - dv + g));
        }
        static double desiredDampeningThrust(double mass, double velocity, double gravity) => mass * (2 * velocity + gravity);
        static double forceOfVelocity(double mass, double velocity, double time) => mass * velocity / time;
        static double momentum(double force, double time) => force * time;
        static double forceOfMomentum(double momentum, double time) => momentum / time;
        static double acceleration(double force, double mass) => force / mass;
        static double force(double mass, double acceleration) => mass * acceleration;
        static double accelerationFromDelta(double deltaVelocity, double deltaTime) => deltaVelocity / deltaTime;
        static Program() {
            Console.WriteLine($"Forward   {Vector3D.Forward}"); // Forward   X:0 Y:0 Z:-1
            Console.WriteLine($"Up        {Vector3D.Up}");      // Up        X:0 Y:1 Z:0
            Console.WriteLine($"Right     {Vector3D.Right}");   // Right     X:1 Y:0 Z:0
        }
        // U = FxL
        // D = FxR
        // L = FxD
        // R = FxU
        // F = RxD
        // B = RxU
        static Vector3D up(Vector3D forward, Vector3D left) => forward.Cross(left);
        static Vector3D down(Vector3D forward, Vector3D right) => forward.Cross(right);
        static Vector3D left(Vector3D forward, Vector3D down) => forward.Cross(down);
        static Vector3D right(Vector3D forward, Vector3D up) => forward.Cross(up);
        static Vector3D front(Vector3D right, Vector3D down) => right.Cross(down);
        static Vector3D back(Vector3D right, Vector3D up) => right.Cross(up);

        static double rps2rpm(double rps) => (rps / (Math.PI * 2)) * 60.0;
        static double rpm2rps(double rpm) => (rpm * (Math.PI * 2)) / 60.0;
        static void gpsx() {
            int i = 0;
            while (true) gps((i++).ToString(), rd());
            
        }
        static Vector2I gps(string s, Vector3D dir, bool force = false) {
            double azimuth, elevation;
            Vector3D.GetAzimuthAndElevation(dir, out azimuth, out elevation);
            if (double.IsNaN(azimuth)) {
                azimuth = 0;
            }
            int yaw = (int)MathHelper.ToDegrees(azimuth);
            int pitch = (int)MathHelper.ToDegrees(elevation);
            
            return new Vector2I(yaw, pitch);


        }

        static void sort() {
            var item = 1;
            var sortedList = new List<int> { 2, 6, 3, 9 };
            while (item < sortedList.Count) {
                var insertLocation = item - 1;
                while (insertLocation > -1 && sortedList[insertLocation] < sortedList[item]) {
                    insertLocation--;
                }
                sortedList.Insert(insertLocation + 1, sortedList[item]);
                sortedList.RemoveAt(item + 1);
                item++;
            }
            return;
        }

        static Vector3D[] vecs ={ Vector3D.Left, Vector3D.Right, Vector3D.Up, Vector3D.Down, Vector3D.Forward, Vector3D.Backward };
        static void Main(string[] args) {

            double dz = -72.11;
            double z = -63.95;
            double m = 10.0;


            var apply = z - dz;

            var force = apply * -m;



            Console.ReadKey();
            return;
            sort();
            gpsx();
            Console.ReadKey();
            return;

            double d = 90;
            int k = (int)d / 100;
            Console.WriteLine($"i    = {k}");
            var rps = 0.1;
            var rpm = 59;
            var result = rpm2rps(rpm);
            Console.WriteLine($"rps    = {rps}");
            Console.WriteLine($"rpm    = {rpm}");
            Console.WriteLine($"result = {result}");
            Console.WriteLine();
            
            // todo check if Vector3D.Transform behaves differently with non normal axis
            // todo check if Vector3D.Transform returns a normal when passed a normal


            var f = 10.0; // n
            //var m = 1.0;
            var v = new Vector3D(2, 0, 0);

            var p = m * v; // kgm/s
            
            var g = new Vector3D(0, -2, 0);
            var dv = new Vector3D(-1, 0, 0);

            int i = 1;  
            int j = 22;
            j = +i;
            var ddt = m * (2 * v + g);

            
            Console.WriteLine($"up = {up(Vector3D.Forward, Vector3D.Left)}");
            Console.WriteLine($"m = {m}");
            Console.WriteLine($"v = {v}");
            Console.WriteLine($"g = {g}");
            Console.WriteLine($"ddt = {ddt}");
            Console.WriteLine($"j = {+j}");

            // maxAcceleration = thrusterThrust / shipMass
            // f = ma
            // a = f/m
            // derpy says
            // maxAcceleration = thrusterThrust / shipMass - 9.81;
            // boosterFireDuration = Speed / maxAcceleration / 2;
            // minAltitude = Speed * boosterFireDuration + landingOffset;


            //var antiGrav = Vector3D.Up;


            //Console.WriteLine($"ddt       {ddt}");
            //Console.WriteLine($"ddtDir    {ddtDir}");
            //Console.WriteLine($"ddtMag    {ddtMag}");
            
            Console.WriteLine(Math.PI / 2.0);
            Console.ReadKey();
            return;
            var targetDir = Vector3D.Normalize(Vector3D.Right + Vector3D.Up);
            //var targetDir = Vector3D.Right;
            


            //var targetDir = Vector3D.Right;
            var gravDir = Vector3D.Down;

            var angle = Math.Acos(targetDir.Dot(gravDir));
            var axis = targetDir.Cross(gravDir);
            axis.Normalize();
            var mat = MatrixD.CreateFromAxisAngle(axis, -(angle - Math.PI / 2.0));
            //var virtualPlane = mRC.CenterOfMass + mvGravity;
            var virtualNormal = Vector3D.Transform(gravDir, mat);
            virtualNormal.Normalize();
            Console.WriteLine($"Forward   {Vector3D.Forward}");
            Console.WriteLine($"Up        {Vector3D.Up}");
            Console.WriteLine($"Right     {Vector3D.Right}");
            Console.WriteLine($"targetDir {targetDir}");
            Console.WriteLine($"gravDir   {gravDir}");
            Console.WriteLine($"axis      {axis}");
            Console.WriteLine($"angle     {angle}");
            Console.WriteLine($"fangle    {(float)angle}");
            Console.WriteLine($"normal    {virtualNormal}");

            Console.ReadKey();
            return;

            //GPS:ctvCoM:19745.06:143861.44:-108993.72:#FF75C9F1:
            var com = new Vector3D(19745.06, 143861.44, -108993.72);

            //GPS:targetabove:19775.06:143846.47:-108977.86:#FF75C9F1:
            var targetAbove = new Vector3D(19775.06, 143846.47, -108977.86);

            //GPS:targetbelow:19766.2:143830.21:-108975.64:#FF75C9F1:
            var targetBelow = new Vector3D(19766.2, 143830.21, -108975.64);

            //GPS:targetprojection:19772.56:143841.03:-108981.25:#FF75C9F1:
            var targetProjection = new Vector3D(19772.56, 143841.03, -108981.25);

            var gravity = new Vector3D(-0.890217840671539, -1.93874788284302, -1.20976269245148);
            var gravityDirection = Vector3D.Normalize(gravity);

            //var leanPosition = new Vector3D(19756.93, 143877.06, -108981.51);
            //var lean = -Vector3D.Normalize(leanPosition - com);
            var lean = gravityDirection;
            //calculateThrustVector((Vector3D.Up + Vector3D.Forward) / 2.0, Vector3D.Zero, Vector3D.Down);
            var mass = 30123.201171875;
            thrustvector.calculateThrustVector(targetProjection, com, gravity, lean, mass, mdNewtons);
            Console.ReadKey();
            return;
            var ab = angleBetween(Vector3D.Right, Vector3D.Left);
            op($"ab = {ab}");

            var thrust = Rejection(Vector3D.Right, Vector3D.Down);
            op($"thrust = {thrust}");
            Console.ReadKey();
            return;
            //trig2();
            upComponent();
            rejection();
            op("");
            stoppingTime();
            Console.ReadKey();
            return;
            op("");
            velocityAwayFromDirectionOfGravity();
            op("");
            trig1();
            
            
            return;
            //int i;
            var set = new HashSet<string>();
            
            Console.WriteLine(set.Count);

            var dir = (Vector3D.Forward + Vector3D.Up) * 0.5;
            Console.WriteLine(dir);
            Console.ReadKey();
            return;

            //  p = m * v
            //  (ddt / mass) - grav = 2x velo
            //  300 = 10     * (2 * 10 + 10)
            //  ddt = 10     * (2 * 10 + 10)
            //  ddt = mass   * (2 * velocity + gravity);

            //var velo = ((mdNewtons / mdMass) - mdGravity) * 0.5;

            //var ddt = mdMass * (2 * mdVelocity + mdGravity);
            //Console.WriteLine($"ddt {ddt}");
            //Console.WriteLine($"velo {velo}");

            var bb = new BoundingBoxD(new Vector3D(-10), new Vector3D(10));
            bb.Inflate(1);
            Console.WriteLine(bb);
            Console.WriteLine(bb.Center);
            Console.WriteLine(bb.Extents);
            Console.ReadKey();
            return;
            cindex();
            cwork();
            kwork();
            Console.ReadKey();
            return;
            var e = new MyDetectedEntityInfo();
            Console.WriteLine(e.EntityId);
            Console.WriteLine("CDIST");
            Console.WriteLine((new Vector3D(100)).Length());
            Console.WriteLine("KDIST");
            Console.WriteLine((new Vector3D(1000)).Length());
            Console.ReadKey();
            return;
            kwork();
            Console.ReadKey();
            return;
            
            Console.WriteLine($"Double {double.MaxValue}");
            Console.WriteLine($"I {uint.MaxValue}");
            Console.WriteLine($"Double/k {double.MaxValue / 1000.0}");
            Console.WriteLine($"Double/k > intmax {(double.MaxValue / 1000.0) > i}");

            //var target = new Vector3D(986148.14, 102603.57, 1599688.09);
            
            //Console.WriteLine($"target {target}");
            
            for ( i = 0; i < 1000; i++) {
                
                
            }
            Console.ReadKey();
            return;
            Console.WriteLine(Math.Sqrt(2));
            sphere();

            




            StringBuilder sb = new StringBuilder();
            foreach (var c in "1234567890") {
                Console.WriteLine((byte)c);
            }
            
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider()) {
                byte[] by = new byte[1024];
                rng.GetBytes(by);
                foreach (var b in by) {
                    if ((b >= 'a' && b <= 'z') || (b >= 'A' && b <= 'Z') || (b >= '0' && b <= '9')) {
                        sb.Append((char)b);
                    }
                    if (sb.Length == 24) {
                        break;
                    }
                }
            }
            Console.WriteLine(sb.ToString());
            var free = 1.0;
            var vol = 1.01;
            var famount = free / vol;
            var iamount = (int)famount;
            Console.WriteLine(famount.ToString());
            Console.WriteLine(iamount.ToString());
            // force = mass x(velocity / time) = (mass x velocity) / time = momentum / time
            // if p = mv and m is constant, then F = m*dv/dt = ma

            // f = ma
            Console.WriteLine(Guid.NewGuid());
            Console.WriteLine($"mdTimeFactor = {mdTimeFactor}");

            //var forceOfVelocityResult = forceOfVelocity(mdMass, mdVelocity, mdTimeFactor);
            //Console.WriteLine($"forceOfVelocity = {forceOfVelocityResult}");

            //var momentumResult = momentum(forceOfVelocityResult, mdTimeFactor);
            //Console.WriteLine($"momentum = {momentumResult}");

            //var forceOfMomentumResult = forceOfMomentum(momentumResult, mdTimeFactor);
            //Console.WriteLine($"forceOfMomentum = {forceOfMomentumResult}");

            //var accelerationResult = acceleration(forceOfVelocityResult, mdMass);
            //Console.WriteLine($"acceleration = {accelerationResult}");

            //var forceResult = force(mdMass, accelerationResult);
            //Console.WriteLine($"force = {forceResult}");

            //var desiredDampeningThrustResult = desiredDampeningThrust(mdMass, mdVelocity, 0);
            //Console.WriteLine($"desiredDampeningThrust = {desiredDampeningThrustResult}");

            Console.ReadKey();
        }
        // whip says....
        // var desiredDampeningThrust = mass * (2 * velocity + gravity);
        // dont use this one



    }/*
      * If v is the vector that points 'up' and p0 is some point on your plane, and finally p is the point that might be below the plane, 
      * compute the dot product v * (p−p0). This projects the vector to p on the up-direction. This product is {−,0,+} if p is below, on, above the plane, respectively.
      */
}

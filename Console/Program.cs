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


        static Random r = new Random(7);
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
        const double mdMass = 15327;
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
        
        


        static void Main(string[] args) {
            int i;
            var set = new HashSet<string>();
            for (i = 0; i < Base27Directions.Directions.Length; i++) {
                var d = Base27Directions.Directions[i];
                if (!set.Contains(d.ToString())) {
                    set.Add(d.ToString());
                    Console.WriteLine($"{i} {d}");
                }
            }
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

            var target = new Vector3D(986148.14, 102603.57, 1599688.09);
            
            Console.WriteLine($"target {target}");
            
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

            var forceOfVelocityResult = forceOfVelocity(mdMass, mdVelocity, mdTimeFactor);
            Console.WriteLine($"forceOfVelocity = {forceOfVelocityResult}");

            var momentumResult = momentum(forceOfVelocityResult, mdTimeFactor);
            Console.WriteLine($"momentum = {momentumResult}");

            var forceOfMomentumResult = forceOfMomentum(momentumResult, mdTimeFactor);
            Console.WriteLine($"forceOfMomentum = {forceOfMomentumResult}");

            var accelerationResult = acceleration(forceOfVelocityResult, mdMass);
            Console.WriteLine($"acceleration = {accelerationResult}");

            var forceResult = force(mdMass, accelerationResult);
            Console.WriteLine($"force = {forceResult}");

            var desiredDampeningThrustResult = desiredDampeningThrust(mdMass, mdVelocity, 0);
            Console.WriteLine($"desiredDampeningThrust = {desiredDampeningThrustResult}");

            Console.ReadKey();
        }
        // whip says....
        // var desiredDampeningThrust = mass * (2 * velocity + gravity);
        // dont use this one

        static double desiredDampeningThrust(double mass, double velocity, double gravity) => mass * (2 * velocity + gravity);
        static double forceOfVelocity(double mass, double velocity, double time) => mass * velocity / time;
        static double momentum(double force, double time) => force * time;
        static double forceOfMomentum(double momentum, double time) => momentum / time;
        static double acceleration(double force, double mass) => force / mass;
        static double force(double mass, double acceleration) => mass * acceleration;        
        static double accelerationFromDelta(double deltaVelocity, double deltaTime) => deltaVelocity / deltaTime;

    }/*
      * If v is the vector that points 'up' and p0 is some point on your plane, and finally p is the point that might be below the plane, 
      * compute the dot product v * (p−p0). This projects the vector to p on the up-direction. This product is {−,0,+} if p is below, on, above the plane, respectively.
      */
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace commandline
{
    class Program
    {

        const int miInterval = 10;
        const double mdTickTime = 1.0 / 60.0;
        const double mdTimeFactor = mdTickTime * miInterval;
        //const double mdTimeFactor = 0.2;
        const double mMass = 3887.0;
        const double mVelocity = 1.71;
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
        const int cmax = 10;
        static int toIndex(int x, int y, int z) => (z * cmax * cmax) + (y * cmax) + x;
        static Vector3D toVector(int idx) {
            int z = idx / (cmax * cmax);
            idx -= (z * cmax * cmax);
            int y = idx / cmax;
            int x = idx % cmax;
            return new Vector3D(x, y, z);
        }
        static Vector3D vectorFromIndex(Vector3D aWorldPosition, int aIndex) => k2v(v2k(aWorldPosition)) + toVector(aIndex) * 100;
        static Vector3D v2n(Vector3D v, long n) {
            if (v.X < 0) { v.X -= n; }
            if (v.Y < 0) { v.Y -= n; }
            if (v.Z < 0) { v.Z -= n; }
            v.X = (long)v.X / n;
            v.Y = (long)v.Y / n;
            v.Z = (long)v.Z / n;
            return v;
        }
        static Vector3D n2v(Vector3D v, long n) {
            if (v.X < 0) { v.X++; }
            if (v.Y < 0) { v.Y++; }
            if (v.Z < 0) { v.Z++; }
            v.X *= n;
            v.Y *= n;
            v.Z *= n;
            return v;
        }
        static Vector3D v2k(Vector3D v) => v2n(v, 1000);
        static Vector3D k2v(Vector3D v) => n2v(v, 1000);

        static void Main(string[] args) {
            var i = UInt64.MaxValue;
            Console.WriteLine($"Double {double.MaxValue}");
            Console.WriteLine($"I {uint.MaxValue}");
            Console.WriteLine($"Double/k {double.MaxValue / 1000.0}");
            Console.WriteLine($"Double/k > intmax {(double.MaxValue / 1000.0) > i}");


            var target = new Vector3D(986148.14, 102603.57, 1599688.09);
            
            Console.WriteLine($"target {target}");
            
            for ( i = 0; i < 1000; i++) {
                
                Console.WriteLine($"{i} {vectorFromIndex(target, (int)i)}");
                
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

            var forceOfVelocityResult = forceOfVelocity(mMass, mVelocity, mdTimeFactor);
            Console.WriteLine($"forceOfVelocity = {forceOfVelocityResult}");

            var momentumResult = momentum(forceOfVelocityResult, mdTimeFactor);
            Console.WriteLine($"momentum = {momentumResult}");

            var forceOfMomentumResult = forceOfMomentum(momentumResult, mdTimeFactor);
            Console.WriteLine($"forceOfMomentum = {forceOfMomentumResult}");

            var accelerationResult = acceleration(forceOfVelocityResult, mMass);
            Console.WriteLine($"acceleration = {accelerationResult}");

            var forceResult = force(mMass, accelerationResult);
            Console.WriteLine($"force = {forceResult}");

            var desiredDampeningThrustResult = desiredDampeningThrust(mMass, mVelocity, 0);
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

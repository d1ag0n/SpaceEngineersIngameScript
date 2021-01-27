using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
        static void Main(string[] args) {
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

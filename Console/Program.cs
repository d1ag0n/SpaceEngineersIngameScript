using System;
using System.Collections.Generic;
using System.Linq;
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

            Console.ReadKey();
        }

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

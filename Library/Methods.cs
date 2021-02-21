using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    class Methods
    {
        // http://danikgames.com/blog/moving-target-intercept-in-3d/

        // this isnt mine I copied it from somewhere
        void fintimeofcol() {
            // Find the time of collision (distance / relative velocity)
            //float timeToCollision = ((shotOrigin - targetOrigin).magnitude - shotRadius - targetRadius) / (shotVelOrth.magnitude - targetVelOrth.magnitude);

            // Calculate where the shot will be at the time of collision
            //Vector3 shotVel = shotVelOrth + shotVelTang;
            //Vector3 shotCollisionPoint = shotOrigin + shotVel * timeToCollision;
        }
        // this isnt mine I copied it from somewhere
        Vector3D FindInterceptVector(Vector3 shotOrigin, double shotSpeed, Vector3D targetOrigin, Vector3 targetVel) {

            Vector3D dirToTarget = Vector3.Normalize(targetOrigin - shotOrigin);

            // Decompose the target's velocity into the part parallel to the
            // direction to the cannon and the part tangential to it.
            // The part towards the cannon is found by projecting the target's
            // velocity on dirToTarget using a dot product.
            Vector3D targetVelOrth =
            Vector3D.Dot(targetVel, dirToTarget) * dirToTarget;

            // The tangential part is then found by subtracting the
            // result from the target velocity.
            Vector3D targetVelTang = targetVel - targetVelOrth;

            /*
            * targetVelOrth
            * |
            * |
            *
            * ^...7  <-targetVel
            * |  /.
            * | / .
            * |/ .
            * t--->  <-targetVelTang
            *
            *
            * s--->  <-shotVelTang
            *
            */

            // The tangential component of the velocities should be the same
            // (or there is no chance to hit)
            // THIS IS THE MAIN INSIGHT!
            Vector3D shotVelTang = targetVelTang;

            // Now all we have to find is the orthogonal velocity of the shot

            double shotVelSpeed = shotVelTang.Length();
            Vector3D result;
            if (shotVelSpeed > shotSpeed) {
                // Shot is too slow to intercept target, it will never catch up.
                // Do our best by aiming in the direction of the targets velocity.
                result = Vector3D.Normalize(targetVel) * shotSpeed;
            } else {
                // We know the shot speed, and the tangential velocity.
                // Using pythagoras we can find the orthogonal velocity.
                double shotSpeedOrth =
                Math.Sqrt(shotSpeed * shotSpeed - shotVelSpeed * shotVelSpeed);
                Vector3D shotVelOrth = dirToTarget * shotSpeedOrth;

                // Finally, add the tangential and orthogonal velocities.
                result = shotVelOrth + shotVelTang;
            }
            return result;
        }
    }
}
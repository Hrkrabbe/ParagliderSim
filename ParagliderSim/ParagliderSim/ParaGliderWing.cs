using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace ParagliderSim
{
    class ParaGliderWing
    {
        String name;
        float velocity;
        float maxVelocity = 2;
        float width;
        Vector3 leftVingPoint;
        Vector3 rightVingPoint;
        Vector2 moveVector = new Vector2(0, 1);
        float leftVelocity;
        float rightVelocity;


        public String Name { get { return name; }}
        public float Speed { get { return velocity; } }
        public float Width { get { return width; } }

        public ParaGliderWing(String name, float speed, float width)
        {
            this.name = name;
            this.width = width;
            this.velocity = speed;
        }

        /*public void move(float amount, float leftWingSpeedFactor, float rightWingSpeedFactor)
        {
            leftVingPoint = new Vector2(0, 0);
            rightVingPoint = new Vector2(width, 0);
            leftVingPoint += moveVector * velocity * leftWingSpeedFactor * amount;
            rightVingPoint += moveVector * velocity * rightWingSpeedFactor * amount;
        }*/

        public void move(Vector2 wind, float updraft, float downforce, float leftAcceleration, float rightAcceleration, float amount, float dragX)
        {
            leftVingPoint = Vector3.Zero;
            rightVingPoint  = Vector3.Zero;

            //Velocity
            leftVelocity += leftAcceleration * amount;
            rightVelocity += rightAcceleration * amount;

            float dragLeft = (float)Math.Pow((double)leftVelocity, 2) * dragX;
            leftVelocity -= dragLeft * amount;
            float dragRight = (float)Math.Pow((double)rightVelocity, 2) * dragX;
            rightVelocity -= dragRight * amount;


           /* if (leftVelocity > maxVelocity)
                leftVelocity = maxVelocity;
            if (rightVelocity > maxVelocity)
                rightVelocity = maxVelocity; */



            leftVingPoint.X = wind.X;
            leftVingPoint.Y = updraft - downforce;
            leftVingPoint.Z = leftVelocity * -1f;
            leftVingPoint *= amount;

            rightVingPoint.X = wind.X;
            rightVingPoint.Y = updraft - downforce;
            rightVingPoint.Z = rightVelocity * -1f;
            rightVingPoint *= amount;
        }

        public float getRotationY()
        {
            return (float)Math.Atan2(-rightVingPoint.Z + leftVingPoint.Z, rightVingPoint.X + width - leftVingPoint.X) * 1.2f;
        }

        public float getRotationZ()
        {
            return (leftVelocity - rightVelocity) * -0.5f;
        }

        public Vector3 getMovementVector()
        {
            //return new Vector3(0, 0, -(leftVingPoint.Y + rightVingPoint.Y) /2);
            return (leftVingPoint + rightVingPoint) / 2f; 
        }
    }
}

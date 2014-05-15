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
        float speed;
        float width;
        Vector2 leftVingPoint;
        Vector2 rightVingPoint;
        Vector2 moveVector = new Vector2(0, 1);


        public String Name { get { return name; }}
        public float Speed { get { return speed; } }
        public float Width { get { return width; } }

        public ParaGliderWing(String name, float speed, float width)
        {
            this.name = name;
            this.width = width;
            this.speed = speed;
        }

        public void move(float amount, float leftWingSpeedFactor, float rightWingSpeedFactor)
        {
            leftVingPoint = new Vector2(0, 0);
            rightVingPoint = new Vector2(width, 0);
            leftVingPoint += moveVector * speed * leftWingSpeedFactor * amount;
            rightVingPoint += moveVector * speed * rightWingSpeedFactor * amount;
        }

        public float getRotation()
        {
            return (float)Math.Atan2(rightVingPoint.Y - leftVingPoint.Y, rightVingPoint.X - leftVingPoint.X);
        }

        public Vector3 getMovementVector()
        {
            return new Vector3(0, 0, -(leftVingPoint.Y + rightVingPoint.Y) /2);
        }
    }
}

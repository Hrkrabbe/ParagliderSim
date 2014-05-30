using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using OculusRift.Oculus;


namespace ParagliderSim
{
    public class Player : Microsoft.Xna.Framework.DrawableGameComponent
    {
        Game1 game;
        ParaGliderWing currentWing = new ParaGliderWing("test", 5.0f, 5.0f);

        //Player
        Model playerModel;
        const float rotationSpeed = 0.1f;
        float moveSpeed = 50.0f;
        float lefrightRot = MathHelper.PiOver2;
        //float updownRot = -MathHelper.Pi / 10.0f;
        float updownRot = 0;
        float rotZ = 0;
        Matrix playerBodyRotation;
        Matrix playerWorld;
        Vector3 playerPosition = new Vector3(740, 250, -700);
        BoundingSphere playerSphere, originalPlayerSphere;

        //Camera and movement
        MouseState originalMouseState;
        Matrix cameraRotation;
        Vector3 cameraOriginalTarget;
        Vector3 cameraRotatedTarget;
        Vector3 cameraFinalTarget;
        Vector3 rotatedVector;
        Vector3 cameraOriginalUpVector;
        Vector3 cameraRotatedUpVector;

        //Physics
        //float maxVel = 5f;
        //float velocity = 1;
        //float acceleration;
        Vector2 wind = new Vector2(0.1f,0);

        //Collision
        bool isColliding;

        //arms
        Model leftArmModel;
        Model rightArmModel;
        Matrix leftArmWorld;
        Matrix rightArmWorld;
        float maxArmRot = MathHelper.PiOver4;
        Vector3 leftArmPos = new Vector3 (-0.22f,-0.277f,0.064f);
        Vector3 rightArmPos = new Vector3(0.22f, -0.277f, 0.064f);
        float leftArmRotX;
        float rightArmRotX;

        #region properties

        public Vector3 Position
        {
            get { return playerPosition; }
        }

        public Vector3 camFinalTarget
        {
            get { return cameraFinalTarget; }
        }

        public Matrix camRotation
        {
            get { return cameraRotation; }
        }

        public bool IsColliding
        {
            get { return isColliding; }
        }

        public Matrix World
        {
            get { return playerWorld; }
        }

        public float RotationY
        {
            get { return lefrightRot; }
        }

        public float RotationX
        {
            get { return updownRot; }
        }

        public Matrix PlayerBodyRotation
        {
            get { return playerBodyRotation; }
        }

        #endregion

        public Player(Game1 game)
            : base(game)
        {
            this.game = game;
        }

        public override void Initialize()
        {
            base.Initialize();
        }



        protected override void LoadContent()
        {
            playerModel = game.Content.Load<Model>(@"Models/body");
            leftArmModel = game.Content.Load<Model>(@"Models/leftArm");
            rightArmModel = game.Content.Load<Model>(@"Models/rightArm");

            initPlayerSphere();
            Mouse.SetPosition(game.GraphicsDevice.Viewport.Width / 2, game.GraphicsDevice.Viewport.Height / 2);
            originalMouseState = Mouse.GetState();
        }

        public override void Update(GameTime gameTime)
        {
            float timeDifference = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (game.IsDebug)
            {
                ProcessInputDebug(timeDifference);
            }
            else 
            {
                processInput(timeDifference);
            }

            CalculateWorldLeftArm();
            CalculateWorldRightArm();

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            Matrix[] transforms = new Matrix[playerModel.Bones.Count];
            playerModel.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (ModelMesh mesh in playerModel.Meshes)
            {
                foreach (BasicEffect beffect in mesh.Effects)
                {
                    beffect.EnableDefaultLighting();
                    beffect.World = transforms[mesh.ParentBone.Index] * playerWorld;
                    beffect.View = game.ViewMatrix;
                    beffect.Projection = game.ProjectionMatrix;
                }
                mesh.Draw();
            }

            drawArms();
        }

        public void drawArms()
        {
            //Left arm
            Matrix[] transforms = new Matrix[leftArmModel.Bones.Count];
           leftArmModel.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (ModelMesh mesh in leftArmModel.Meshes)
            {
                foreach (BasicEffect beffect in mesh.Effects)
                {
                    beffect.EnableDefaultLighting();
                    beffect.World = transforms[mesh.ParentBone.Index] * leftArmWorld;
                    beffect.View = game.ViewMatrix;
                    beffect.Projection = game.ProjectionMatrix;
                }
                mesh.Draw();
            }

            //Right arm
            transforms = new Matrix[rightArmModel.Bones.Count];
            rightArmModel.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (ModelMesh mesh in rightArmModel.Meshes)
            {
                foreach (BasicEffect beffect in mesh.Effects)
                {
                    beffect.EnableDefaultLighting();
                    beffect.World = transforms[mesh.ParentBone.Index] * rightArmWorld;
                    beffect.View = game.ViewMatrix;
                    beffect.Projection = game.ProjectionMatrix;
                }
                mesh.Draw();
            }
 
        }

        private void CalculateWorldLeftArm()
        {
            Matrix positionRotationMatrix = Matrix.CreateTranslation(-game.Player.Position)
                               * game.Player.PlayerBodyRotation
                               * Matrix.CreateTranslation(game.Player.Position);
            Vector3 translation = Vector3.Transform(game.Player.Position + leftArmPos,
                                           positionRotationMatrix);

            leftArmWorld = Matrix.CreateScale(0.01f) * Matrix.CreateRotationY((float)Math.PI) * Matrix.CreateRotationZ(rotZ) * Matrix.CreateRotationX(updownRot) * Matrix.CreateRotationX(leftArmRotX) * Matrix.CreateRotationY(lefrightRot) * Matrix.CreateTranslation(translation);
        }

        private void CalculateWorldRightArm()
        {
            Matrix positionRotationMatrix = Matrix.CreateTranslation(-game.Player.Position)
                               * game.Player.PlayerBodyRotation
                               * Matrix.CreateTranslation(game.Player.Position);
            Vector3 translation = Vector3.Transform(game.Player.Position + rightArmPos,
                                           positionRotationMatrix);

            rightArmWorld = Matrix.CreateScale(0.01f) * Matrix.CreateRotationY((float)Math.PI) * Matrix.CreateRotationZ(rotZ) * Matrix.CreateRotationX(updownRot) * Matrix.CreateRotationX(rightArmRotX) * Matrix.CreateRotationY(lefrightRot) * Matrix.CreateTranslation(translation);
        }

        private void initPlayerSphere()
        {
            foreach (ModelMesh mesh in playerModel.Meshes)
            {
                originalPlayerSphere = BoundingSphere.CreateMerged(originalPlayerSphere, mesh.BoundingSphere);
            }
            //originalPlayerSphere = originalPlayerSphere.Transform(Matrix.CreateScale(100.0f));
        }

        #region movement
        private void ProcessInputDebug(float amount)
        {
            MouseState currentMouseState = Mouse.GetState();
            if (currentMouseState != originalMouseState)
            {
                float deltaX = currentMouseState.X - originalMouseState.X;
                float deltaY = currentMouseState.Y - originalMouseState.Y;
                lefrightRot -= rotationSpeed * deltaX * amount;
                updownRot -= rotationSpeed * deltaY * amount;
                Mouse.SetPosition(game.GraphicsDevice.Viewport.Width / 2, game.GraphicsDevice.Viewport.Height / 2);
                //UpdateViewMatrix();
            }

            Vector3 moveVector = new Vector3(0, 0, 0);
            KeyboardState keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.Up) || keyState.IsKeyDown(Keys.W))
                moveVector += new Vector3(0, 0, -1);
            if (keyState.IsKeyDown(Keys.Down) || keyState.IsKeyDown(Keys.S))
                moveVector += new Vector3(0, 0, 1);
            if (keyState.IsKeyDown(Keys.Right) || keyState.IsKeyDown(Keys.D))
                moveVector += new Vector3(1, 0, 0);
            if (keyState.IsKeyDown(Keys.Left) || keyState.IsKeyDown(Keys.A))
                moveVector += new Vector3(-1, 0, 0);
            if (keyState.IsKeyDown(Keys.Q))
                moveVector += new Vector3(0, 1, 0);
            if (keyState.IsKeyDown(Keys.Z))
                moveVector += new Vector3(0, -1, 0);
            AddToPlayerPosition(moveVector * amount);
            UpdateViewMatrix();
        }

        private void processInput(float amount)
        {
            KeyboardState keyState = Keyboard.GetState();
            GamePadState padState = GamePad.GetState(PlayerIndex.One);

            //arms
            leftArmRotX = 0f;
            rightArmRotX = 0f;

            if (padState.IsConnected && padState.ThumbSticks.Left != Vector2.Zero && padState.ThumbSticks.Left.Y > 0)
            {
                updownRot = padState.ThumbSticks.Left.Y * 0.153f * -1f;
            }


            //downforce
            float downforce = 0.05f;
            moveSpeed = currentWing.Speed;

            //acceleration
            float leftAcceleration =((float)Math.Sin(updownRot) * -9.8f) +  0.25f;
            float rightAcceleration = ((float)Math.Sin(updownRot) * -9.8f) + 0.25f;
            float dragX = 3f / 8f;



            //Keyboard
            if (keyState.IsKeyDown(Keys.Left) || keyState.IsKeyDown(Keys.A))
            {
                leftArmRotX = -maxArmRot;
                leftAcceleration = 0;
            }
            if (keyState.IsKeyDown(Keys.Right) || keyState.IsKeyDown(Keys.D))
            {
                rightArmRotX = -maxArmRot;
                rightAcceleration = 0;
            }
            if (keyState.IsKeyDown(Keys.R))
            {
                OculusClient.ResetSensorOrientation(0);
            }

            //gamepad
            if (padState.IsConnected && padState.Triggers.Left != 0)
            {
                leftArmRotX = -maxArmRot * padState.Triggers.Left;
                leftAcceleration -= leftAcceleration * padState.Triggers.Left;
            }

            if (padState.IsConnected && padState.Triggers.Right != 0)
            {
                rightArmRotX = -maxArmRot * padState.Triggers.Right;
                rightAcceleration -= rightAcceleration * padState.Triggers.Right;
            }

           

            if (padState.IsConnected && padState.Buttons.A == ButtonState.Pressed)
            {
                OculusClient.ResetSensorOrientation(0);
            }

            currentWing.move(wind, game.Terrain.getUpdraft(playerPosition), downforce, leftAcceleration, rightAcceleration, amount, dragX);

            lefrightRot += currentWing.getRotationY();
            rotZ = currentWing.getRotationZ();

            Vector3 upDraft = new Vector3(0, 1, 0) * game.Terrain.getUpdraft(playerPosition);
            AddToPlayerPosition(currentWing.getMovementVector() + (upDraft*amount));
            UpdateViewMatrix();
        }

        private void AddToPlayerPosition(Vector3 delta)
        {
            playerBodyRotation = Matrix.CreateRotationZ(rotZ) * Matrix.CreateRotationX(updownRot) * Matrix.CreateRotationY(lefrightRot) ;
            rotatedVector = Vector3.Transform(delta, playerBodyRotation);
            playerPosition += rotatedVector * moveSpeed;
            isColliding = checkCollision();
        }

        private void UpdateViewMatrix()
        {
            Vector3 cameraPosition;
            playerBodyRotation = Matrix.CreateRotationZ(rotZ) * Matrix.CreateRotationX(updownRot) * Matrix.CreateRotationY(lefrightRot);

            if (game.OREnabled)
            {
                cameraRotation = Matrix.CreateFromQuaternion(OculusClient.GetPredictedOrientation()) * playerBodyRotation;
                //Neck movement
                
                Vector3 neck = new Vector3(0, 0.3f, 0);
                Vector3 neck2 = Vector3.Transform(neck, playerBodyRotation);
                cameraPosition = playerPosition - neck2;
                neck = Vector3.Transform(neck, Matrix.CreateFromQuaternion((OculusClient.GetPredictedOrientation()) * 0.65f)* playerBodyRotation);
                cameraPosition += neck;
                 
                //cameraPosition = playerPosition;
            }
            else
            {
                cameraRotation = playerBodyRotation;
                cameraPosition = playerPosition;
            }

            cameraOriginalTarget = new Vector3(0, 0, -1);
            cameraRotatedTarget = Vector3.Transform(cameraOriginalTarget, cameraRotation);
            cameraFinalTarget = cameraPosition + cameraRotatedTarget;

            cameraOriginalUpVector = new Vector3(0, 1, 0);
            cameraRotatedUpVector = Vector3.Transform(cameraOriginalUpVector, cameraRotation);

            game.ViewMatrix = Matrix.CreateLookAt(cameraPosition, cameraFinalTarget, cameraRotatedUpVector);




            //player
            playerWorld = Matrix.Identity * Matrix.CreateScale(0.01f) * Matrix.CreateRotationY((float)Math.PI) * playerBodyRotation * Matrix.CreateTranslation(playerPosition);
            playerSphere = originalPlayerSphere.Transform(playerWorld);
        }
        #endregion

        #region collision
        public bool checkCollision()
        {

            Vector3 d = new Vector3(0,0,-playerSphere.Radius);
            Vector3 direction = Vector3.Transform(d, playerBodyRotation);
            if (checkTerrainCollision(playerSphere.Center) || checkTerrainCollision(playerSphere.Center + direction) || checkWorldComponentCollision())
                return true;
            else
                return false;
        }

        public bool checkWorldComponentCollision()
        {
            bool collision = false;

            foreach (WorldComponent wc in game.WorldComponents)
            {
                if (playerSphere.Intersects(wc.getBoundingSphere()))
                {
                    Vector3 direction = wc.getBoundingSphere().Center - playerSphere.Center;
                    direction.Normalize();
                    Ray ray = new Ray(playerSphere.Center, direction);
                    float? collisionDistance = ray.Intersects(wc.getBoundingSphere());
                    float collistionDepth = collisionDistance.HasValue ? playerSphere.Radius - collisionDistance.Value : 0.0f;

                    playerPosition += -direction * collistionDepth;
                    collision = true;
                }
            }
            return collision;
        }

        public bool checkTerrainCollision(Vector3 position)
        {
            if (position.X < 0 || position.Z > 0 || position.X > game.Terrain.getWidthUnits() || -position.Z > game.Terrain.getHeightUnits())
                return false;
            else
            {
                Plane plane = game.Terrain.getPlane(position);
                if (playerSphere.Intersects(plane) == PlaneIntersectionType.Intersecting)
                {
                    Vector3 rayPos = playerSphere.Center;
                    Vector3 planeNormal = -plane.Normal;
                    planeNormal.Normalize();
                    Ray ray = new Ray(rayPos, -planeNormal);
                    float? collisionDistance = ray.Intersects(plane);
                    float collisionDepth = collisionDistance.HasValue ? playerSphere.Radius - collisionDistance.Value : 0.0f;

                    playerPosition += planeNormal * collisionDepth;
                    return true;
                }
                else
                    return false;
            }
        }

        public bool checkTerrainCollisionSquare(Vector3 position)
        {
            if (position.X < 0 || position.Z > 0 || position.X > game.Terrain.getWidthUnits() || -position.Z > game.Terrain.getHeightUnits())
                return false;
            else
            {
                foreach (Plane plane in game.Terrain.getPlanes(position))
                {
                if (playerSphere.Intersects(plane) == PlaneIntersectionType.Intersecting)
                {
                    Vector3 rayPos = playerSphere.Center;
                    Vector3 planeNormal = -plane.Normal;
                    planeNormal.Normalize();
                    Ray ray = new Ray(rayPos, -planeNormal);
                    float? collisionDistance = ray.Intersects(plane);
                    float collisionDepth = collisionDistance.HasValue ? playerSphere.Radius - collisionDistance.Value : 0.0f;

                    playerPosition += planeNormal * collisionDepth;
                    return true;
                }
                }
                    return false;
            }
        }
        #endregion

        
        public Quaternion getRotation()
        {
            Quaternion quat = Quaternion.CreateFromRotationMatrix(
                //Matrix.CreateRotationZ(0.0f)*
                 Matrix.CreateRotationY(lefrightRot)
                * Matrix.CreateRotationX(updownRot));
            return quat;
        } 
    }
}

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
        ParaGliderWing currentWing = new ParaGliderWing("test", 2.0f, 5.0f);

        //Player
        Model playerModel;
        const float rotationSpeed = 0.1f;
        float moveSpeed = 150.0f;
        float lefrightRot = MathHelper.PiOver2;
        //float updownRot = -MathHelper.Pi / 10.0f;
        float updownRot = 0;
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

        //Collision
        bool isColliding;
        
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
            playerModel = game.Content.Load<Model>(@"Models/CharacterModelNew");

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
        }

        private void initPlayerSphere()
        {
            foreach (ModelMesh mesh in playerModel.Meshes)
            {
                originalPlayerSphere = BoundingSphere.CreateMerged(originalPlayerSphere, mesh.BoundingSphere);
            }
            originalPlayerSphere = originalPlayerSphere.Transform(Matrix.CreateScale(100.0f));
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
            moveSpeed = currentWing.Speed;
            float leftWingFactor = 1;
            float righWingFactor = 1;

            KeyboardState keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.Left) || keyState.IsKeyDown(Keys.A))
                leftWingFactor = 0.65f;
            if (keyState.IsKeyDown(Keys.Right) || keyState.IsKeyDown(Keys.D))
                righWingFactor = 0.65f;

            currentWing.move(amount, leftWingFactor, righWingFactor);

            lefrightRot += currentWing.getRotation();
            AddToPlayerPosition(currentWing.getMovementVector());
            UpdateViewMatrix();
        }

        private void AddToPlayerPosition(Vector3 delta)
        {
            playerBodyRotation = Matrix.CreateRotationX(updownRot) * Matrix.CreateRotationY(lefrightRot);
            rotatedVector = Vector3.Transform(delta, playerBodyRotation);
            playerPosition += rotatedVector * moveSpeed;
            isColliding = checkCollision();
        }

        private void UpdateViewMatrix()
        {
            Vector3 cameraPosition;
            playerBodyRotation = Matrix.CreateRotationX(updownRot) * Matrix.CreateRotationY(lefrightRot);

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

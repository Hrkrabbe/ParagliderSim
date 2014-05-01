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
using JigLibX.Physics;
using JigLibX.Geometry;
using JigLibX.Collision;

namespace ParagliderSim
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont font;
        GraphicsDevice device;
        Effect effect;

        Vector3 lightDirection = new Vector3(1.0f, -1.0f, 1.0f);
        double  resolutionX = 1280,
                resolutionY = 800;
        
        //Test
        Model unitMeter;
        Model house;
        //Texture2D cloudMap;
        Model skyDome;

        List<WorldComponent> worldComponents;

        //Terrain
        float terrainScale = 16;
        Texture2D heightmap;
        Texture2D grassTexture;
        Texture2D sandTexture;
        Texture2D rockTexture;
        Texture2D snowTexture;
        Terrain terrain;

        //Water
        const float waterHeight = 5.0f;
        RenderTarget2D refractionRenderTarget;
        Texture2D refractionMap;
        RenderTarget2D reflectionRenderTarget;
        Texture2D reflectionMap;


        //Camera and movement
        Matrix viewMatrix;
        Matrix projectionMatrix, originalProjectionMatrix;
        MouseState originalMouseState;
        Matrix cameraRotation;
        Vector3 cameraOriginalTarget;
        Vector3 cameraRotatedTarget;
        Vector3 cameraFinalTarget;
        Vector3 rotatedVector;
        Vector3 cameraOriginalUpVector;
        Vector3 cameraRotatedUpVector;

        //Player
        Model playerModel;
        const float rotationSpeed = 0.1f;
        const float moveSpeed = 30.0f;
        float lefrightRot = MathHelper.PiOver2;
        float updownRot = -MathHelper.Pi / 10.0f;
        Matrix playerBodyRotation;
        Matrix playerWorld;
        Vector3 playerPosition = new Vector3(740, 250, -700);
        BoundingSphere playerSphere, originalPlayerSphere;

        //Oculus Rift
        bool OREnabled = true;
        #region ORVars
        OculusClient oculusClient;
        Effect oculusRiftDistortionShader;
        RenderTarget2D renderTargetLeft;
        RenderTarget2D renderTargetRight;
        Texture2D renderTextureLeft;
        Texture2D renderTextureRight;
        float scaleImageFactor;
        float fov_x;
        float fov_d;
        int IPD = 0;
        public static float aspectRatio;
        float yfov;
        float viewCenter;
        float eyeProjectionShift;
        float projectionCenterOffset;
        Matrix projCenter;
        Matrix projLeft;
        Matrix projRight;
        Matrix viewLeft;
        Matrix viewRight;
        float halfIPD;

        private int viewportWidth;
        private int viewportHeight;
        private Microsoft.Xna.Framework.Rectangle sideBySideLeftSpriteSize;
        private Microsoft.Xna.Framework.Rectangle sideBySideRightSpriteSize;
        #endregion

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            this.IsFixedTimeStep = true;

            if (OREnabled)
            {
                oculusClient = new OculusClient();
                scaleImageFactor = 0.71f;

                graphics.PreferredBackBufferWidth = (int)Math.Ceiling(resolutionX / scaleImageFactor);
                graphics.PreferredBackBufferHeight = (int)Math.Ceiling(resolutionY / scaleImageFactor);
                graphics.IsFullScreen = true;
            }
            else
            {
                graphics.PreferredBackBufferWidth = (int)resolutionX;
                graphics.PreferredBackBufferHeight = (int)resolutionY;
                graphics.IsFullScreen = false;
            }
            
            graphics.ApplyChanges();
            Window.Title = "ParagliderSim";

        }

        protected override void Initialize()
        {
            device = graphics.GraphicsDevice;
            PhysicsSystem world = new PhysicsSystem();
            world.CollisionSystem = new CollisionSystemSAP();

            if (OREnabled)
            {
                InitOculus();
            }
            else
            {
                projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 1.0f, 2000.0f);
            }
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(device);

            font = Content.Load<SpriteFont>("SpriteFont1");
            effect = Content.Load<Effect>(@"Shader/effects");
            heightmap = Content.Load<Texture2D>(@"Images/heightmap2");
            playerModel = Content.Load<Model>(@"Models/CharacterModelNew");
            grassTexture = Content.Load<Texture2D>(@"Textures/grass");
            sandTexture = Content.Load<Texture2D>(@"Textures/sand");
            rockTexture = Content.Load<Texture2D>(@"Textures/rock");
            snowTexture = Content.Load<Texture2D>(@"Textures/snow");
            unitMeter = Content.Load<Model>(@"Models/unitMeter");
            house = Content.Load<Model>(@"Models/house2");

            skyDome = Content.Load<Model>(@"Models/SkyDome");
            //skyDome.Meshes[0].MeshParts[0].Effect = effect.Clone();
            //cloudMap = Content.Load<Texture2D>(@"Textures/cloudMap");

            PresentationParameters pp = device.PresentationParameters;
            refractionRenderTarget = new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight);
            reflectionRenderTarget = new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight);

            terrain = new Terrain(device,terrainScale, heightmap, grassTexture, sandTexture, rockTexture, snowTexture);

            Mouse.SetPosition(device.Viewport.Width / 2, device.Viewport.Height / 2);
            originalMouseState = Mouse.GetState();

            initGameWorld();
            initPlayerSphere();
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            float timeStep = (float)gameTime.ElapsedGameTime.Ticks / TimeSpan.TicksPerSecond;
            PhysicsSystem.CurrentPhysicsSystem.Integrate(timeStep);

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            float timeDifference = (float)gameTime.ElapsedGameTime.TotalSeconds;
            ProcessInput(timeDifference);

            base.Update(gameTime);
        }

        public void initPlayerSphere()
        {
            foreach (ModelMesh mesh in playerModel.Meshes)
            {
                originalPlayerSphere = BoundingSphere.CreateMerged(originalPlayerSphere, mesh.BoundingSphere);
            }
            originalPlayerSphere = originalPlayerSphere.Transform(Matrix.CreateScale(100.0f));
        }

        #region movement
        private void ProcessInput(float amount)
        {
            MouseState currentMouseState = Mouse.GetState();
            if (currentMouseState != originalMouseState)
            {
                float deltaX = currentMouseState.X - originalMouseState.X;
                float deltaY = currentMouseState.Y - originalMouseState.Y;
                lefrightRot -= rotationSpeed * deltaX * amount;
                updownRot -= rotationSpeed * deltaY * amount;
                Mouse.SetPosition(device.Viewport.Width / 2, device.Viewport.Height / 2);
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
            AddToCameraPosition(moveVector * amount);
            UpdateViewMatrix();
        }

        private void AddToCameraPosition(Vector3 delta)
        {
            playerBodyRotation = Matrix.CreateRotationX(updownRot) * Matrix.CreateRotationY(lefrightRot);
            rotatedVector = Vector3.Transform(delta, playerBodyRotation);
            playerPosition += rotatedVector * moveSpeed;
        }

        private void UpdateViewMatrix()
        {
            playerBodyRotation = Matrix.CreateRotationX(updownRot) * Matrix.CreateRotationY(lefrightRot);
       
            if (OREnabled)
            {
                cameraRotation = Matrix.CreateFromQuaternion(OculusClient.GetPredictedOrientation()) * playerBodyRotation;
            }
            else
                cameraRotation = playerBodyRotation;

            cameraOriginalTarget = new Vector3(0, 0, -1);
            cameraRotatedTarget = Vector3.Transform(cameraOriginalTarget, cameraRotation);
            cameraFinalTarget = playerPosition + cameraRotatedTarget;

            cameraOriginalUpVector = new Vector3(0, 1, 0);
            cameraRotatedUpVector = Vector3.Transform(cameraOriginalUpVector, cameraRotation);

            viewMatrix = Matrix.CreateLookAt(playerPosition, cameraFinalTarget, cameraRotatedUpVector);

            //player
            playerWorld = Matrix.Identity * Matrix.CreateScale(0.01f) * Matrix.CreateRotationY((float)Math.PI) * playerBodyRotation * Matrix.CreateTranslation(playerPosition);
            playerSphere = originalPlayerSphere.Transform(playerWorld);
        }
        #endregion

        #region collision
        public bool checkCollision()
        {
            if (checkTerrainCollision() || checkWorldComponentCollision())
                return true;
            else
                return false;
        }

        public bool checkWorldComponentCollision()
        {
            bool collision = false;

            foreach (WorldComponent wc in worldComponents)
            {
                if (playerSphere.Intersects(wc.getBoundingSphere()))
                    collision = true;
            }
            return collision;
        }

        public bool checkTerrainCollision()
        {
            if (playerPosition.X < 0 || playerPosition.Z > 0 || playerPosition.X > terrain.getWidthUnits() || -playerPosition.Z > terrain.getHeightUnits())
                return false;
            else
            {
                if (playerSphere.Intersects(terrain.getPlane(playerPosition)) == PlaneIntersectionType.Intersecting)
                    return true;
                else
                    return false;
            }
        }
        #endregion

        #region Water
        private Microsoft.Xna.Framework.Plane CreatePlane(float height, Vector3 planeNormalDirection, Matrix currentViewMatrix, bool clipSide)
        {
            planeNormalDirection.Normalize();
            Vector4 planeCoeffs = new Vector4(planeNormalDirection, height);
            if (clipSide)
                planeCoeffs *= -1;

            Microsoft.Xna.Framework.Plane finalPlane = new Microsoft.Xna.Framework.Plane(planeCoeffs);

            return finalPlane;
        }
        private void DrawRefractionMap()
        {
            Microsoft.Xna.Framework.Plane refractionPlane = CreatePlane(waterHeight + 1.5f, new Vector3(0, -1, 0), viewMatrix, false);

            //refractionPlane = CreatePlane(30.0045F, new Vector3(0, -1, 0), viewMatrix, false);
            effect.Parameters["ClipPlane0"].SetValue(new Vector4(refractionPlane.Normal, refractionPlane.D));
            effect.Parameters["Clipping"].SetValue(true);    // Allows the geometry to be clipped for the purpose of creating a refraction map
            device.SetRenderTarget(refractionRenderTarget);
            //  device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);
            terrain.Draw(viewMatrix, projectionMatrix, effect, lightDirection);
            device.SetRenderTarget(null);
            effect.Parameters["Clipping"].SetValue(false);   // Make sure you turn it back off so the whole scene doesnt keep rendering as clipped
            refractionMap = refractionRenderTarget;
        }
        #endregion

        #region OR
        private void InitOculus()
        {
            // Load the Oculus Rift Distortion Shader
            // https://mega.co.nz/#!E4YkjJ6K!MuIDuB78NwgHsGgeONikDAT_OLJQ0ZeLXbfGF1OAhzw
            oculusRiftDistortionShader = Content.Load<Effect>(@"Shader/OculusRift");

            aspectRatio = (float)(OculusClient.GetScreenResolution().X * 0.5f / (float)(OculusClient.GetScreenResolution().Y));
            fov_d = OculusClient.GetEyeToScreenDistance();
            fov_x = OculusClient.GetScreenSize().Y * scaleImageFactor;
            yfov = 2.0f * (float)Math.Atan(fov_x / fov_d);

            // Set ProjectionMatrix
            originalProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(yfov, aspectRatio, 0.05f, 10000.0f);

            // Init left and right RenderTarget
            renderTargetLeft = new RenderTarget2D(device, graphics.PreferredBackBufferWidth / 2, graphics.PreferredBackBufferHeight, true, SurfaceFormat.Bgr565,DepthFormat.Depth24Stencil8);
            renderTargetRight = new RenderTarget2D(device, graphics.PreferredBackBufferWidth / 2, graphics.PreferredBackBufferHeight, true, SurfaceFormat.Bgr565,DepthFormat.Depth24Stencil8);

            OculusClient.SetSensorPredictionTime(0, 0.03f);
            UpdateResolutionAndRenderTargets();
        }

        private void UpdateResolutionAndRenderTargets()
        {

            if (viewportWidth != device.Viewport.Width || viewportHeight != device.Viewport.Height)
            {
                viewportWidth = device.Viewport.Width;
                viewportHeight = device.Viewport.Height;
                sideBySideLeftSpriteSize = new Microsoft.Xna.Framework.Rectangle(0, 0, viewportWidth / 2, viewportHeight);
                sideBySideRightSpriteSize = new Microsoft.Xna.Framework.Rectangle(viewportWidth / 2, 0, viewportWidth / 2, viewportHeight);
            }
        }

        private void SetProjectionOffset()
        {
            viewCenter = OculusClient.GetScreenSize().X * 0.212f; // 0.25f
            eyeProjectionShift = viewCenter - OculusClient.GetLensSeparationDistance() * 0.5f;
            projectionCenterOffset = 4.0f * eyeProjectionShift / OculusClient.GetScreenSize().X;

            projCenter = originalProjectionMatrix;
            projLeft = Matrix.CreateTranslation(projectionCenterOffset, 0, 0) * projCenter;
            projRight = Matrix.CreateTranslation(-projectionCenterOffset, 0, 0) * projCenter;

            halfIPD = OculusClient.GetInterpupillaryDistance() * 0.5f;
            //Matrix viewLefOffset = Matrix.CreateTranslation
            viewLeft =  viewMatrix * Matrix.CreateTranslation(halfIPD, 0, 0) ;
            viewRight = viewMatrix * Matrix.CreateTranslation(-halfIPD, 0, 0);
        }

        private void DrawOculusRenderTargets()
        {
            // Set RenderTargets
            device.SetRenderTarget(null);
            renderTextureLeft = (Texture2D)renderTargetLeft;
            renderTextureRight = (Texture2D)renderTargetRight;
            device.Clear(Color.Black);

            //Set the four Distortion params of the oculus
            oculusRiftDistortionShader.Parameters["distK0"].SetValue(oculusClient.DistK0);
            oculusRiftDistortionShader.Parameters["distK1"].SetValue(oculusClient.DistK1);
            oculusRiftDistortionShader.Parameters["distK2"].SetValue(oculusClient.DistK2);
            oculusRiftDistortionShader.Parameters["distK3"].SetValue(oculusClient.DistK3);
            oculusRiftDistortionShader.Parameters["imageScaleFactor"].SetValue(scaleImageFactor);

            // Pass for left lens
            oculusRiftDistortionShader.Parameters["drawLeftLens"].SetValue(true);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, null, null, null, oculusRiftDistortionShader);
            spriteBatch.Draw(renderTextureLeft, sideBySideLeftSpriteSize, Microsoft.Xna.Framework.Color.White);
            spriteBatch.End();

            // Pass for right lens
            oculusRiftDistortionShader.Parameters["drawLeftLens"].SetValue(false);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, null, null, null, oculusRiftDistortionShader);
            spriteBatch.Draw(renderTextureRight, sideBySideRightSpriteSize, Microsoft.Xna.Framework.Color.White);
            spriteBatch.End();
        }

        private void DrawOR()
        {
            device.Clear(Color.Black);
            SetProjectionOffset();

            SetLeftEye();
            DrawSkyDome(viewMatrix);
            terrain.Draw(viewMatrix, projectionMatrix, effect, lightDirection);
            drawGameWorld();
            DrawPlayer();
            //DrawModel(unitMeter, new Vector3(736, 195.2f, -700));
            //DrawModel(house, new Vector3(740, 195, -700));
            
            //if (playerPosition.X > 0 || playerPosition.Z < 0 || playerPosition.X > terrain.getWidthUnits() || -playerPosition.Z < terrain.getHeightUnits())
            //    DrawCollision();
            //DrawRefractionMap();

            SetRightEye();
            DrawSkyDome(viewMatrix);
            terrain.Draw(viewMatrix, projectionMatrix, effect, lightDirection);
            drawGameWorld();
            DrawPlayer();
            //DrawModel(unitMeter, new Vector3(736, 195.2f, -700));
            //DrawModel(house, new Vector3(740, 195, -700));
            
            //if (playerPosition.X > 0 || playerPosition.Z < 0 || playerPosition.X > terrain.getWidthUnits() || -playerPosition.Z < terrain.getHeightUnits())
            //    DrawCollision();
            //DrawRefractionMap();

            DrawOculusRenderTargets();
            DrawInfo();
        }

        private void SetLeftEye()
        {
            device.SetRenderTarget(renderTargetLeft);
            device.Clear(Color.Black);
            viewMatrix = viewLeft;
            projectionMatrix = projLeft;
        }

        private void SetRightEye()
        {
            device.SetRenderTarget(renderTargetRight);
            device.Clear(Color.Black);
            viewMatrix = viewRight;
            projectionMatrix = projRight;
        }
        #endregion

        #region Draw
        protected override void Draw(GameTime gameTime)
        {

            if (OREnabled)
            {
                DrawOR();
            }
            else
            {
                terrain.Draw(viewMatrix, projectionMatrix, effect, lightDirection);
                DrawPlayer();
                DrawModel(unitMeter);
                DrawInfo();
                //if (playerPosition.X > 0 || playerPosition.Z < 0 || playerPosition.X > terrain.getWidthUnits() || -playerPosition.Z < terrain.getHeightUnits())
                //    DrawCollision();
            }
            base.Draw(gameTime);
        }

        private void DrawPlayer()
        {
            Matrix[] transforms = new Matrix[playerModel.Bones.Count];
            playerModel.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (ModelMesh mesh in playerModel.Meshes)
            {
                foreach (BasicEffect beffect in mesh.Effects)
                {
                    beffect.EnableDefaultLighting();
                    beffect.World = transforms[mesh.ParentBone.Index] * playerWorld;
                    beffect.View = viewMatrix;
                    beffect.Projection = projectionMatrix;
                }
                mesh.Draw();
            }
        }

        private void DrawModel(Model model)
        {
            Matrix[] transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect beffect in mesh.Effects)
                {
                    beffect.EnableDefaultLighting();
                    beffect.World = transforms[mesh.ParentBone.Index] * Matrix.CreateScale(0.01f) ;
                    beffect.View = viewMatrix;
                    beffect.Projection = projectionMatrix;
                }
                mesh.Draw();
            }
        }

        private void DrawModel(Model model, Vector3 position)
        {
            Matrix[] transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect beffect in mesh.Effects)
                {
                    beffect.EnableDefaultLighting();
                    beffect.World = transforms[mesh.ParentBone.Index] * Matrix.CreateScale(0.01f) * Matrix.CreateTranslation(position) ;
                    beffect.View = viewMatrix;
                    beffect.Projection = projectionMatrix;
                }
                mesh.Draw();
            }
        }


        private void DrawInfo()
        {
            spriteBatch.Begin();
            spriteBatch.DrawString(font, playerPosition.ToString() +"\n"+ halfIPD * 2 + "\n" + checkCollision().ToString(), new Vector2(20, 20), Color.Red);
            spriteBatch.End();

        }

        private void DrawSkyDome(Matrix currentViewMatrix)
        {
            device.Clear(Color.Black);
            RasterizerState rs = new RasterizerState();
            rs.CullMode = CullMode.None;
            rs.FillMode = FillMode.Solid;
            rs.DepthBias = 0;
            device.RasterizerState = rs;
            device.DepthStencilState = DepthStencilState.None;
            //device.SamplerStates[0] = SamplerState.LinearWrap;
            //device.BlendState = BlendState.Opaque;

            Matrix[] modelTransforms = new Matrix[skyDome.Bones.Count];
            skyDome.CopyAbsoluteBoneTransformsTo(modelTransforms);

            Matrix wMatrix = Matrix.CreateTranslation(0, -100.0f, 0) * Matrix.CreateScale(1) * Matrix.CreateTranslation(playerPosition);
            foreach (ModelMesh mesh in skyDome.Meshes)
            {
                foreach (BasicEffect currentEffect in mesh.Effects)
                {
                    Matrix worldMatrix = modelTransforms[mesh.ParentBone.Index] * wMatrix;
                    currentEffect.LightingEnabled = false;
                    currentEffect.World = worldMatrix;
                    currentEffect.View = viewMatrix;
                    currentEffect.Projection = projectionMatrix;
                    
                }
                mesh.Draw();
            }
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default; 
        }

        //test for å se hvor kollisjonsplanet havner
        private void DrawCollision()
        {
            BasicEffect e = new BasicEffect(device);
            e.View = viewMatrix;
            e.Projection = projectionMatrix;
            e.World = Matrix.Identity * Matrix.CreateTranslation(0, 0.1f, 0);
            foreach(EffectPass p in e.CurrentTechnique.Passes)
            {
                p.Apply();
                device.DrawUserPrimitives(Microsoft.Xna.Framework.Graphics.PrimitiveType.TriangleList, terrain.getCollisionVertices(playerPosition), 0, 1, VertexPositionColor.VertexDeclaration);
            }
        }
        #endregion

        #region gameWorld
        public void initGameWorld()
        {
            worldComponents = new List<WorldComponent>();

            //Add all static components of the world map here:
            worldComponents.Add(new WorldComponent(unitMeter, 0.01f, 0, new Vector3(736, 195.2f, -700)));
            worldComponents.Add(new WorldComponent(house, 0.01f, 0, new Vector3(740, 195, -700)));
            worldComponents.Add(new WorldComponent(unitMeter, 0.02f, 0, new Vector3(726, 200.2f, -700)));
            worldComponents.Add(new WorldComponent(unitMeter, 0.03f, 0, new Vector3(716, 210.2f, -700)));
            worldComponents.Add(new WorldComponent(unitMeter, 0.05f, 0, new Vector3(706, 220.2f, -700)));
            worldComponents.Add(new WorldComponent(unitMeter, 0.1f, 0, new Vector3(696, 240.2f, -700)));
        }

        public void drawGameWorld()
        {
            //draws all static components of world map
            foreach (WorldComponent wc in worldComponents)
            {
                wc.Draw(viewMatrix, projectionMatrix);
            }
        }
        #endregion
    }


    public struct VertexMultitextured
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector4 TextureCoordinate;
        public Vector4 TexWeights;

        //   public static int SizeInBytes = (3 + 3 + 4 + 4) * sizeof(float);  

        public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration
         (
         new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
         new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
         new VertexElement(sizeof(float) * 6, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0),
         new VertexElement(sizeof(float) * 10, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1)
         );

    }
}

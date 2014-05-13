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
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont font;
        GraphicsDevice device;
        Effect effect;
        Player player;

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
        const float waterHeight = 80.0f;
        RenderTarget2D refractionRenderTarget;
        Texture2D refractionMap;
        RenderTarget2D reflectionRenderTarget;
        Texture2D reflectionMap;
        Matrix reflectionViewMatrix;
        VertexBuffer waterVertexBuffer;
        VertexDeclaration waterVertexDeclaration;
        Texture2D waterBumpMap;
        Vector3 windDirection = new Vector3(0, 0, 1);

        //Camera and movement
        Matrix viewMatrix;
        Matrix projectionMatrix, originalProjectionMatrix;
        MouseState originalMouseState;

        bool isDebug = true;
        //Oculus Rift
        bool orEnabled = true;
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

        #region properties
        public Matrix ViewMatrix
        {
            get { return viewMatrix; }
            set { viewMatrix = value; }
        }

        public Matrix ProjectionMatrix
        {
            get { return projectionMatrix; }
            //set { ProjectionMatrix = value; }
        }

        public OculusClient OculusClient
        {
            get {return oculusClient;}
        }

        public bool OREnabled
        {
            get { return orEnabled; }
        }

        public Terrain Terrain
        {
            get { return terrain; }
        }

        public List<WorldComponent> WorldComponents
        {
            get { return worldComponents; }
        }

        public bool IsDebug
        {
            get { return isDebug; }
        }

        #endregion

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            this.IsFixedTimeStep = true;

            if (orEnabled)
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
            player = new Player(this);
            Components.Add(player);

            if (orEnabled)
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
            
            grassTexture = Content.Load<Texture2D>(@"Textures/grass");
            sandTexture = Content.Load<Texture2D>(@"Textures/sand");
            rockTexture = Content.Load<Texture2D>(@"Textures/rock");
            snowTexture = Content.Load<Texture2D>(@"Textures/snow");
            unitMeter = Content.Load<Model>(@"Models/unitMeter");
            house = Content.Load<Model>(@"Models/house2");
            waterBumpMap = Content.Load<Texture2D>("waterbump");

            skyDome = Content.Load<Model>(@"Models/SkyDome");
            //skyDome.Meshes[0].MeshParts[0].Effect = effect.Clone();
            //cloudMap = Content.Load<Texture2D>(@"Textures/cloudMap");

            PresentationParameters pp = device.PresentationParameters;
         //   refractionRenderTarget = new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight);
            refractionRenderTarget = new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, false, pp.BackBufferFormat, pp.DepthStencilFormat);
            reflectionRenderTarget = new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, true, SurfaceFormat.Bgr565, DepthFormat.Depth24Stencil8);

            terrain = new Terrain(device,terrainScale, heightmap, grassTexture, sandTexture, rockTexture, snowTexture);
            SetUpWaterVertices();
            waterVertexDeclaration = new VertexDeclaration(VertexPositionTexture.VertexDeclaration.GetVertexElements());
            initGameWorld();
            //initPlayerSphere();
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            base.Update(gameTime);
            //water
            Vector3 dirnormal = player.camFinalTarget - player.Position;
            dirnormal.Y = -dirnormal.Y; dirnormal.Normalize();
           //Vector3 playerPosition = player.Position;
            Vector3 reflCameraPosition = player.Position;
            reflCameraPosition.Y = -(player.Position.Y - (waterHeight * 2f));
            //Vector3 reflTargetPos = player.camFinalTarget;
            //reflTargetPos.Y = -player.camFinalTarget.Y + waterHeight * 2;
            Vector3 reflTargetPos = reflCameraPosition + (dirnormal * 2);

            Vector3 cameraRight = Vector3.Transform(new Vector3(1, 0, 0), player.camRotation);
            Vector3 invUpVector = Vector3.Cross(cameraRight, reflTargetPos - reflCameraPosition);

            reflectionViewMatrix = Matrix.CreateLookAt(reflCameraPosition, reflTargetPos, invUpVector);
        }

        #region Water
        private Plane CreatePlane(float height, Vector3 planeNormalDirection, Matrix currentViewMatrix, bool clipSide)
        {
            planeNormalDirection.Normalize();
            Vector4 planeCoeffs = new Vector4(planeNormalDirection, height);
            if (clipSide)
                planeCoeffs *= -1;

            Plane finalPlane = new Plane(planeCoeffs);

            return finalPlane;
        }
        private void DrawRefractionMap()
        {
            Plane refractionPlane = CreatePlane(waterHeight + 1.5f, new Vector3(0, -1, 0), viewMatrix, false);

            //refractionPlane = CreatePlane(30.0045F, new Vector3(0, -1, 0), viewMatrix, false);
            effect.Parameters["ClipPlane0"].SetValue(new Vector4(refractionPlane.Normal, refractionPlane.D));
            effect.Parameters["Clipping"].SetValue(true);    // Allows the geometry to be clipped for the purpose of creating a refraction map
            device.SetRenderTarget(refractionRenderTarget);
             device.Clear(Color.Black); //ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);
            terrain.Draw(viewMatrix, projectionMatrix, effect, lightDirection);
            device.SetRenderTarget(null);
            effect.Parameters["Clipping"].SetValue(false);   // Make sure you turn it back off so the whole scene doesnt keep rendering as clipped
            refractionMap = refractionRenderTarget;


           
           //System.IO.Stream ss = System.IO.File.OpenWrite("C:\\Test\\Refraction.jpg");
           //refractionRenderTarget.SaveAsJpeg(ss, 500, 500);
           //ss.Close();
        }

        private void DrawReflectionMap()
        {
            Plane reflectionPlane = CreatePlane(waterHeight - 0.5f, new Vector3(0, -1, 0), reflectionViewMatrix, true);
            effect.Parameters["ClipPlane0"].SetValue(new Vector4(reflectionPlane.Normal, reflectionPlane.D));
            effect.Parameters["Clipping"].SetValue(true);    // Allows the geometry to be clipped for the purpose of creating a refraction map
            device.SetRenderTarget(reflectionRenderTarget);
            device.Clear(Color.Black);
            DrawSkyDome(reflectionViewMatrix);
            terrain.Draw(reflectionViewMatrix, projectionMatrix, effect, lightDirection);
            effect.Parameters["Clipping"].SetValue(false);
            device.SetRenderTarget(null);
            reflectionMap = reflectionRenderTarget;

            //System.IO.Stream ss = System.IO.File.OpenWrite("C:\\Test\\Reflection.jpg");
            //reflectionRenderTarget.SaveAsJpeg(ss, 500, 500);
            //ss.Close();
        }

        private void SetUpWaterVertices()
        {
            VertexPositionTexture[] waterVertices = new VertexPositionTexture[6];

            waterVertices[0] = new VertexPositionTexture(new Vector3(0, waterHeight, 0), new Vector2(0, 1));
            waterVertices[2] = new VertexPositionTexture(new Vector3(terrain.getWidthUnits(), waterHeight, -terrain.getHeightUnits()), new Vector2(1, 0));
            waterVertices[1] = new VertexPositionTexture(new Vector3(0, waterHeight, -terrain.getHeightUnits()), new Vector2(0, 0));

            waterVertices[3] = new VertexPositionTexture(new Vector3(0, waterHeight, 0), new Vector2(0, 1));
            waterVertices[5] = new VertexPositionTexture(new Vector3(terrain.getWidthUnits(), waterHeight, 0), new Vector2(1, 1));
            waterVertices[4] = new VertexPositionTexture(new Vector3(terrain.getWidthUnits(), waterHeight, -terrain.getHeightUnits()), new Vector2(1, 0));

            
         //   VertexDeclaration vertexDeclaration = new VertexDeclaration(VertexMultitextured.VertexElements);
            waterVertexBuffer = new VertexBuffer(device, VertexPositionTexture.VertexDeclaration, waterVertices.Count(), BufferUsage.WriteOnly);
            waterVertexBuffer.SetData(waterVertices);
        }


        private void DrawWater(float time)
        {
            effect.CurrentTechnique = effect.Techniques["Water"];
            Matrix worldMatrix = Matrix.Identity;
            effect.Parameters["xWorld"].SetValue(worldMatrix);
            effect.Parameters["xView"].SetValue(viewMatrix);
            effect.Parameters["xReflectionView"].SetValue(reflectionViewMatrix);
            effect.Parameters["xProjection"].SetValue(projectionMatrix);
            effect.Parameters["xReflectionMap"].SetValue(reflectionMap);
            effect.Parameters["xRefractionMap"].SetValue(refractionMap);
            effect.Parameters["xWaterBumpMap"].SetValue(waterBumpMap);           
            effect.Parameters["xWaveLength"].SetValue(0.1f);
            effect.Parameters["xWaveHeight"].SetValue(0.1f);
            effect.Parameters["xTime"].SetValue(time);
            effect.Parameters["xWindForce"].SetValue(0.0002f);
            effect.Parameters["xWindDirection"].SetValue(windDirection);

            effect.CurrentTechnique.Passes[0].Apply();

            device.SetVertexBuffer(waterVertexBuffer);

            device.DrawPrimitives(PrimitiveType.TriangleList, 0, waterVertexBuffer.VertexCount / 3);
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

            if (orEnabled)
            {
                DrawOR(gameTime);
            }
            else
            {
                terrain.Draw(viewMatrix, projectionMatrix, effect, lightDirection);
                //DrawPlayer();
                DrawModel(unitMeter);
                DrawInfo();
                //if (playerPosition.X > 0 || playerPosition.Z < 0 || playerPosition.X > terrain.getWidthUnits() || -playerPosition.Z < terrain.getHeightUnits())
                //    DrawCollision();

                base.Draw(gameTime);
            }
            
        }

        private void DrawOR(GameTime gameTime)
        {
            float time = (float)gameTime.TotalGameTime.TotalMilliseconds / 100.0f;
          
            device.Clear(Color.Black);
            SetProjectionOffset();

            DrawRefractionMap();
            DrawReflectionMap();

            SetLeftEye();
            DrawSkyDome(viewMatrix);
            terrain.Draw(viewMatrix, projectionMatrix, effect, lightDirection);
            drawGameWorld();
            player.Draw();
            DrawWater(time);
            base.Draw(gameTime);
            DrawRefractionMap();
            DrawReflectionMap();
            
            
            //if (player.Position.X > 0 || player.Position.Z < 0 || player.Position.X > terrain.getWidthUnits() || -player.Position.Z < terrain.getHeightUnits())
            //    DrawCollision();


            SetRightEye();

            DrawSkyDome(viewMatrix);
            terrain.Draw(viewMatrix, projectionMatrix, effect, lightDirection);
            drawGameWorld();
            player.Draw();
            DrawWater(time);
            base.Draw(gameTime);
            //DrawRefractionMap();
            //DrawReflectionMap();
            //DrawWater(time);
            //if (player.Position.X > 0 || player.Position.Z < 0 || player.Position.X > terrain.getWidthUnits() || -player.Position.Z < terrain.getHeightUnits())
            //    DrawCollision();


            DrawOculusRenderTargets();
            DrawInfo();
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
            spriteBatch.DrawString(font, player.Position.ToString() +"\n"+ halfIPD * 2 + "\n" + player.IsColliding.ToString(), new Vector2(20, 20), Color.Red);
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

            Matrix wMatrix = Matrix.CreateTranslation(0, -100.0f, 0) * Matrix.CreateScale(1) * Matrix.CreateTranslation(player.Position);
            foreach (ModelMesh mesh in skyDome.Meshes)
            {
                foreach (BasicEffect currentEffect in mesh.Effects)
                {
                    Matrix worldMatrix = modelTransforms[mesh.ParentBone.Index] * wMatrix;
                    currentEffect.LightingEnabled = false;
                    currentEffect.World = worldMatrix;
                    currentEffect.View = currentViewMatrix;
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
                device.DrawUserPrimitives(Microsoft.Xna.Framework.Graphics.PrimitiveType.TriangleList, terrain.getCollisionVertices(player.Position), 0, 1, VertexPositionColor.VertexDeclaration);
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

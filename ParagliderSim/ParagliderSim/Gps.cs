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


namespace ParagliderSim
{
    public class Gps : Microsoft.Xna.Framework.DrawableGameComponent
    {
        SpriteFont font, font2;
        RenderTarget2D renderTarget;
        Texture2D screen;
        Texture2D arrow;
        Model model;
        ParagliderSimulator game;
        Matrix screenWorld, GpsWorld;
        float rotation = 0.0f;
        Vector2 origin, arrowPos;
        Vector3 GpsPos = new Vector3(0, -0.7f, -0.4f);
        float scale = 0.8f;
        Vector3 target;

        VertexPositionNormalTexture[] vertices;

        public Gps(Game game)
            : base(game)
        {
            this.game = (ParagliderSimulator)game;
        }

        public override void Initialize()
        {
            setupVertices();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            renderTarget = new RenderTarget2D(GraphicsDevice, 64, 64, false, SurfaceFormat.Bgr565, DepthFormat.None);
            arrow = game.Content.Load<Texture2D>(@"Images/arrow");
            model = game.Content.Load<Model>(@"Models/phone");
            font = game.Content.Load<SpriteFont>(@"Fonts/SpriteFont2");
            font2 = game.Content.Load<SpriteFont>(@"Fonts/SpriteFont3");

            origin.X = arrow.Width / 2;
            origin.Y = arrow.Height / 2;
            arrowPos.X = renderTarget.Width / 2;
            arrowPos.Y = renderTarget.Height / 4;

            target = game.Target.Position;
            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            CalculateRotation();
            CalculateWorldGPS();
            CalculateWorldScreen();

            //height and distance
            float height = game.Player.Position.Y - game.WaterHeight;
            Vector2 v = new Vector2(target.X - game.Player.Position.X, target.Z - game.Player.Position.Z);
            float distance = v.Length();

            float score = 100f - distance;
            if (score < 0f)
                score = 0f;

            GraphicsDevice.SetRenderTarget(renderTarget);
            GraphicsDevice.Clear(Color.White);

            if (game.currentGameState == gameState.Playing)
            {
                game.SpriteBatch.Begin();
                game.SpriteBatch.Draw(arrow, arrowPos, null, Color.White, rotation, origin, 1.2f, SpriteEffects.None, 0.0f);
                game.SpriteBatch.DrawString(font, distance.ToString("0"), new Vector2(renderTarget.Width / 4f, renderTarget.Height / 2.4f), Color.Black);
                game.SpriteBatch.DrawString(font, height.ToString("0"), new Vector2(renderTarget.Width / 4f, renderTarget.Height / 1.5f), Color.Black);
                game.SpriteBatch.End();
            }

            if (game.currentGameState == gameState.Ended)
            {
                game.SpriteBatch.Begin();
                game.SpriteBatch.DrawString(font2,"GAME", new Vector2(renderTarget.Width / 6f, 0), Color.Black);
                game.SpriteBatch.DrawString(font2, "OVER", new Vector2(renderTarget.Width / 6f, 15), Color.Black);
                game.SpriteBatch.DrawString(font2, "SCORE:", new Vector2(renderTarget.Width / 12f, 30), Color.Black);
                game.SpriteBatch.DrawString(font2, score.ToString("0"), new Vector2(renderTarget.Width / 4f, 44f), Color.Black);
                game.SpriteBatch.End();
            }

            screen = (Texture2D)renderTarget;
            GraphicsDevice.SetRenderTarget(null);
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            Matrix[] transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect beffect in mesh.Effects)
                {
                    beffect.EnableDefaultLighting();
                    beffect.World = transforms[mesh.ParentBone.Index] * GpsWorld;
                    beffect.View = game.ViewMatrix;
                    beffect.Projection = game.ProjectionMatrix;
                }
                mesh.Draw();
            }

            game.Effect.CurrentTechnique = game.Effect.Techniques["TexturedNoShading"];
            game.Effect.Parameters["xView"].SetValue(game.ViewMatrix);
            game.Effect.Parameters["xProjection"].SetValue(game.ProjectionMatrix);
            game.Effect.Parameters["xTexture"].SetValue(screen);
            game.Effect.Parameters["xWorld"].SetValue(screenWorld);

            foreach (EffectPass p in game.Effect.CurrentTechnique.Passes)
            {
                p.Apply();
                GraphicsDevice.DrawUserPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList, vertices, 0, vertices.Length / 3, VertexPositionNormalTexture.VertexDeclaration);
            }
            
            base.Draw(gameTime);
        }

        private void setupVertices()
        {
            vertices = new VertexPositionNormalTexture[6];
            vertices[0] = new VertexPositionNormalTexture(new Vector3(0,0,0), new Vector3(0,1,0), new Vector2(0, 1));
            vertices[1] = new VertexPositionNormalTexture(new Vector3(0, 0, -1.5f), new Vector3(0, 1, 0), new Vector2(0, 0));
            vertices[2] = new VertexPositionNormalTexture(new Vector3(1, 0, -1.5f), new Vector3(0, 1, 0), new Vector2(1, 0));

            vertices[3] = new VertexPositionNormalTexture(new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector2(0, 1));
            vertices[4] = new VertexPositionNormalTexture(new Vector3(1, 0, -1.5f), new Vector3(0, 1, 0), new Vector2(1, 0));
            vertices[5] = new VertexPositionNormalTexture(new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector2(1, 1));
        }

        private void CalculateWorldGPS()
        {
            Matrix positionRotationMatrix = Matrix.CreateTranslation(-game.Player.Position)
                               * game.Player.PlayerBodyRotation
                               * Matrix.CreateTranslation(game.Player.Position);
            Vector3 translation = Vector3.Transform(game.Player.Position + GpsPos,
                                           positionRotationMatrix);

            GpsWorld = Matrix.CreateScale(0.005f * scale) * game.Player.PlayerBodyRotation * Matrix.CreateTranslation(translation);
        }

        private void CalculateWorldScreen()
        {
            Matrix positionRotationMatrix = Matrix.CreateTranslation(-game.Player.Position)
                               * game.Player.PlayerBodyRotation
                               * Matrix.CreateTranslation(game.Player.Position);
            Vector3 translation = Vector3.Transform(game.Player.Position + GpsPos + (new Vector3(-0.08f,0.06f, 0.1f) * scale),
                                           positionRotationMatrix);

            screenWorld = Matrix.CreateScale(0.16f * scale) * game.Player.PlayerBodyRotation * Matrix.CreateTranslation(translation);
        }

        private void CalculateRotation()
        {
            rotation = (-(float)Math.Atan2(-target.Z + game.Player.Position.Z, target.X - game.Player.Position.X)) + game.Player.RotationY + (float)Math.PI /2 ;
        }
    }
}

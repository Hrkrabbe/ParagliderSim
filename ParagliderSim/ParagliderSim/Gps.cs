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
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class Gps : Microsoft.Xna.Framework.DrawableGameComponent
    {
        RenderTarget2D renderTarget;
        Texture2D screen;
        Texture2D arrow;
        Game1 game;
        Matrix screenWorld;
        float rotation = 0.0f;
        Vector2 origin;

        VertexPositionNormalTexture[] vertices;

        public Gps(Game game)
            : base(game)
        {
            this.game = (Game1)game;
        }

        public override void Initialize()
        {
            setupVertices();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            renderTarget = new RenderTarget2D(GraphicsDevice, 32, 32, false, SurfaceFormat.Bgr565, DepthFormat.None);
            arrow = game.Content.Load<Texture2D>(@"Images/arrow");

            origin.X = arrow.Width / 2;
            origin.Y = arrow.Height / 2;
            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            rotation += 0.1f;
            screenWorld = Matrix.Identity * Matrix.CreateTranslation(new Vector3(700,200,-700));

            GraphicsDevice.SetRenderTarget(renderTarget);
            //GraphicsDevice.Clear(Color.White);
            game.SpriteBatch.Begin();
            game.SpriteBatch.Draw(arrow, renderTarget.Bounds, null, Color.White, rotation, origin,SpriteEffects.None,0.0f);
            game.SpriteBatch.End();

            screen = (Texture2D)renderTarget;
            GraphicsDevice.SetRenderTarget(null);
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            
            
            //GraphicsDevice.SetRenderTarget(game.CurrentRenderTarget); 

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
            vertices[1] = new VertexPositionNormalTexture(new Vector3(0, 0, -1), new Vector3(0, 1, 0), new Vector2(0, 0));
            vertices[2] = new VertexPositionNormalTexture(new Vector3(1, 0, -1), new Vector3(0, 1, 0), new Vector2(1, 0));

            vertices[3] = new VertexPositionNormalTexture(new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector2(0, 1));
            vertices[4] = new VertexPositionNormalTexture(new Vector3(1, 0, -1), new Vector3(0, 1, 0), new Vector2(1, 0));
            vertices[5] = new VertexPositionNormalTexture(new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector2(1, 1));
        }

    }
}

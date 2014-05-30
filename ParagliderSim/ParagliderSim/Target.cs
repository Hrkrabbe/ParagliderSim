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

    public class Target : Microsoft.Xna.Framework.DrawableGameComponent
    {
        public Vector3 Position
        {
            get { return position; }
        }

        Game1 game;
        Effect bbEffect;
        Vector3 position;

        //bounce
        Vector3 newPos;
        float bouncingHeight;
        float bouncingSpeed;

        Texture2D texture;
        VertexPositionTexture[] vertices;


        public Target(Game game, Vector3 position)
            : base(game)
        {
            this.game = (Game1)game;
            this.position = position;
            
        }

        public override void Initialize()
        {
            vertices = new VertexPositionTexture[6];
            
            

            base.Initialize();
        }

        protected override void  LoadContent()
        {
            bbEffect = game.Content.Load<Effect>(@"Shader/bbEffect");
            texture = game.Content.Load<Texture2D>(@"Textures/target");

            //bouncing

            newPos = position;
            newPos.Y += 2f;
            bouncingHeight = 2f;
            bouncingSpeed = 0.04f;

            initVertices();

 	        base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            if(newPos.Y > (position.Y + bouncingHeight + 2f) || newPos.Y < (position.Y + 2f))
                bouncingSpeed *= -1f;

            newPos.Y += bouncingSpeed;
            initVertices();

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {

            bbEffect.CurrentTechnique = bbEffect.Techniques["CylBillboard"];
            bbEffect.Parameters["xWorld"].SetValue(Matrix.Identity);
            bbEffect.Parameters["xView"].SetValue(game.ViewMatrix);
            bbEffect.Parameters["xProjection"].SetValue(game.ProjectionMatrix);
            bbEffect.Parameters["xCamPos"].SetValue(game.Player.Position);
            bbEffect.Parameters["xAllowedRotDir"].SetValue(new Vector3(0, 1, 0));
            bbEffect.Parameters["xBillboardTexture"].SetValue(texture);
            //bbEffect.Parameters["FogStart"].SetValue(fogStart);
            //bbEffect.Parameters["FogEnd"].SetValue(fogEnd);
            game.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            foreach (EffectPass pass in bbEffect.CurrentTechnique.Passes)
            {
                game.GraphicsDevice.BlendState = BlendState.Opaque;
                game.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                {
                    pass.Apply();
                    //game.GraphicsDevice.SetVertexBuffer(treeVertexBuffer);
                    //int noVertices = treeVertexBuffer.VertexCount;
                    //int noTriangles = noVertices / 3;
                    game.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList,vertices, 0, vertices.Length / 3);
                }
            }
            game.GraphicsDevice.BlendState = BlendState.Opaque;
        }

        public void initVertices()
        {
            vertices[0] = new VertexPositionTexture(newPos, new Vector2(0, 0));
            vertices[1] = new VertexPositionTexture(newPos, new Vector2(1, 0));
            vertices[2] = new VertexPositionTexture(newPos, new Vector2(1, 1));

            vertices[3] = new VertexPositionTexture(newPos, new Vector2(0, 0));
            vertices[4] = new VertexPositionTexture(newPos, new Vector2(1, 1));
            vertices[5] = new VertexPositionTexture(newPos, new Vector2(0, 1));
        }

    }
}

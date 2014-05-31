using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace ParagliderSim
{
    public class WorldComponent
    {
        Matrix world;
        float scale;
        float rotation;
        Vector3 position;
        Model model;
        BoundingSphere boundingSphere;

        public WorldComponent(Model model, float scale, float rotation, Vector3 position)
        {
            this.model = model;
            this.scale = scale;
            this.rotation = rotation;
            this.position = position;
            world = Matrix.Identity * Matrix.CreateScale(scale) * Matrix.CreateRotationY(rotation) * Matrix.CreateTranslation(position);

            initBoundingSphere();
        }

        protected void initBoundingSphere()
        {
            foreach (ModelMesh mesh in model.Meshes)
            {
                boundingSphere = BoundingSphere.CreateMerged(boundingSphere, mesh.BoundingSphere);
            }
            boundingSphere = boundingSphere.Transform(world);
        }

        public BoundingSphere getBoundingSphere()
        {
            return boundingSphere;
        }

        public void Draw(Matrix viewMatrix, Matrix projectionMatrix)
        {
            // Copy any parent transforms.
            Matrix[] transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);

            // Draw the model. A model can have multiple meshes, so loop.
            foreach (ModelMesh mesh in model.Meshes)
            {
                // This is where the mesh orientation is set, as well 
                // as our camera and projection.
                foreach (BasicEffect effect in mesh.Effects)
                {
                    //effect.EnableDefaultLighting();
                    effect.LightingEnabled = true;
                    effect.DirectionalLight0.Enabled = true;
                    effect.DirectionalLight0.DiffuseColor = Color.Black.ToVector3();
                    effect.DirectionalLight0.Direction = Vector3.Normalize(Vector3.Zero);
                    effect.EmissiveColor = Color.White.ToVector3();

                    effect.World = transforms[mesh.ParentBone.Index] * world;
                    effect.View = viewMatrix;
                    effect.Projection = projectionMatrix;
                }
                // Draw the mesh, using the effects set above.
                mesh.Draw();
            }
        }
    }
}

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
    public class Terrain
    {
        public VertexMultitextured Vertices
        {
            get { return Vertices; }
        }

        GraphicsDevice device;

        VertexMultitextured[] vertices;
        IndexBuffer indexBuffer;
        VertexBuffer vertexBuffer;
        int[] indices;
        private int terrainWidth;
        private int terrainHeight;
        private float[,] heightData;

        float terrainScale;
        Texture2D heightmap;
        Texture2D grassTexture;
        Texture2D sandTexture;
        Texture2D rockTexture;
        Texture2D snowTexture;

        public Terrain(GraphicsDevice device,float terrainScale, Texture2D heightmap, Texture2D grassTexture, Texture2D sandTexture, Texture2D rockTexture, Texture2D snowTexture)
        {
            this.device = device;
            this.heightmap = heightmap;
            this.grassTexture = grassTexture;
            this.sandTexture = sandTexture;
            this.rockTexture = rockTexture;
            this.snowTexture = snowTexture;
            this.terrainScale = terrainScale;

            LoadHeightData(heightmap);
            SetUpVertices();
            SetUpIndices();
            CalculateNormals();
            CopyToBuffers();
        }

        #region setup
        private void SetUpVertices()
        {
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;
            for (int x = 0; x < terrainWidth; x++)
            {
                for (int y = 0; y < terrainHeight; y++)
                {
                    if (heightData[x, y] < minHeight)
                        minHeight = heightData[x, y];
                    if (heightData[x, y] > maxHeight)
                        maxHeight = heightData[x, y];
                }
            }


            vertices = new VertexMultitextured[terrainWidth * terrainHeight];
            for (int x = 0; x < terrainWidth; x++)
            {
                for (int y = 0; y < terrainHeight; y++)
                {
                    vertices[x + y * terrainWidth].Position = new Vector3(x, heightData[x, y], -y) * terrainScale;
                    vertices[x + y * terrainWidth].TextureCoordinate.X = (float)x / 30.0f;
                    vertices[x + y * terrainWidth].TextureCoordinate.Y = (float)y / 30.0f;

                    vertices[x + y * terrainWidth].TexWeights.X = MathHelper.Clamp(1.0f - Math.Abs(heightData[x, y] - 0) / 8.0f, 0, 1);
                    vertices[x + y * terrainWidth].TexWeights.Y = MathHelper.Clamp(1.0f - Math.Abs(heightData[x, y] - 18) / 12.0f, 0, 1);
                    vertices[x + y * terrainWidth].TexWeights.Z = MathHelper.Clamp(1.0f - Math.Abs(heightData[x, y] - 30) / 12.0f, 0, 1);
                    vertices[x + y * terrainWidth].TexWeights.W = MathHelper.Clamp(1.0f - Math.Abs(heightData[x, y] - 60) / 24.0f, 0, 1);

                    float total = vertices[x + y * terrainWidth].TexWeights.X;
                    total += vertices[x + y * terrainWidth].TexWeights.Y;
                    total += vertices[x + y * terrainWidth].TexWeights.Z;
                    total += vertices[x + y * terrainWidth].TexWeights.W;

                    vertices[x + y * terrainWidth].TexWeights.X /= total;
                    vertices[x + y * terrainWidth].TexWeights.Y /= total;
                    vertices[x + y * terrainWidth].TexWeights.Z /= total;
                    vertices[x + y * terrainWidth].TexWeights.W /= total;
                }
            }
        }

        private void SetUpIndices()
        {
            indices = new int[(terrainWidth - 1) * (terrainHeight - 1) * 6];
            int counter = 0;
            for (int y = 0; y < terrainHeight - 1; y++)
            {
                for (int x = 0; x < terrainWidth - 1; x++)
                {
                    int lowerLeft = x + y * terrainWidth;
                    int lowerRight = (x + 1) + y * terrainWidth;
                    int topLeft = x + (y + 1) * terrainWidth;
                    int topRight = (x + 1) + (y + 1) * terrainWidth;

                    indices[counter++] = topLeft;
                    indices[counter++] = lowerRight;
                    indices[counter++] = lowerLeft;

                    indices[counter++] = topLeft;
                    indices[counter++] = topRight;
                    indices[counter++] = lowerRight;
                }
            }
        }

        private void CalculateNormals()
        {
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Normal = new Vector3(0, 0, 0);

            for (int i = 0; i < indices.Length / 3; i++)
            {
                int index1 = indices[i * 3];
                int index2 = indices[i * 3 + 1];
                int index3 = indices[i * 3 + 2];

                Vector3 side1 = vertices[index1].Position - vertices[index3].Position;
                Vector3 side2 = vertices[index1].Position - vertices[index2].Position;
                Vector3 normal = Vector3.Cross(side1, side2);

                vertices[index1].Normal += normal;
                vertices[index2].Normal += normal;
                vertices[index3].Normal += normal;
            }

            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Normal.Normalize();

        }


        private void LoadHeightData(Texture2D heightMap)
        {
            terrainWidth = heightMap.Width;
            terrainHeight = heightMap.Height;

            Color[] heightMapColors = new Color[terrainWidth * terrainHeight];
            heightMap.GetData(heightMapColors);

            heightData = new float[terrainWidth, terrainHeight];
            for (int x = 0; x < terrainWidth; x++)
                for (int y = 0; y < terrainHeight; y++)
                    heightData[x, y] = heightMapColors[x + y * terrainWidth].R / 5.0f;
        }

        private void CopyToBuffers()
        {
            vertexBuffer = new VertexBuffer(device, VertexMultitextured.VertexDeclaration, vertices.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices);

            indexBuffer = new IndexBuffer(device, typeof(int), indices.Length, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices);
        }
        #endregion

        //Returns a plane for collision testing
        public Plane getPlane(Vector3 position)
        {
            VertexMultitextured botLeft, upLeft, botRight, upRight;
            int x, y;
            x = (int)Math.Floor(position.X / terrainScale);
            y = (int)Math.Floor(-position.Z / terrainScale);

            botLeft = vertices[x + y * terrainWidth];
            upLeft = vertices[x + (y + 1) * terrainWidth];
            botRight = vertices[(x + 1) + y * terrainWidth];
            upRight = vertices[(x + 1) + (y + 1) * terrainWidth];

            if ((position.X / terrainScale) - x > (-position.Z / terrainScale) - y)
                return new Plane(botLeft.Position, upRight.Position, botRight.Position);
            else
                return new Plane(botLeft.Position, upLeft.Position, botRight.Position);
        }

        public List<Plane> getPlanes(Vector3 position)
        {
            List<Plane> planes = new List<Plane>();
            VertexMultitextured botLeft, upLeft, botRight, upRight;
            int x, y;
            x = (int)Math.Floor(position.X / terrainScale);
            y = (int)Math.Floor(-position.Z / terrainScale);

            botLeft = vertices[x + y * terrainWidth];
            upLeft = vertices[x + (y + 1) * terrainWidth];
            botRight = vertices[(x + 1) + y * terrainWidth];
            upRight = vertices[(x + 1) + (y + 1) * terrainWidth];

            planes.Add(new Plane(botLeft.Position, upRight.Position, botRight.Position));
            planes.Add(new Plane(botLeft.Position, upLeft.Position, botRight.Position));

            return planes;
        }

        //Test method for drawing current plane
        public VertexPositionColor[] getCollisionVertices(Vector3 position)
        {
            VertexMultitextured botLeft, upLeft, botRight, upRight;
            int x, y;
            x = (int)Math.Floor(position.X / terrainScale);
            y = (int)Math.Floor(-position.Z / terrainScale);

            botLeft = vertices[x + y * terrainWidth];
            upLeft = vertices[x + (y + 1) * terrainWidth];
            botRight = vertices[(x + 1) + y * terrainWidth];
            upRight = vertices[(x + 1) + (y + 1) * terrainWidth];

            VertexPositionColor[] vs = new VertexPositionColor[3];

            if ((position.X / terrainScale) - x > (-position.Z / terrainScale) - y)
            {
                vs[0].Position = botLeft.Position;
                vs[0].Color = Color.Red;
                vs[1].Position = upRight.Position;
                vs[1].Color = Color.Red;
                vs[2].Position = botRight.Position;
                vs[2].Color = Color.Red;
            }
            else
            {
                vs[0].Position = botLeft.Position;
                vs[0].Color = Color.Red;
                vs[1].Position = upLeft.Position;
                vs[1].Color = Color.Red;
                vs[2].Position = botRight.Position;
                vs[2].Color = Color.Red;
            }

            return vs;
        }

        public float getWidthUnits()
        {
            return terrainWidth * terrainScale;
        }

        public float getHeightUnits()
        {
            return terrainHeight * terrainScale;
        }


        public void Draw(Matrix viewMatrix, Matrix projectionMatrix, Effect effect, Vector3 lightDirection)
        {
            //device.Clear(Color.Black);
            RasterizerState rs = new RasterizerState();
            rs.CullMode = CullMode.None;
            rs.FillMode = FillMode.Solid;
            rs.DepthBias = 0;
            device.RasterizerState = rs;
            device.DepthStencilState = DepthStencilState.Default;
            //device.SamplerStates[0] = SamplerState.LinearWrap;
            device.BlendState = BlendState.Opaque;

            effect.CurrentTechnique = effect.Techniques["MultiTextured"];
            lightDirection.Normalize();
            effect.Parameters["xLightDirection"].SetValue(lightDirection);
            effect.Parameters["xAmbient"].SetValue(0.3f);
            effect.Parameters["xEnableLighting"].SetValue(true);

            effect.Parameters["xTexture0"].SetValue(sandTexture);
            effect.Parameters["xTexture1"].SetValue(grassTexture);
            effect.Parameters["xTexture2"].SetValue(rockTexture);
            effect.Parameters["xTexture3"].SetValue(snowTexture);

            //Matrix worldMatrix = Matrix.CreateTranslation((-terrainWidth*terrainScale) / 2.0f, -terrainBot * terrainScale, (terrainHeight*terrainScale) / 2.0f);
            Matrix worldMatrix = Matrix.Identity * Matrix.CreateTranslation(0, 0, 0);
            effect.Parameters["xWorld"].SetValue(worldMatrix);
            effect.Parameters["xView"].SetValue(viewMatrix);
            effect.Parameters["xProjection"].SetValue(projectionMatrix);

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.Indices = indexBuffer;
                device.SetVertexBuffer(vertexBuffer);
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertices.Length, 0, indices.Length / 3);
            }
        }
    }
}

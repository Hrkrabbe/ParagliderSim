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
using LTreesLibrary.Trees;
using LTreesLibrary.Trees.Wind;
using LTreesLibrary.Pipeline;

namespace ParagliderSim
{
    public class Terrain
    {
        public VertexMultitextured Vertices
        {           
            get { return Vertices; }
        }

        GraphicsDevice device;

        Game1 game;
        VertexMultitextured[] vertices;
        IndexBuffer indexBuffer;
        VertexBuffer vertexBuffer;
        int[] indices;
        private int terrainWidth;
        private int terrainHeight;
        private float[,] heightData;
        private float[,] updraftData;

        float terrainScale;
        Texture2D heightmap;
        Texture2D grassTexture;
        Texture2D sandTexture;
        Texture2D rockTexture;
        Texture2D snowTexture;
        Texture2D treeMap;
        Texture2D treeTexture;
        Texture2D dirtTexture;
        //Trees

        String profileAssetFormat = "Trees/{0}";

        String[] profileNames = new String[]
        {
            "Birch",
            "Pine",
            "Gardenwood",
            "Graywood",
            "Rug",
            "Willow",
        };
        TreeProfile[] profiles;

        TreeLineMesh linemesh;

        int currentTree = 0;

        SimpleTree tree;

        WindStrengthSin wind;
        TreeWindAnimator animator;
        List<Vector3> treeList = new List<Vector3>();
        VertexBuffer treeVertexBuffer;
        VertexDeclaration treeVertexDeclaration;
        Effect bbEffect;

        //Fog
        float fogStart;
        float fogEnd;

        //grass
        List<Vector3> grassList = new List<Vector3>();
        VertexBuffer grassVertexBuffer;
        VertexDeclaration grassVertexDeclaration;

        public Terrain(Game1 game, GraphicsDevice device,float terrainScale, float fogStart, float fogEnd, Texture2D heightmap, Texture2D grassTexture, Texture2D sandTexture, Texture2D rockTexture, Texture2D snowTexture, Texture2D treeMap, Texture2D grassMap, Texture2D treeTexture, ContentManager Content, Texture2D updraftMap, Texture2D dirtTexture)


        {
            this.game = game;
            this.device = device;
            this.heightmap = heightmap;
            this.grassTexture = grassTexture;
            this.sandTexture = sandTexture;
            this.rockTexture = rockTexture;
            this.snowTexture = snowTexture;
            this.terrainScale = terrainScale;
            this.fogStart = fogStart;
            this.fogEnd = fogEnd;
            this.treeTexture = treeTexture;
            this.dirtTexture = dirtTexture;
            LoadHeightData(heightmap);
            LoadUpdraftData(updraftMap);
            SetUpVertices();
            SetUpIndices();
            CalculateNormals();
            CopyToBuffers();
            
            wind = new WindStrengthSin();
            animator = new TreeWindAnimator(wind);
            LoadTreeGenerators(Content);
            NewTree();
            
            List<Vector3> treeList = GenerateTreePositions(treeMap, vertices);
            CreateBillboardVerticesFromList(treeList);
            //List<Vector3> grassList = GenerateTreePositions(grassMap, vertices);
            bbEffect = Content.Load<Effect>(@"Shader/bbEffect");


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

                    vertices[x + y * terrainWidth].TexWeights2.X = MathHelper.Clamp(Math.Abs(updraftData[x, y]) / 8.0f, 0, 4);
                    //vertices[x + y * terrainWidth].TexWeights2.X = 0;

                    float total = vertices[x + y * terrainWidth].TexWeights.X;
                    total += vertices[x + y * terrainWidth].TexWeights.Y;
                    total += vertices[x + y * terrainWidth].TexWeights.Z;
                    total += vertices[x + y * terrainWidth].TexWeights.W;

                    total += vertices[x + y * terrainWidth].TexWeights2.X;

                    vertices[x + y * terrainWidth].TexWeights.X /= total;
                    vertices[x + y * terrainWidth].TexWeights.Y /= total;
                    vertices[x + y * terrainWidth].TexWeights.Z /= total;
                    vertices[x + y * terrainWidth].TexWeights.W /= total;

                    vertices[x + y * terrainWidth].TexWeights2.X /= total;
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

        //Testmetode for å sjekke hvilket triangel kollisjon sjekkes med
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

        #region Trees
        void LoadTreeGenerators(ContentManager Content)
        {

            profiles = new TreeProfile[profileNames.Length];
            for (int i = 0; i < profiles.Length; i++)
            {
                profiles[i] = Content.Load<TreeProfile>(String.Format(profileAssetFormat, profileNames[i]));
            }
        }

        void NewTree()
        {
            // Generates a new tree using the currently selected tree profile
            // We call TreeProfile.GenerateSimpleTree() which does three things for us:
            // 1. Generates a tree skeleton
            // 2. Creates a mesh for the branches
            // 3. Creates a particle cloud (TreeLeafCloud) for the leaves
            // The line mesh is just for testing and debugging
            tree = profiles[1].GenerateSimpleTree();
            linemesh = new TreeLineMesh(device, tree.Skeleton);
        }


        private List<Vector3> GenerateTreePositions(VertexMultitextured[] terrainVertices)
        {


            treeList.Add(terrainVertices[3310].Position);
            treeList.Add(terrainVertices[3315].Position);
            treeList.Add(terrainVertices[6000].Position);
            treeList.Add(terrainVertices[7000].Position);

            return treeList;
        }

        private List<Vector3> GenerateTreePositions(Texture2D treeMap, VertexMultitextured[] terrainVertices)
        {
            Color[] treeMapColors = new Color[treeMap.Width * treeMap.Height];
            treeMap.GetData(treeMapColors);

            int[,] noiseData = new int[treeMap.Width, treeMap.Height];
            for (int x = 0; x < treeMap.Width; x++)
                for (int y = 0; y < treeMap.Height; y++)
                    noiseData[x, y] = treeMapColors[x + y * treeMap.Height].R;


            //List<Vector3> treeList = new List<Vector3>(); 
            Random random = new Random();

            for (int x = 0; x < terrainWidth; x++)
            {
                for (int y = 0; y < terrainHeight; y++)
                {
                    float terrHeight = heightData[x, y];
                    if ((terrHeight > 7) && (terrHeight < 250))
                    {
                        float flatness = Vector3.Dot(terrainVertices[x + y * (int)terrainWidth].Normal, new Vector3(0, 1, 0));
                        float minFlatness = (float)Math.Cos(MathHelper.ToRadians(15));
                        if (flatness > minFlatness)
                        {
                            float relx = (float)x / (float)terrainWidth;
                            float rely = (float)y / (float)terrainHeight;

                            float noiseValueAtCurrentPosition = noiseData[(int)(relx * treeMap.Width), (int)(rely * treeMap.Height)];
                            float treeDensity;
                            if (noiseValueAtCurrentPosition > 200)
                                treeDensity = 4;
                            else if (noiseValueAtCurrentPosition > 150)
                                treeDensity = 2;
                            else if (noiseValueAtCurrentPosition > 100)
                                treeDensity = 1;
                            else
                                treeDensity = 0;

                            for (int currDetail = 0; currDetail < treeDensity; currDetail++)
                            {
                                float rand1 = (float)random.Next(1000) / 1000.0f;
                                float rand2 = (float)random.Next(1000) / 1000.0f;
                                Vector3 treePos = new Vector3(((float)x - rand1)*terrainScale, 0, (-(float)y - rand2)*terrainScale);
                                treePos.Y = heightData[x, y]*terrainScale;
                                treeList.Add(treePos);
                            }
                        }
                    }
                }
            }
            Vector3 testTree = new Vector3(833.0f, 214.0f, -625.0f);
            treeList.Add(testTree);
            return treeList;
        }

        private void CreateBillboardVerticesFromList(List<Vector3> treeList)
        {
            VertexPositionTexture[] billboardVertices = new VertexPositionTexture[treeList.Count * 6];
            int i = 0;
            foreach (Vector3 currentV3 in treeList)
            {

                billboardVertices[i++] = new VertexPositionTexture(currentV3, new Vector2(0, 0));
                billboardVertices[i++] = new VertexPositionTexture(currentV3, new Vector2(1, 0));
                billboardVertices[i++] = new VertexPositionTexture(currentV3, new Vector2(1, 1));

                billboardVertices[i++] = new VertexPositionTexture(currentV3, new Vector2(0, 0));
                billboardVertices[i++] = new VertexPositionTexture(currentV3, new Vector2(1, 1));
                billboardVertices[i++] = new VertexPositionTexture(currentV3, new Vector2(0, 1));
            }

            VertexDeclaration vertexDeclaration = VertexPositionTexture.VertexDeclaration;

            treeVertexBuffer = new VertexBuffer(device, vertexDeclaration, billboardVertices.Length, BufferUsage.WriteOnly);
            treeVertexBuffer.SetData(billboardVertices);
            treeVertexDeclaration = vertexDeclaration;
        }

        public void DrawTrees(GameTime gameTime, Matrix viewMatrix, Matrix projectionMatrix)
        {
            //..

            Matrix world = Matrix.Identity;
            //Matrix scale = Matrix.CreateScale(0.0015f);
            Matrix scale = Matrix.CreateScale(0.0015f);
          //  Matrix translation = Matrix.CreateTranslation(840, 195, -700);
            //Matrix translation2 = Matrix.CreateTranslation(-3.0f, 0.0f, 0.0f);


            foreach (Vector3 currentV3 in treeList)
            {
                //device.BlendState = BlendState.AlphaBlend;
                Vector2 distance = new Vector2(currentV3.X - game.Player.Position.X, currentV3.Z - game.Player.Position.Z);
                Matrix x = Matrix.CreateTranslation(currentV3);
                if (distance.Length() < 100.0f)
                {
                    
                    device.BlendState = BlendState.Opaque;
                    device.DepthStencilState = DepthStencilState.Default;
                    tree.DrawTrunk(world * scale * x, viewMatrix, projectionMatrix);
                    tree.DrawLeaves(world * scale * x, viewMatrix, projectionMatrix);
                    animator.Animate(tree.Skeleton, tree.AnimationState, gameTime);
                }
                //else
                    //tree.DrawLeaves(world * scale * x, viewMatrix, projectionMatrix);
                //device.BlendState = BlendState.Opaque;
            }

            
        }

        public void DrawBillboards(Matrix ViewMatrix, Matrix projectionMatrix)
        {
 
            bbEffect.CurrentTechnique = bbEffect.Techniques["CylBillboard"];
            bbEffect.Parameters["xWorld"].SetValue(Matrix.Identity);
            bbEffect.Parameters["xView"].SetValue(ViewMatrix);
            bbEffect.Parameters["xProjection"].SetValue(projectionMatrix);
            bbEffect.Parameters["xCamPos"].SetValue(game.Player.Position);
            bbEffect.Parameters["xAllowedRotDir"].SetValue(new Vector3(0, 1, 0));
            bbEffect.Parameters["xBillboardTexture"].SetValue(treeTexture);
            bbEffect.Parameters["FogStart"].SetValue(fogStart);
            bbEffect.Parameters["FogEnd"].SetValue(fogEnd);
            device.BlendState = BlendState.AlphaBlend;
            foreach (EffectPass pass in bbEffect.CurrentTechnique.Passes)
            {
                device.BlendState = BlendState.Opaque;
                device.DepthStencilState = DepthStencilState.Default;
                {
                    pass.Apply();
                    device.SetVertexBuffer(treeVertexBuffer);
                    int noVertices = treeVertexBuffer.VertexCount;
                    int noTriangles = noVertices / 3;
                    device.DrawPrimitives(PrimitiveType.TriangleList, 0, noTriangles);
                }
            }
            device.BlendState = BlendState.Opaque;
        }

        #endregion

        #region Grass

        private List<Vector3> GenerateGrassPositions(Texture2D grassMap, VertexMultitextured[] terrainVertices)
        {
            Color[] grassMapColors = new Color[grassMap.Width * grassMap.Height];
            grassMap.GetData(grassMapColors);

            int[,] noiseData = new int[grassMap.Width, grassMap.Height];
            for (int x = 0; x < grassMap.Width; x++)
                for (int y = 0; y < grassMap.Height; y++)
                    noiseData[x, y] = grassMapColors[y + x * grassMap.Height].R;


            //List<Vector3> treeList = new List<Vector3>(); 
            Random random = new Random();

            for (int x = 0; x < terrainWidth; x++)
            {
                for (int y = 0; y < terrainHeight; y++)
                {
                    float terrHeight = heightData[x, y];
                    if ((terrHeight > 7) && (terrHeight < 250))
                    {
                        float flatness = Vector3.Dot(terrainVertices[x + y * (int)terrainWidth].Normal, new Vector3(0, 1, 0));
                        float minFlatness = (float)Math.Cos(MathHelper.ToRadians(10));
                        if (flatness > minFlatness)
                        {
                            float relx = (float)x / (float)terrainWidth;
                            float rely = (float)y / (float)terrainHeight;

                            float noiseValueAtCurrentPosition = noiseData[(int)(relx * treeMap.Width), (int)(rely * grassMap.Height)];
                            float grassDensity;
                            if (noiseValueAtCurrentPosition > 200)
                                grassDensity = 8;
                            else if (noiseValueAtCurrentPosition > 150)
                                grassDensity = 4;
                            else if (noiseValueAtCurrentPosition > 100)
                                grassDensity = 1;
                            else
                                grassDensity = 0;

                            for (int currDetail = 0; currDetail < grassDensity; currDetail++)
                            {
                                float rand1 = (float)random.Next(1000) / 1000.0f;
                                float rand2 = (float)random.Next(1000) / 1000.0f;
                                Vector3 grassPos = new Vector3(((float)x - rand1) * terrainScale, 0, (-(float)y - rand2) * terrainScale);
                                grassPos.Y = heightData[x, y] * terrainScale;
                                grassList.Add(grassPos);
                            }
                        }
                    }
                }
            }


            return treeList;
        }


        #endregion

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
            effect.Parameters["xTexture4"].SetValue(dirtTexture);

            //Matrix worldMatrix = Matrix.CreateTranslation((-terrainWidth*terrainScale) / 2.0f, -terrainBot * terrainScale, (terrainHeight*terrainScale) / 2.0f);
            Matrix worldMatrix = Matrix.Identity * Matrix.CreateTranslation(0, 0, 0);
            effect.Parameters["xWorld"].SetValue(worldMatrix);
            effect.Parameters["xView"].SetValue(viewMatrix);
            effect.Parameters["xProjection"].SetValue(projectionMatrix);
            effect.Parameters["xCamPos"].SetValue(game.Player.Position);
            effect.Parameters["FogStart"].SetValue(fogStart);
            effect.Parameters["FogEnd"].SetValue(fogEnd);

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.Indices = indexBuffer;
                device.SetVertexBuffer(vertexBuffer);
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertices.Length, 0, indices.Length / 3);
            }
        }

        private void LoadUpdraftData(Texture2D updraftMap)
        {

            Color[] updraftMapColors = new Color[terrainWidth * terrainHeight];
            updraftMap.GetData(updraftMapColors);

            updraftData = new float[terrainWidth, terrainHeight];
            for (int x = 0; x < terrainWidth; x++)
                for (int y = 0; y < terrainHeight; y++)
                    updraftData[x, y] = updraftMapColors[x + y * terrainWidth].R / 5.0f;
        }

        public float getUpdraft(Vector3 position)
        {
            int x, y;
            x = (int)Math.Floor(position.X / terrainScale);
            y = (int)Math.Floor(-position.Z / terrainScale);

            if ((x > 0) && (x < terrainWidth) && (y > 0) && (y < terrainHeight))
                return updraftData[x, y] * 0.005f;
            else
                return 0f;
        }
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

using System.Collections.Generic;

using System.IO;
using System.Linq;
using System.Numerics;

using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Vector4 = Microsoft.Xna.Framework.Vector4;

using System.Threading;
namespace PixelMidpointDisplacement
{

    public class WorldContext
    {
        /*
         * A class that is passed to all gametime objects. This class contains the arrays that define the world, scaling and any other contextual information required by objects
         * 
         * ==========================================
         * World Context Settings:
         * 
         * - initial pixels per block
         * - pixels per block after world generation
         */

        /*
         *  The worlrd array is an integer containing block data as follows: 
         *  2 bytes store block ID (More than we really need, but having 2^16 possible IDs will be useful)
         *  2 bytes can store individual data such as texture variation (grass for example can use 3 bits to store 
         */

        public Block[,] worldArray { get; set; }
        public int[,] backgroundArray { get; set; }
        public int[,] intWorldArray { get; set; }
        public int[] surfaceHeight { get; set; } //The index is the x value, the value of the array is the actual height of the surface

        public List<(int x, int y)> surfaceBlocks { get; set; }

        public Dictionary<(int x, int y), Block> exposedBlocks = new Dictionary<(int x, int y), Block>();//This list contains all the blocks that are exposed to air, and hence would cast shadows.

        public List<Biome> surfaceWorldBiomeList = new List<Biome>();
        public List<SubterraneanBiome> subterraneanWorldBiomeList = new List<SubterraneanBiome>();
        public int[,] lightArray { get; set; }
        public int pixelsPerBlock { get; set; } = 4; //Overwritten by the settings file

        public int pixelsPerBlockAfterGeneration;

        public int applicationWidth;
        public int applicationHeight;

        public Sun sun;

        Dictionary<blockIDs, int> intFromBlockID = Enum.GetValues(typeof(blockIDs)).Cast<blockIDs>().ToDictionary(e => e, e => (int)e);
        Dictionary<int, blockIDs> blockIDFromInt = Enum.GetValues(typeof(blockIDs)).Cast<blockIDs>().ToDictionary(e => (int)e, e => e);
        Dictionary<blockIDs, Block> blockFromID = new Dictionary<blockIDs, Block>();

        public (int x, int y) screenSpaceOffset { get; set; }

        public List<PhysicsObject> physicsObjects = new List<PhysicsObject>();

        public EngineController engineController;

        public AnimationController animationController;

        public Player player;

        public string runtimePath { get; set; }

        public WorldContext(EngineController engineController)
        {
            this.engineController = engineController;

            runtimePath = AppDomain.CurrentDomain.BaseDirectory;

            //Load settings from file
            loadSettings();
            generateBlockReferences();

        }

        private void loadSettings()
        {
            StreamReader sr = new StreamReader(runtimePath + "Settings\\WorldContextSettings.txt");
            sr.ReadLine();
            pixelsPerBlock = Convert.ToInt32(sr.ReadLine());
            sr.ReadLine();
            pixelsPerBlockAfterGeneration = Convert.ToInt32(sr.ReadLine());
        }

        public void generateWorld((int width, int height) worldDimensions)
        {

            worldArray = new Block[worldDimensions.width, worldDimensions.height];
            backgroundArray = new int[worldDimensions.width, worldDimensions.height];

            intWorldArray = new int[worldDimensions.width, worldDimensions.height];

            lightArray = new int[worldDimensions.width, worldDimensions.height];

            surfaceHeight = new int[worldDimensions.width];

            surfaceBlocks = new List<(int x, int y)>();




            WorldGenerator worldGenerator = new WorldGenerator(this);

            intWorldArray = worldGenerator.generateWorld(worldDimensions);
            backgroundArray = worldGenerator.getBackgroundArray();
            surfaceHeight = worldGenerator.getSurfaceHeight();
            surfaceBlocks = worldGenerator.getSurfaceBlocks();

            surfaceWorldBiomeList = worldGenerator.biomeList;

            lightArray = engineController.lightingSystem.initialiseLight(worldDimensions, surfaceHeight);
            engineController.lightingSystem.generateSunlight(intWorldArray, surfaceHeight);
            engineController.lightingSystem.calculateSurfaceLight(intWorldArray, surfaceBlocks);


            for (int x = 0; x < worldArray.GetLength(0); x++)
            {
                for (int y = 0; y < worldArray.GetLength(1); y++)
                {
                    generateInstanceFromID(intWorldArray, blockIDFromInt[intWorldArray[x, y]], x, y);
                }
            }

            for (int x = 0; x < worldArray.GetLength(0); x++)
            {
                for (int y = 0; y < worldArray.GetLength(1); y++)
                {
                    addBlockToDictionaryIfExposedToAir(worldArray, x, y);
                }
            }

            updatePixelsPerBlock(pixelsPerBlockAfterGeneration);

            sun = new Sun(this);

            updateSurfaceHeight();

            player.setSpawn((int)player.x, pixelsPerBlock * (surfaceHeight[(int)Math.Floor(player.x / pixelsPerBlock)] - 3));
            player.respawn();

        }

        public void updateSurfaceHeight()
        {
            for (int x = 0; x < worldArray.GetLength(0); x++)
            {
                int y = surfaceHeight[x];
                while (worldArray[x, y].isBlockTransparent || worldArray[x, y].ID == (int)blockIDs.air)
                {
                    y += 1;
                }
                surfaceHeight[x] = y;
            }
        }
        public void setApplicationDimensions(int width, int height)
        {
            applicationHeight = height;
            applicationWidth = width;
        }

        public void generateInstanceFromID(int[,] intArray, blockIDs ID, int x, int y)
        {
            if (ID == blockIDs.air || ID == blockIDs.stone || ID == blockIDs.dirt)
            {
                worldArray[x, y] = new Block(blockFromID[ID]);
            }
            else if (ID == blockIDs.grass)
            {
                worldArray[x, y] = new GrassBlock(blockFromID[ID]);
            }
            else if (ID == blockIDs.torch)
            {
                worldArray[x, y] = new TorchBlock(blockFromID[ID]);
            }
            else if (ID == blockIDs.chest)
            {
                worldArray[x, y] = new ChestBlock(blockFromID[ID]);
            }
            else if (ID == blockIDs.water)
            {
                worldArray[x, y] = new FluidBlock(blockFromID[ID]);
            }
            else if (ID == blockIDs.ironOre)
            {
                worldArray[x, y] = new OreBlock(blockFromID[ID]);
            }
            else if (ID == blockIDs.treeTrunk)
            {
                worldArray[x, y] = new TreeBlock(blockFromID[ID]);
            }
            else if (ID == blockIDs.leaves)
            {
                worldArray[x, y] = new LeafBlock(blockFromID[ID]);
            }
            else if (ID == blockIDs.semiLeaves)
            {
                worldArray[x, y] = new SemiLeafBlock(blockFromID[ID]);
            }
            else if (ID == blockIDs.bush)
            {
                worldArray[x, y] = new BushBlock(blockFromID[ID]);
            }
            else if (ID == blockIDs.bigBush)
            {
                worldArray[x, y] = new BigBushBlock(blockFromID[ID]);
            }

            worldArray[x, y].setupInitialData(this, intArray, (x, y));
        }

        public double locationToShaderSpace(int value, Vector2 axis)
        {
            if (axis.X > 0)
            {
                return ((double)value * pixelsPerBlock + screenSpaceOffset.x) / (double)applicationWidth;

            }
            else
            {
                return ((double)value * pixelsPerBlock + screenSpaceOffset.y) / (double)applicationHeight;
            }
        }
        public int findLowestSurfaceHeightOnScreen()
        {
            int y = 0;
            for (int x = -screenSpaceOffset.x / pixelsPerBlock; x < (-screenSpaceOffset.x + applicationWidth) / pixelsPerBlock; x++)
            {
                if (surfaceHeight[x] > y) { y = surfaceHeight[x]; }
            }
            return y;
        }

        public int findFirstSurfaceHeightVisibleFromRight()
        {
            for (int x = -screenSpaceOffset.x / pixelsPerBlock; x < (-screenSpaceOffset.x + applicationWidth) / pixelsPerBlock; x++)
            {
                if (surfaceHeight[x] * pixelsPerBlock + screenSpaceOffset.y >= 0 && surfaceHeight[x] * pixelsPerBlock + screenSpaceOffset.y < applicationHeight) { return x; }
            }
            return -1;
        }

        public int findFirstSurfaceHeightVisibleFromLeft()
        {
            for (int x = (-screenSpaceOffset.x + applicationWidth) / pixelsPerBlock; x < (-screenSpaceOffset.x) / pixelsPerBlock; x--)
            {
                if (surfaceHeight[x] * pixelsPerBlock + screenSpaceOffset.y >= 0 && surfaceHeight[x] * pixelsPerBlock + screenSpaceOffset.y < applicationHeight) { return x; }
            }
            return -1;
        }

        public void addBlockToDictionaryIfExposedToAir(int[,] blockArray, int x, int y)
        {
            if (x > 0 && y > 0 && x < worldArray.GetLength(0) - 1 && y < worldArray.GetLength(1) - 1)
            {

                if ((blockArray[x - 1, y] == (int)blockIDs.air || blockArray[x + 1, y] == (int)blockIDs.air || blockArray[x, y - 1] == (int)blockIDs.air || blockArray[x, y + 1] == (int)blockIDs.air ||
                    blockFromID[(blockIDs)blockArray[x - 1, y]].isBlockTransparent || blockFromID[(blockIDs)blockArray[x + 1, y]].isBlockTransparent || blockFromID[(blockIDs)blockArray[x, y - 1]].isBlockTransparent || blockFromID[(blockIDs)blockArray[x, y + 1]].isBlockTransparent) && blockArray[x, y] != (int)blockIDs.air) //Then it is exposed to air
                {
                    if (blockArray[x, y] != (int)blockIDs.torch && blockArray[x, y] != (int)blockIDs.chest)
                    {
                        exposedBlocks.Add((x, y), worldArray[x, y]);
                        worldArray[x, y].setupFaceVertices(calculateExposedFaces(blockArray, x, y));
                    }
                }
            }
        }

        /*
         * This is why blocks that are transparent are still blocking other blocks from casing shadows that are below them!
         
         */
        public Vector4 calculateExposedFaces(int[,] blockArray, int x, int y)
        {
            return new Vector4(Convert.ToInt32((blockArray[x, y - 1] == (int)blockIDs.air)), Convert.ToInt32(blockArray[x + 1, y] == (int)blockIDs.air), Convert.ToInt32(blockArray[x, y + 1] == (int)blockIDs.air), Convert.ToInt32(blockArray[x - 1, y] == (int)blockIDs.air));
        }

        public void addBlockToDictionaryIfExposedToAir(Block[,] blockArray, int x, int y)
        {
            if (x > 0 && y > 0 && x < worldArray.GetLength(0) - 1 && y < worldArray.GetLength(1) - 1)
            {
                bool isBlockExposedToAir = (blockArray[x - 1, y].ID == (int)blockIDs.air || blockArray[x + 1, y].ID == (int)blockIDs.air || blockArray[x, y - 1].ID == (int)blockIDs.air || blockArray[x, y + 1].ID == (int)blockIDs.air) && blockArray[x, y].ID != (int)blockIDs.air;
                bool isBlockExposedToATransparentBlock = (blockArray[x - 1, y].isBlockTransparent || blockArray[x + 1, y].isBlockTransparent || blockArray[x, y - 1].isBlockTransparent || blockArray[x, y + 1].isBlockTransparent) && blockArray[x, y].ID != (int)blockIDs.air;
                if (isBlockExposedToAir || isBlockExposedToATransparentBlock)
                {
                    if (!blockArray[x, y].isBlockTransparent)
                    {
                        if (!exposedBlocks.ContainsKey((x, y)))
                        {
                            exposedBlocks.Add((x, y), worldArray[x, y]);
                            worldArray[x, y].setupFaceVertices(calculateExposedFaces(blockArray, x, y));
                        }
                        else
                        {
                            worldArray[x, y].setupFaceVertices(calculateExposedFaces(blockArray, x, y));
                        }
                    }
                }
            }
        }

        public Vector4 calculateExposedFaces(Block[,] blockArray, int x, int y)
        {
            return new Vector4(Convert.ToInt32((blockArray[x, y - 1].ID == (int)blockIDs.air) || blockArray[x, y - 1].isBlockTransparent), Convert.ToInt32(blockArray[x + 1, y].ID == (int)blockIDs.air || blockArray[x + 1, y].isBlockTransparent), Convert.ToInt32(blockArray[x, y + 1].ID == (int)blockIDs.air || blockArray[x, y + 1].isBlockTransparent), Convert.ToInt32(blockArray[x - 1, y].ID == (int)blockIDs.air || blockArray[x - 1, y].isBlockTransparent));
        }
        public void generateBlockReferences()
        {
            blockFromID.Add(blockIDs.air, new Block(new Rectangle(0, 0, 0, 0), intFromBlockID[blockIDs.air])); //Air block
            blockFromID.Add(blockIDs.stone, new Block(new Rectangle(0, 0, 32, 32), intFromBlockID[blockIDs.stone]));
            blockFromID.Add(blockIDs.dirt, new Block(new Rectangle(0, 32, 32, 32), intFromBlockID[blockIDs.dirt]));
            blockFromID.Add(blockIDs.grass, new GrassBlock(new Rectangle(0, 64, 32, 32), intFromBlockID[blockIDs.grass]));
            blockFromID.Add(blockIDs.torch, new TorchBlock(new Rectangle(0, 96, 32, 32), intFromBlockID[blockIDs.torch]));
            blockFromID.Add(blockIDs.chest, new ChestBlock(new Rectangle(0, 128, 32, 32), (int)blockIDs.chest));
            blockFromID.Add(blockIDs.water, new FluidBlock(new Rectangle(0, 160, 32, 32), intFromBlockID[blockIDs.water]));
            blockFromID.Add(blockIDs.ironOre, new OreBlock(new Rectangle(0, 224, 32, 32), (int)blockIDs.ironOre, (int)oreIDs.iron));
            blockFromID.Add(blockIDs.treeTrunk, new TreeBlock(new Rectangle(0, 256, 32, 32), (int)blockIDs.treeTrunk));
            blockFromID.Add(blockIDs.leaves, new LeafBlock(new Rectangle(0, 288, 32, 32), (int)blockIDs.leaves));
            blockFromID.Add(blockIDs.semiLeaves, new SemiLeafBlock(new Rectangle(0, 288, 32, 32), (int)blockIDs.semiLeaves));
            blockFromID.Add(blockIDs.bush, new BushBlock(new Rectangle(0, 320, 32, 32), (int)blockIDs.bush));
            blockFromID.Add(blockIDs.bigBush, new BigBushBlock(new Rectangle(96, 320, 32, 32), (int)blockIDs.bigBush));
        }

        public Block getBlockFromID(blockIDs ID)
        {
            return blockFromID[ID];
        }

        public void updatePixelsPerBlock(int newPixelsPerBlock)
        {
            pixelsPerBlock = newPixelsPerBlock;
            foreach (PhysicsObject obj in physicsObjects)
            {
                obj.recalculateCollider();
            }
        }

        public Biome getBiomeFromBlockLocation(int x, int y)
        {
            //Go through each subterrainean biome
            for (int i = 0; i < subterraneanWorldBiomeList.Count; i++)
            {
                bool withinX = false;
                if (x >= subterraneanWorldBiomeList[i].x && x <= subterraneanWorldBiomeList[i].x + subterraneanWorldBiomeList[i].biomeDimensions.width)
                {
                    withinX = true;
                }
                bool withinY = false;
                if (y >= subterraneanWorldBiomeList[i].y && y <= subterraneanWorldBiomeList[i].y + subterraneanWorldBiomeList[i].biomeDimensions.height)
                {
                    withinY = true;
                }

                if (withinX && withinY)
                {
                    return subterraneanWorldBiomeList[i];
                }
            }

            //Presuming no biomes were found, then determine which surface biome:
            int cummulativeLength = 0;
            for (int i = 0; i < surfaceWorldBiomeList.Count; i++)
            {
                if (x > cummulativeLength && x < cummulativeLength + surfaceWorldBiomeList[i].biomeDimensions.width)
                {
                    return surfaceWorldBiomeList[i];
                }
                cummulativeLength += surfaceWorldBiomeList[i].biomeDimensions.width;
            }

            return null;
        }
        public void setPlayer(Player player)
        {
            this.player = player;
        }

        public bool damageBlock(double damageStrength, double durabilityLoss, int x, int y)
        {
            bool canDamageBlock = false;

            if (worldArray[x, y].ID != 0)
            {
                if (damageStrength >= worldArray[x, y].hardness)
                {
                    worldArray[x, y].durability -= durabilityLoss;

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public bool deleteBlock(int x, int y)
        {
            if (worldArray[x, y].ID != 0)
            {
                worldArray[x, y].onBlockDestroyed(exposedBlocks, this);
                worldArray[x, y] = new Block(blockFromID[blockIDs.air]);
                worldArray[x, y].setLocation((x, y));
                for (int checkX = x - 1; checkX <= x + 1; checkX++)
                {
                    for (int checkY = y - 1; checkY <= y + 1; checkY++)
                    {
                        if (worldArray[checkX, checkY].ID != (int)blockIDs.air)
                        {
                            addBlockToDictionaryIfExposedToAir(worldArray, checkX, checkY);
                            worldArray[checkX, checkY].updateBlock(this);
                        }
                    }
                }

                return true;
            }

            return false;
        }
        public bool addBlock(int x, int y, int ID)
        {
            if (worldArray[x, y].ID == (int)blockIDs.air && blockFromID[blockIDFromInt[ID]].canBlockBePlaced(this, (x, y)))
            {
                worldArray[x, y] = blockFromID[blockIDFromInt[ID]].copyBlock();
                worldArray[x, y].onBlockPlaced(this, (x, y));
                addBlockToDictionaryIfExposedToAir(worldArray, x, y);
                return true;
            }

            return false;
        }

        public bool setBackground(int x, int y, int ID)
        {
            if (x >= 0 && y >= 0 && x < backgroundArray.GetLength(0) && y < backgroundArray.GetLength(1))
            {
                if (backgroundArray[x, y] != ID)
                {
                    backgroundArray[x, y] = ID;
                    return true;
                }
            }
            return false;
        }
    }

    public class Sun : IEmissive
    {
        public Vector3 lightColor { get; set; }
        public float luminosity { get; set; }
        public float baseLuminosity;
        public float range { get; set; }
        public RenderTarget2D shadowMap { get; set; }
        public RenderTarget2D lightMap { get; set; }
        public double x { get; set; }
        public double y { get; set; }

        public double time = 0;

        public double dayDuration = 1 * 60;

        public const double horizonAngle = 3 * Math.PI / 4;

        public double angle { get; set; }
        public double distance { get; set; }

        public double coefficientOfDepthDecay = 0.1;

        public Sky sky;

        public WorldContext worldContext;

        public Sun(WorldContext worldContext)
        {
            lightColor = new Vector3(155, 155, 145);
            luminosity = 14000;
            baseLuminosity = luminosity;
            range = 150;
            x = 50;
            y = 50;
            shadowMap = new RenderTarget2D(worldContext.engineController.lightingSystem.graphics.GraphicsDevice, (int)(worldContext.engineController.lightingSystem.graphics.PreferredBackBufferWidth * worldContext.engineController.lightingSystem.shaderPrecision), (int)(worldContext.engineController.lightingSystem.graphics.PreferredBackBufferHeight * worldContext.engineController.lightingSystem.shaderPrecision));
            lightMap = new RenderTarget2D(worldContext.engineController.lightingSystem.graphics.GraphicsDevice, (int)(worldContext.engineController.lightingSystem.graphics.PreferredBackBufferWidth * worldContext.engineController.lightingSystem.shaderPrecision), (int)(worldContext.engineController.lightingSystem.graphics.PreferredBackBufferHeight * worldContext.engineController.lightingSystem.shaderPrecision));

            angle = -3 * Math.PI / 4;
            distance = 3;
            this.worldContext = worldContext;
        }

        public void updateTime(double elapsedTime)
        {
            time += elapsedTime;

            double dayTime = time % dayDuration;

            angle = ((dayTime / dayDuration) * 0.75 * horizonAngle) - horizonAngle;

            lightColor = new Vector3(lightColor.X, 55 + 100f * (float)Math.Sin(Math.PI * dayTime / dayDuration), 145f * (float)Math.Sin(Math.PI * dayTime / dayDuration));

            //Adjust the luminosity as the player goes deeper:
            luminosity = baseLuminosity;
            double playerDistanceDown = worldContext.player.y / (double)worldContext.pixelsPerBlock - worldContext.surfaceHeight[(int)(worldContext.player.x / worldContext.pixelsPerBlock)];

            if (playerDistanceDown > 0)
            {
                luminosity = (float)(baseLuminosity / (coefficientOfDepthDecay * (playerDistanceDown + 1)));
                if (luminosity > baseLuminosity)
                {
                    luminosity = baseLuminosity;
                }
            }
        }
    }

    public class Sky
    {
        public int spriteSheet = (int)spriteSheetIDs.skyLayers;
        public List<SkyLayer> skyLayers = new List<SkyLayer>();

        public Sky(int spriteSheetID, int layerCount, List<double> layerMovement, Vector2 sourceDimensions)
        {
            this.spriteSheet = spriteSheetID;
            
            for (int i = layerCount; i >= 0; i--) {
                double layerMotion = 0;
                if (i < layerMovement.Count) {
                    layerMotion = layerMovement[i];
                }
                skyLayers.Add(new SkyLayer(layerMotion, new Rectangle((int)sourceDimensions.X, i * (int)sourceDimensions.Y, (int)sourceDimensions.X, (int)sourceDimensions.Y), new Rectangle(0,0,1920,1080)));
            }
        }
        public void updateSky(WorldContext wc)
        {
            for (int i = 0; i < skyLayers.Count; i++)
            {
                skyLayers[i].updateLocation(wc.screenSpaceOffset.x / (double)wc.pixelsPerBlock, wc.screenSpaceOffset.y / (double)wc.pixelsPerBlock);
            }
        }
    }

    public class SkyLayer : DrawnClass
    {
        public double movement;

        public int baseX;
        public SkyLayer(double motion, Rectangle sourceRect, Rectangle drawRect)
        {
            movement = motion;
            sourceRectangle = sourceRect;
            drawRectangle = drawRect;
            baseX = sourceRectangle.X;
        }

        public void updateLocation(double x, double y)
        {
            sourceRectangle.X = baseX + (int)(x * movement);
            //drawRectangle.Y = (int)(y * movement);
        }
    }



}
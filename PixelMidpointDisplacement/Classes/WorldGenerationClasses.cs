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


namespace PixelMidpointDisplacement {
    public class WorldGenerator
    {
        public WorldContext worldContext;

        public int[,] worldArray;
        public int[,] backgroundArray;
        public int[] surfaceHeight;
        List<(int x, int y)> surfaceBlocks = new List<(int x, int y)>(); //This list contains all the blocks facing the surface, not only the ones that are highest. Eg. cliff faces


        double[,] perlinNoiseArray;
        public BlockGenerationVariables[,] brownianMotionArray;
        public List<BlockGenerationVariables> ores = new List<BlockGenerationVariables>();
        int maxAttempts = 16;

        public List<Biome> biomeList = new List<Biome>();
        public List<SubterraneanBiome> subterraneanBiomeList = new List<SubterraneanBiome>();
        List<Biome> biomeStencilList = new List<Biome>() {
            new MeadowBiome(),
            new MountainBiome()
        };
        List<SubterraneanBiome> subterraneanBiomeStencilList = new List<SubterraneanBiome>() {
            new CaveBiome()
        };

        const int subterraneanBiomeSpawnAttempt = 15;

        int rightMountainRangeWidth = 0;

        int horizonLine = 900;

        //Perlin Noise Variables:
        int noiseIterations = 8;

        double[] octaveWeights = {
        5,
        0.9,
        0.055,
        0.05,
        0.02,
        0.015,
        0.0075,
        0.00325
        };

        //Smaller means the blocks are also smaller...
        double frequency = 0.025;
        int vectorCount = 5;
        double vectorAngleOffset = 0;



        public WorldGenerator(WorldContext wc)
        {
            worldContext = wc;

            //Load a select few variables pertaining mostly to the perlin noise caves
            //Not all important variables can be loaded (or aren't) just due to the complexity of the system
            loadSettings();
        }

        private void loadSettings()
        {
            //Load octave count and octave weights, 
            //Load frequency
            //Load vector count and offset

            StreamReader sr = new StreamReader(worldContext.runtimePath + "Settings\\WorldGenerationVariables.txt");
            sr.ReadLine();
            noiseIterations = Convert.ToInt32(sr.ReadLine());
            sr.ReadLine();
            octaveWeights = new double[noiseIterations];
            for (int i = 0; i < noiseIterations; i++)
            {
                octaveWeights[i] = Convert.ToDouble(sr.ReadLine());
            }
            sr.ReadLine();
            frequency = Convert.ToDouble(sr.ReadLine());
            sr.ReadLine();
            vectorCount = Convert.ToInt32(sr.ReadLine());
            sr.ReadLine();
            vectorAngleOffset = Convert.ToDouble(sr.ReadLine());
        }

        public int[,] generateWorld((int width, int height) worldDimensions)
        {
            perlinNoiseArray = new double[worldDimensions.width, worldDimensions.height];
            brownianMotionArray = new BlockGenerationVariables[worldDimensions.width, worldDimensions.height];
            worldArray = new int[worldDimensions.width, worldDimensions.height];

            backgroundArray = new int[worldDimensions.width, worldDimensions.height];

            surfaceHeight = new int[worldDimensions.width];



            for (int x = 0; x < worldDimensions.width; x++)
            {
                surfaceHeight[x] = worldDimensions.height;
            }

            perlinNoise(worldDimensions, noiseIterations, octaveWeights, frequency, vectorCount, vectorAngleOffset);

            generateBiomes(worldDimensions);

            calculateSurfaceBlocks();

            convertDirtToGrass();

            return worldArray;
        }

        public void generateBiomes((int width, int height) worldDimensions)
        {
            generateSurfaceBiomes(worldDimensions);
            generateSubterraneanBiomes(worldDimensions);

            //Generate the worlds ores now:
            SeededBrownianMotion sbm = new SeededBrownianMotion();
            brownianMotionArray = sbm.brownianAlgorithm(brownianMotionArray, maxAttempts, fillOutput: true);

            //Go back through the biomes and generate caves, structures and the likes:
            for (int i = 0; i < biomeList.Count; i++)
            {
                combineAlgorithms(biomeList[i].biomeOffset, i);
                biomeList[i].generateBackground();
                biomeList[i].generateStructures();
            }

            for (int i = 0; i < subterraneanBiomeList.Count; i++)
            {
                combineAlgorithmsSubterranean((subterraneanBiomeList[i].x, subterraneanBiomeList[i].y), i, subterraneanBiomeList[i].externalBiomeIndex);
                subterraneanBiomeList[i].generateBackground();
                subterraneanBiomeList[i].generateStructures();
            }

            for (int i = 0; i < biomeList.Count; i++)
            {
                biomeList[i].generateFluids();

            }

            for (int i = 0; i < subterraneanBiomeList.Count; i++)
            {
                subterraneanBiomeList[i].generateFluids();
            }

            for (int i = 0; i < biomeList.Count; i++)
            {
                biomeList[i].generateDecorations();

            }

            for (int i = 0; i < subterraneanBiomeList.Count; i++)
            {
                subterraneanBiomeList[i].generateDecorations();
            }

            //To generate something on the right. Put it here
        }

        public void generateSurfaceBiomes((int width, int height) worldDimensions)
        {
            //In blocks. The biome offset is in blocks, the points just get converted into pixel space
            int currentLeadingWidth = 0;
            //To generate something on the left. Put it here
            //Blocks aren't generation. What's happening is that there's only blocks at the point when the biomes are generated
            Biome ocean = biomeStencilList[0].generateBiomeCopy((0, horizonLine), this, (0, 0), (0, worldDimensions.height));

            biomeList.Add(ocean);
            ocean.generateSurfaceTerrain();

            ocean.generateOres();


            currentLeadingWidth += ocean.biomeDimensions.width;

            while (worldDimensions.width - currentLeadingWidth > rightMountainRangeWidth)
            {
                int biomeNumber = new Random().Next(biomeStencilList.Count);
                //Generate a copy of the biome and pass in the rightmost point of the most recent biome terrain in the list 

                Biome biome = biomeStencilList[biomeNumber].generateBiomeCopy(biomeList[biomeList.Count - 1].initialPoints[biomeList[biomeList.Count - 1].initialPoints.Count - 1], this, (currentLeadingWidth, 0), (0, worldDimensions.height));
                biomeList.Add(biome);
                biome.generateSurfaceTerrain();
                biome.generateOres();

                currentLeadingWidth += biome.biomeDimensions.width;
            }
        }

        public void generateSubterraneanBiomes((int width, int height) worldDimensions)
        {
            for (int i = 0; i < subterraneanBiomeStencilList.Count; i++)
            {
                for (int c = 0; c < subterraneanBiomeStencilList[i].biomesPerWorld; c++)
                {
                    SubterraneanBiome biome = (SubterraneanBiome)subterraneanBiomeStencilList[i].generateBiomeCopy((0, 900), this, (0, 0), (0, 0));

                    bool foundASpot = false;

                    int attempts = 0;
                    Random r = new Random();
                    int biomeX = r.Next(0, worldDimensions.width);
                    int biomeY = r.Next(biome.minY, biome.maxY);

                    while (!foundASpot && attempts < subterraneanBiomeSpawnAttempt)
                    {
                        biomeX = r.Next(0, worldDimensions.width);
                        if (surfaceHeight[biomeX] + biome.maxY < worldArray.GetLength(1))
                        {
                            biomeY = r.Next(surfaceHeight[biomeX] + biome.minY, surfaceHeight[biomeX] + biome.maxY);
                        }
                        else
                        {
                            //biomeY = r.Next(surfaceHeight[biomeX] + biome.minY, worldArray.GetLength(1));
                        }
                        //Get the biome the x and y is in                        
                        int cummulativeWidth = 0;
                        for (int b = 0; b < biomeList.Count; b++)
                        {
                            if (biomeX > cummulativeWidth && biomeX < cummulativeWidth + biomeList[b].biomeDimensions.width)
                            {
                                if (biome.biomesThisCanSpawnIn.Contains(biomeList[b].GetType()))
                                {
                                    foundASpot = true;
                                    biome.externalBiomeIndex = b;
                                    biome.setBiomeLocation(biomeX, biomeY);
                                }
                                break;
                            }
                        }
                    }

                    if (foundASpot)
                    {
                        subterraneanBiomeList.Add(biome);
                        biome.generateOres();
                    }
                }
            }
        }
        public int[] getSurfaceHeight()
        {
            return surfaceHeight;
        }

        public List<(int x, int y)> getSurfaceBlocks()
        {
            return surfaceBlocks;
        }

        public int[,] getBackgroundArray()
        {
            return backgroundArray;
        }


        private void calculateSurfaceBlocks()
        {
            for (int x = 0; x < surfaceHeight.Length; x++)
            {
                surfaceBlocks.Add((x, surfaceHeight[x]));

                int y = surfaceHeight[x] + 1;
                bool isStillSurface = true;
                while (isStillSurface)
                {
                    isStillSurface = addSurfaceBlock(x, y);

                    if (!isStillSurface && x >= 0 && y >= 0 && x < worldArray.GetLength(0) && y < worldArray.GetLength(1))
                    {

                        surfaceBlocks.Add((x, y)); //If it has determined that a block is no longer on the surface, add the block right below: corners
                    }
                    y++;
                }

            }

        }

        private bool addSurfaceBlock(int x, int y)
        {
            if (y >= 0 && y < worldArray.GetLength(1) && x > 0 && x < worldArray.GetLength(0) - 1)
            { //If either side of the block is exposed to air, then add it to the surfaceBlocks list. However, make sure to account for
              //Letting boundary blocks still be checked
                if (worldArray[x - 1, y] == 0 || worldArray[x + 1, y] == 0)
                {
                    surfaceBlocks.Add((x, y));
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (x == 0 && y >= 0 && y < worldArray.GetLength(1))
            {
                if (worldArray[x + 1, y] == 0)
                {
                    surfaceBlocks.Add((x, y));
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (x == worldArray.GetLength(0) - 1 && y >= 0 && y < worldArray.GetLength(1))
            {
                if (worldArray[x - 1, y] == 0)
                {
                    surfaceBlocks.Add((x, y));
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

        private void convertDirtToGrass()
        {
            for (int i = 0; i < surfaceBlocks.Count; i++)
            {
                if (surfaceBlocks[i].x > 0 && surfaceBlocks[i].y > 0 && surfaceBlocks[i].x < worldArray.GetLength(0) - 1 && surfaceBlocks[i].y < worldArray.GetLength(1) - 1)

                    if (worldArray[surfaceBlocks[i].x - 1, surfaceBlocks[i].y] == 0 || worldArray[surfaceBlocks[i].x + 1, surfaceBlocks[i].y] == 0 || worldArray[surfaceBlocks[i].x, surfaceBlocks[i].y - 1] == 0)
                    {
                        //If the block is dirt and on the surface, convert it to grass
                        if (worldArray[surfaceBlocks[i].x, surfaceBlocks[i].y] == 2)
                        {
                            worldArray[surfaceBlocks[i].x, surfaceBlocks[i].y] = 3;
                        }
                    }
            }
        }

        //Code from the perlin noise caves:
        private void perlinNoise((int width, int height) worldDimensions, int perlinNoiseIterations, double[] octaveWeights, double frequency, int vectorCount, double vectorAngleOffset)
        {
            PerlinNoise pn = new PerlinNoise(worldDimensions, perlinNoiseIterations, vectorCount, vectorAngleOffset);
            int[] g = generateRandomIntArray();
            perlinNoiseArray = pn.generatePerlinNoise(g, worldDimensions, octaveWeights, frequency);
        }

        private int[] generateRandomIntArray()
        {
            int[] initialArray = new int[256];
            List<int> sortedArray = new List<int>();

            for (int i = 0; i < initialArray.Count(); i++)
            {
                sortedArray.Add(i);
            }
            for (int i = 0; i < initialArray.Count(); i++)
            {
                Random r = new Random();
                int rIndex = r.Next(0, sortedArray.Count());
                initialArray[i] = sortedArray[rIndex];
                sortedArray.RemoveAt(rIndex);
            }

            int[] outputArray = new int[initialArray.Count() * 2];
            for (int i = 0; i < outputArray.Count(); i++)
            {
                outputArray[i] = initialArray[i % 255];
            }

            return outputArray;
        }

        private void combineAlgorithms((int x, int y) biomeOffset, int biomeIndex)
        {
            Biome biome = biomeList[biomeIndex];
            int maxX = biomeOffset.x + biome.biomeDimensions.width;
            int maxY = biomeOffset.y + biome.biomeDimensions.height;
            if (maxX > worldArray.GetLength(0))
            {
                maxX = worldArray.GetLength(0);
            }
            if (maxY > worldArray.GetLength(1))
            {
                maxY = worldArray.GetLength(1);
            }
            for (int x = biomeOffset.x; x < maxX; x++)
            {
                for (int y = biomeOffset.y; y < maxY; y++)
                {
                    //Compute an averaged value of the noise threshold
                    double threshold = biome.changeThresholdByDepth((x, y));
                    //If the block is very close to the border, blend the threshold
                    int blockBlendRange = 10;
                    //Final - initial + initial. Calculate the threshold of the block not in the biome, but at the edgedw

                    if (x - biomeOffset.x < blockBlendRange && biomeIndex > 0) { threshold = biomeList[biomeIndex - 1].changeThresholdByDepth((biomeOffset.x - 1, y)) + (x - biomeOffset.x) * (threshold - biomeList[biomeIndex - 1].changeThresholdByDepth((biomeOffset.x - 1, y))) / blockBlendRange; }
                    if (perlinNoiseArray[x, y] > threshold)
                    { //If it's above the block threshold, set the block to be air, 
                        worldArray[x, y] = 0;

                    }
                    else if (brownianMotionArray[x, y] != null && worldArray[x, y] != 0) //If the brownian motion defined it, and it's solid from the midpoint generation
                    {
                        worldArray[x, y] = brownianMotionArray[x, y].block.ID;

                    }
                    else
                    {
                        worldArray[x, y] = 0;
                    }
                }
            }
        }


        private void combineAlgorithmsSubterranean((int x, int y) biomeOffset, int biomeIndex, int externalBiomeIndex)
        {
            Biome biome = subterraneanBiomeList[biomeIndex];
            int maxX = biomeOffset.x + biome.biomeDimensions.width;
            int maxY = biomeOffset.y + biome.biomeDimensions.height;
            if (maxX > worldArray.GetLength(0))
            {
                maxX = worldArray.GetLength(0);
            }
            if (maxY > worldArray.GetLength(1))
            {
                maxY = worldArray.GetLength(1);
            }
            for (int x = biomeOffset.x; x < maxX; x++)
            {
                for (int y = biomeOffset.y; y < maxY; y++)
                {
                    //Compute an averaged value of the noise threshold
                    double threshold = biome.changeThresholdByDepth((x, y));
                    //If the block is very close to the border, blend the threshold
                    int blockBlendRange = 10;
                    //Final - initial + initial. Calculate the threshold of the block not in the biome, but at the edgedw

                    //Use old/current cave systems: Don't re-do the caves

                    //if (x - biomeOffset.x < blockBlendRange || (biome.biomeDimensions.width - (x - biomeOffset.x) < blockBlendRange) || y - biomeOffset.y < blockBlendRange || (biome.biomeDimensions.height - (y - biomeOffset.y) < blockBlendRange)) { threshold = biomeList[externalBiomeIndex].changeThresholdByDepth((biomeOffset.x - 1, y)) + (x - biomeOffset.x) * (threshold - biomeList[externalBiomeIndex].changeThresholdByDepth((biomeOffset.x - 1, y))) / blockBlendRange; }
                    /*if (perlinNoiseArray[x, y] > threshold)
                    { //If it's above the block threshold, set the block to be air, 
                        worldArray[x, y] = 0;

                    }*/
                    if (brownianMotionArray[x, y] != null && worldArray[x, y] != 0) //If the brownian motion defined it, and it's solid from the midpoint generation
                    {
                        worldArray[x, y] = brownianMotionArray[x, y].block.ID;
                        if (worldArray[x, y] != 0)
                        {
                            if (surfaceHeight[x] == null || surfaceHeight[x] > y)
                            {
                                surfaceHeight[x] = y;
                            }

                        }
                    }
                    else
                    {
                        worldArray[x, y] = 0;
                    }
                }
            }
        }


        public (int x, int y) findLocalMin(int x, int y)
        {
            const int maxSearchAttempts = 300;
            const int maxSidewaysSearchAttempts = 30;
            int minX = x;
            int minY = y;
            Random r = new Random();
            int resetX = 0;
            int resetY = 0;
            int oppositeDirection = 0;
            if (worldArray[minX, minY] == 0)
            {
                //Search downwards first
                bool foundMin = false;
                int searchAttempts = 0;
                int sidewaysSearchAttempts = 0;
                int sidewaysSearchDirection = 0;

                bool hitAWallLeft = false;
                bool hitAWallRight = false;
                while (!foundMin && searchAttempts < maxSearchAttempts && sidewaysSearchAttempts < maxSidewaysSearchAttempts)
                {
                    if (minX >= 0 && minX < worldArray.GetLength(0) && minY >= 0 && minY < worldArray.GetLength(1) - 1)
                    {
                        if (worldArray[minX, minY + 1] == 0)
                        {
                            minY = minY + 1;
                            sidewaysSearchDirection = 0;
                            searchAttempts += 1;

                            resetX = 0;
                            resetY = 0;
                            oppositeDirection = 0;
                        }
                        else
                        {
                            searchAttempts = 0;
                            //Not air below, so pick a direction to search in:

                            if (sidewaysSearchDirection == 0)
                            {
                                sidewaysSearchDirection = (2 * r.Next(2)) - 1; //Randomly pick -1 or 1
                                resetX = minX;
                                resetY = minY;
                                oppositeDirection = sidewaysSearchDirection * -1;

                            }
                            if (minX + sidewaysSearchDirection >= 0 && minX + sidewaysSearchDirection < worldArray.GetLength(0) && minY >= 0 && minY < worldArray.GetLength(1))
                            {
                                if (worldArray[minX + sidewaysSearchDirection, minY] == 0)
                                {
                                    minX += sidewaysSearchDirection;
                                }
                                else
                                {
                                    //Hit a wall!
                                    if (sidewaysSearchDirection == -1)
                                    {
                                        hitAWallLeft = true;
                                    }
                                    else
                                    {
                                        hitAWallRight = true;
                                    }


                                    if (hitAWallLeft && hitAWallRight)
                                    {
                                        foundMin = true;
                                    }
                                    else
                                    {
                                        sidewaysSearchDirection = oppositeDirection;
                                        minX = resetX;
                                        minY = resetY;
                                    }
                                }
                            }
                            else
                            {
                                return (0, 0);
                            }

                        }
                    }
                    else
                    {
                        return (0, 0);
                    }
                }
            }

            return (minX, minY);

        }
    }

    public class BlockGenerationVariables
    {
        public double seedDensity;
        public Block block;
        public int maxSingleSpread;
        public int currentSingleSpread;
        public int oreVeinSpread;

        public int identifier;

        public List<BlockGenerationVariables> veinList = new List<BlockGenerationVariables>();

        public (double north, double northEast, double east, double southEast, double south, double southWest, double west, double northWest) directionWeights = (0.125, 0.125, 0.125, 0.125, 0.125, 0.125, 0.125, 0.125); //Perfectly weighted as default
        public BlockGenerationVariables(double seedDensity, Block block, int maxSingleSpread, int oreVeinSpread, (double north, double northEast, double east, double southEast, double south, double southWest, double west, double northWest) directionWeights)
        {
            this.seedDensity = seedDensity;
            this.block = block;
            this.maxSingleSpread = maxSingleSpread;
            this.currentSingleSpread = maxSingleSpread;
            this.oreVeinSpread = oreVeinSpread;
            this.directionWeights = directionWeights;

        }

        public BlockGenerationVariables(double seedDensity, Block block, int maxSingleSpread, int oreVeinSpread)
        {
            this.seedDensity = seedDensity;
            this.block = block;
            this.maxSingleSpread = maxSingleSpread;
            this.currentSingleSpread = maxSingleSpread;
            this.oreVeinSpread = oreVeinSpread;
        }

        public BlockGenerationVariables(BlockGenerationVariables blockVariables)
        {
            seedDensity = blockVariables.seedDensity;
            block = blockVariables.block;
            maxSingleSpread = blockVariables.maxSingleSpread;
            currentSingleSpread = blockVariables.maxSingleSpread;
            oreVeinSpread = blockVariables.oreVeinSpread;
            directionWeights = blockVariables.directionWeights;
            veinList = blockVariables.veinList;
            identifier = blockVariables.identifier + 2;
        }

        public void hasSpread()
        {
            currentSingleSpread -= 1;
            oreVeinSpread -= 1;
        }

        public void hasSpreadVein(int oreVeinSpread)
        {
            this.oreVeinSpread = oreVeinSpread;
        }

        public void initialiseVeinList(BlockGenerationVariables thisBlock)
        {
            veinList = new List<BlockGenerationVariables>();
            veinList.Add(thisBlock);
        }

        public void updateVeinList(BlockGenerationVariables newBlock)
        {
            if (!veinList.Contains(newBlock))
            {
                veinList.Add(newBlock);
            }
        }

    }
    public class BlockThresholdValues
    {
        //Higher means more solid
        public double blockThreshold;
        public double maximumY;
        public double decreasePerY;
        public double maximumThreshold;
        public double minimumThreshold;
        //The effect of the absolute y value (from the top of the map) and the relative y value (from the surface)
        public double absoluteYHeightWeight;
        public double relativeYHeightWeight;

        public BlockThresholdValues(double blockThreshold, double maximumY, double decreasePerY, double maximumThreshold, double minimumThreshold, double absoluteYHeightWeight, double relativeYHeightWeight)
        {
            this.blockThreshold = blockThreshold;
            this.maximumY = maximumY;
            this.decreasePerY = decreasePerY;
            this.maximumThreshold = maximumThreshold;
            this.minimumThreshold = minimumThreshold;
            this.absoluteYHeightWeight = absoluteYHeightWeight;
            this.relativeYHeightWeight = relativeYHeightWeight;
        }
    }
    public class PerlinNoise
    {
        List<double[,]> pixelOctaves = new List<double[,]>();

        Vector2[] randomisedUnitVectors;

        double vectorAngleOffset;
        public PerlinNoise((int outputSizeX, int outputSizeY) outputDimensions, int octaveCount, int vectorCount, double vectorAngleOffset)
        {
            randomisedUnitVectors = new Vector2[vectorCount];
            this.vectorAngleOffset = vectorAngleOffset;
            for (int octaves = 0; octaves < octaveCount; octaves++)
            {
                pixelOctaves.Add(new double[outputDimensions.outputSizeX, outputDimensions.outputSizeY]);
            }
        }

        public void randomiseVectorArray(int[] g)
        {
            double radiansPerIndex = 2 * Math.PI / randomisedUnitVectors.Count();

            for (int i = 0; i < randomisedUnitVectors.Count(); i++)
            {
                randomisedUnitVectors[i] = new Vector2((float)Math.Cos(radiansPerIndex * g[i] + vectorAngleOffset), (float)Math.Sin(radiansPerIndex * g[i] + vectorAngleOffset));
            }
        }


        public double[,] generatePerlinNoise(int[] g, (int noiseOutputSizeX, int noiseOutputSizeY) outputDimensions, double[] octaveWeights, double frequency)
        {
            double[,] noiseOutput = new double[outputDimensions.noiseOutputSizeX, outputDimensions.noiseOutputSizeY];

            randomiseVectorArray(g);

            for (int i = 0; i < pixelOctaves.Count(); i++)
            {
                //pixelOctaves[i] = randomlyInitialisePixelArray(pixelOctaves[i]);
                pixelOctaves[i] = perlinAlgorithm(pixelOctaves[i], frequency * Math.Pow(2, i), g);
                noiseOutput = addNoiseToOutput(noiseOutput, pixelOctaves[i], octaveWeights[i]);
            }


            return noiseOutput;
        }

        public double[,] addNoiseToOutput(double[,] currentNoise, double[,] newNoise, double octaveWeight)
        {
            double[,] cumulatedNoise = new double[currentNoise.GetLength(0), currentNoise.GetLength(1)];

            for (int x = 0; x < currentNoise.GetLength(0); x++)
            {
                for (int y = 0; y < currentNoise.GetLength(1); y++)
                {
                    double cumulateNoise = currentNoise[x, y] + octaveWeight * newNoise[x, y];
                    double boundedNoise = cumulateNoise / (1 + octaveWeight);
                    cumulatedNoise[x, y] = boundedNoise;

                }
            }

            return cumulatedNoise;
        }
        public double[,] perlinAlgorithm(double[,] pixels, double frequency, int[] g)
        {
            for (int x = 0; x < pixels.GetLength(0); x++)
            {
                for (int y = 0; y < pixels.GetLength(1); y++)
                {

                    //Multiply the location by a small 'frequency' value
                    Vector2 location = new Vector2((float)frequency * x, (float)frequency * y);

                    int X = (int)Math.Floor(location.X) % 255;
                    int Y = (int)Math.Floor(location.Y) % 255;

                    float xlocal = (float)(location.X - Math.Floor(location.X));
                    float ylocal = (float)(location.Y - Math.Floor(location.Y));

                    Vector2 topLeft = new Vector2(xlocal, ylocal);
                    Vector2 topRight = new Vector2(xlocal - 1, ylocal);
                    Vector2 bottomLeft = new Vector2(xlocal, ylocal - 1);
                    Vector2 bottomRight = new Vector2(xlocal - 1, ylocal - 1);


                    int topLeftValue = g[g[X] + Y];
                    int topRightValue = g[g[X + 1] + Y];
                    int bottomLeftValue = g[g[X] + Y + 1];
                    int bottomRightValue = g[g[X + 1] + Y + 1];

                    double dotTopLeft = Vector2.Dot(topLeft, getConstantVector(topLeftValue));
                    double dotTopRight = Vector2.Dot(topRight, getConstantVector(topRightValue));
                    double dotBottomLeft = Vector2.Dot(bottomLeft, getConstantVector(bottomLeftValue));
                    double dotBottomRight = Vector2.Dot(bottomRight, getConstantVector(bottomRightValue));



                    double xf = fadeFunction(xlocal);
                    double yf = fadeFunction(ylocal);

                    double noise = Lerp(xf,
                    Lerp(yf, dotTopLeft, dotBottomLeft),
                    Lerp(yf, dotTopRight, dotBottomRight));

                    if (noise > 1)
                    {
                        Console.WriteLine("At this pixel-");
                        Console.WriteLine("Noise: " + noise);
                        Console.WriteLine("xf & yf: " + xf + ", " + yf);
                    }

                    pixels[x, y] = (noise + 1) / 2;
                }
            }

            return pixels;
        }

        public Vector2 getConstantVector(int value)
        {
            Vector2 constantVector;

            constantVector = randomisedUnitVectors[value % randomisedUnitVectors.Count()];

            return constantVector;
        }
        public double[] dotProduct(System.Numerics.Vector2[,] unitSquareVector, System.Numerics.Vector2[,] differenceVector)
        {
            //A method returning a list of dot products in English reading order
            double[] dotProducts = new double[4];

            //Both vector arrays are 2x2 but it is easier to read and implement using for loops
            for (int y = 0; y < unitSquareVector.GetLength(0); y++)
            {
                for (int x = 0; x < unitSquareVector.GetLength(1); x++)
                {
                    dotProducts[x + 2 * y] = Vector2.Dot(unitSquareVector[x, y], differenceVector[x, y]);
                }
            }

            return dotProducts;
        }

        public double fadeFunction(double t)
        {
            //Perlins improved fade function: 6^t5 -15t^4 +10t^3
            double fadeFunctionValue = 6.0 * Math.Pow(t, 5) - 15.0 * Math.Pow(t, 4) + 10.0 * Math.Pow(t, 3);
            return fadeFunctionValue;
        }

        public double Lerp(double t, double v1, double v2)
        {
            double lerp = v1 + t * (v2 - v1);

            return lerp;
        }

    }
    public class SeededBrownianMotion
    {
        public BlockGenerationVariables[,] seededBrownianMotion(BlockGenerationVariables[,] worldArray, BlockGenerationVariables[] ores)
        {
            BlockGenerationVariables[,] seededArray = new BlockGenerationVariables[worldArray.GetLength(0), worldArray.GetLength(1)];
            seededArray = seedArray(worldArray, ores);
            //seededArray = BrownianAlgorithm(seededArray);
            return seededArray;
        }

        public BlockGenerationVariables[,] seedArray(BlockGenerationVariables[,] worldArray, BlockGenerationVariables[] ores)
        {
            //Generate a random number of seeds for each ore, depending on it's seedDensity, 
            //then randomly distribute them inside the world Array
            foreach (BlockGenerationVariables ore in ores)
            {
                int numberOfSeeds = (int)((ore.seedDensity / 100) * worldArray.Length);
                for (int i = 0; i < numberOfSeeds; i++)
                {
                    Random r = new Random();
                    int seedX = r.Next(0, worldArray.GetLength(0) - 1);
                    int seedY = r.Next(0, worldArray.GetLength(1) - 1);
                    //Creates a new class with the same parameters. If it directly equals it just passes a pointer
                    BlockGenerationVariables newBlock = new BlockGenerationVariables(ore);
                    newBlock.initialiseVeinList(newBlock); //Add itself to the Veinlist Array
                    newBlock.identifier = i;
                    worldArray[seedX, seedY] = newBlock;
                }
            }

            return worldArray;
        }

        public BlockGenerationVariables[,] brownianAlgorithm(BlockGenerationVariables[,] worldArray, int attemptCount, bool fillOutput)
        {
            //It would probably be more efficient to have a seperate array containing only the non-null blocks but I don't know a
            //readable way of doing it (ironic with the line break)
            int attempts = 0;

            while (attempts < attemptCount) //Runs until no changes have been made in that iteration.
            {
                bool hasChangedTheArray = false;
                //Read everything from the worldArray but write to the tempArray then equalise at the end
                BlockGenerationVariables[,] tempArray = worldArray.Clone() as BlockGenerationVariables[,];

                for (int x = 0; x < worldArray.GetLength(0); x++)
                {
                    for (int y = 0; y < worldArray.GetLength(1); y++)
                    {
                        (BlockGenerationVariables[,] outputArray, bool hasChanged) output = brownianMotion(worldArray, tempArray, x, y);
                        tempArray = output.outputArray;
                        if (output.hasChanged && !hasChangedTheArray)
                        {
                            hasChangedTheArray = true;
                        }
                    }

                }

                worldArray = tempArray.Clone() as BlockGenerationVariables[,];
                if (!hasChangedTheArray)
                {
                    attempts += 1;
                }
            }

            if (fillOutput)
            {
                fill(worldArray);
            }

            return worldArray;
        }

        public (BlockGenerationVariables[,], bool) brownianMotion(BlockGenerationVariables[,] worldArray, BlockGenerationVariables[,] tempArray, int x, int y)
        {
            bool hasChanged = false;
            if (worldArray[x, y] != null)
            {
                BlockGenerationVariables block = worldArray[x, y];
                if (block.currentSingleSpread > 0 && block.oreVeinSpread > 0) //If the block/vein is allowed to spread
                {
                    Random r = new Random();
                    double rValue = r.NextDouble();
                    if (rValue <= block.directionWeights.north)
                    {
                        if (y - 1 >= 0)
                        {
                            if (tempArray[x, y - 1] == null)
                            {
                                tempArray[x, y - 1] = spreadBlock(worldArray, tempArray, (x, y));
                                hasChanged = true;
                            }

                        }
                    }
                    else if (rValue < block.directionWeights.north + block.directionWeights.northEast)
                    {
                        if (x + 1 < worldArray.GetLength(0) && y - 1 >= 0)
                        {
                            if (tempArray[x + 1, y - 1] == null)
                            {
                                tempArray[x + 1, y - 1] = spreadBlock(worldArray, tempArray, (x, y));
                                hasChanged = true;
                            }
                        }

                    }
                    else if (rValue < (block.directionWeights.north + block.directionWeights.northEast + block.directionWeights.east))
                    {
                        if (x + 1 < worldArray.GetLength(0))
                        {
                            if (tempArray[x + 1, y] == null)
                            {
                                tempArray[x + 1, y] = spreadBlock(worldArray, tempArray, (x, y));
                                hasChanged = true;
                            }
                        }

                    }
                    else if (rValue < (block.directionWeights.north + block.directionWeights.northEast + block.directionWeights.east + block.directionWeights.southEast))
                    {
                        if (y + 1 < worldArray.GetLength(1) && x + 1 < worldArray.GetLength(0)) //Make sure the block is inside the array bounds
                        {
                            if (tempArray[x + 1, y + 1] == null)
                            {
                                tempArray[x + 1, y + 1] = spreadBlock(worldArray, tempArray, (x, y));
                                hasChanged = true;
                            }
                        }

                    }

                    else if (rValue < (block.directionWeights.north + block.directionWeights.northEast + block.directionWeights.east + block.directionWeights.southEast + block.directionWeights.south))
                    {
                        if (y + 1 < worldArray.GetLength(1)) //Make sure the block is inside the array bounds
                        {
                            if (tempArray[x, y + 1] == null)
                            {
                                tempArray[x, y + 1] = spreadBlock(worldArray, tempArray, (x, y));
                                hasChanged = true;
                            }
                        }

                    }
                    else if (rValue < (block.directionWeights.north + block.directionWeights.northEast + block.directionWeights.east + block.directionWeights.southEast + block.directionWeights.south + block.directionWeights.southWest))
                    {
                        if (y + 1 < worldArray.GetLength(1) && x - 1 >= 0) //Make sure the block is inside the array bounds
                        {
                            if (tempArray[x - 1, y + 1] == null)
                            {
                                tempArray[x - 1, y + 1] = spreadBlock(worldArray, tempArray, (x, y));
                                hasChanged = true;
                            }
                        }

                    }
                    else if (rValue < (block.directionWeights.north + block.directionWeights.northEast + block.directionWeights.east + block.directionWeights.southEast + block.directionWeights.south + block.directionWeights.southWest + block.directionWeights.west))
                    {
                        if (x - 1 >= 0) //Make sure the block is inside the array bounds
                        {
                            if (tempArray[x - 1, y] == null)
                            {
                                tempArray[x - 1, y] = spreadBlock(worldArray, tempArray, (x, y));
                                hasChanged = true;
                            }
                        }

                    }
                    else
                    {
                        if (x - 1 >= 0 && y - 1 >= 0) //Make sure the block is inside the array bounds
                        {
                            if (tempArray[x - 1, y - 1] == null)
                            {
                                tempArray[x - 1, y - 1] = spreadBlock(worldArray, tempArray, (x, y));
                                hasChanged = true;
                            }
                        }

                    }
                }
            }
            return (tempArray, hasChanged);
        }

        public BlockGenerationVariables spreadBlock(BlockGenerationVariables[,] worldArray, BlockGenerationVariables[,] tempArray, (int x, int y) location)
        {
            int x = location.x;
            int y = location.y;
            foreach (BlockGenerationVariables b in worldArray[x, y].veinList)
            {
                b.oreVeinSpread -= 1;    //Sychronise the updated ore size across all the blocks in the vein
            }
            tempArray[x, y].currentSingleSpread -= 1;
            BlockGenerationVariables newBlock = new BlockGenerationVariables(worldArray[x, y]);
            newBlock.updateVeinList(newBlock);
            return newBlock;
        }

        public Block[,] fill(Block[,] blockArray)
        {
            for (int x = 0; x < blockArray.GetLength(0); x++)
            {
                for (int y = 0; y < blockArray.GetLength(1); y++)
                {
                    if (blockArray[x, y] == null)
                    {
                        List<Block> blocks = new List<Block>();
                        List<int> blockCount = new List<int>();
                        for (int xLocal = x - 1; xLocal <= x + 1; xLocal++)
                        {
                            for (int yLocal = y - 1; yLocal <= y + 1; yLocal++)
                            {
                                if (xLocal >= 0 && yLocal >= 0 && xLocal < blockArray.GetLength(0) && yLocal < blockArray.GetLength(1))
                                    if (blockArray[xLocal, yLocal] != null)
                                    {
                                        if (blocks.Contains(blockArray[xLocal, yLocal]))
                                        {
                                            blockCount[blocks.IndexOf(blockArray[xLocal, yLocal])] += 1;
                                        }
                                        else
                                        {
                                            blocks.Add(blockArray[xLocal, yLocal]);
                                            blockCount.Add(1);
                                        }
                                    }
                            }
                        }
                        if (blocks.Count != 0)
                        {
                            blockArray[x, y] = blocks[blockCount.IndexOf(blockCount.Max())];
                        }

                    }
                }
            }

            return blockArray;
        }

        public BlockGenerationVariables[,] fill(BlockGenerationVariables[,] blockArray)
        {
            for (int x = 0; x < blockArray.GetLength(0); x++)
            {
                for (int y = 0; y < blockArray.GetLength(1); y++)
                {
                    if (blockArray[x, y] == null)
                    {
                        List<Block> blocks = new List<Block>();
                        List<BlockGenerationVariables> blockVariables = new List<BlockGenerationVariables>();
                        List<int> blockCount = new List<int>();
                        for (int xLocal = x - 1; xLocal <= x + 1; xLocal++)
                        {
                            for (int yLocal = y - 1; yLocal <= y + 1; yLocal++)
                            {
                                if (xLocal >= 0 && yLocal >= 0 && xLocal < blockArray.GetLength(0) && yLocal < blockArray.GetLength(1))
                                {

                                    if (blockArray[xLocal, yLocal] != null)
                                    {
                                        if (blocks.Contains(blockArray[xLocal, yLocal].block))
                                        {
                                            blockCount[blocks.IndexOf(blockArray[xLocal, yLocal].block)] += 1;
                                        }
                                        else
                                        {
                                            blocks.Add(blockArray[xLocal, yLocal].block);
                                            blockVariables.Add(blockArray[xLocal, yLocal]);
                                            blockCount.Add(1);
                                        }
                                    }
                                }
                            }
                        }
                        if (blocks.Count != 0)
                        {
                            blockArray[x, y] = blockVariables[blockCount.IndexOf(blockCount.Max())];
                        }
                    }
                }
            }

            return blockArray;
        }

    }

    public class MidpointDisplacementAlgorithm
    {
        //An iterative process. Takes in a list of points, returns the same list. Can then be converted to blocks in the worldGeneration function
        List<(double x, double y)> pointList;
        double offset;
        double decayPower;
        int iterations;
        int positiveWeight = 50;
        public MidpointDisplacementAlgorithm(List<(double, double)> initialPoints, double initialOffset, double decayPower, int iterations)
        {
            this.pointList = initialPoints;
            this.offset = initialOffset;
            this.decayPower = decayPower;
            this.iterations = iterations;
        }
        public MidpointDisplacementAlgorithm(List<(double, double)> initialPoints, double initialOffset, double decayPower, int iterations, int positiveWeight)
        {
            this.pointList = initialPoints;
            this.offset = initialOffset;
            this.decayPower = decayPower;
            this.iterations = iterations;
            this.positiveWeight = positiveWeight;
        }

        public List<(double x, double y)> midpointAlgorithm()
        {
            for (int i = 0; i < iterations; i++)
            {
                for (int j = 1; j < pointList.Count; j += 2)
                {
                    (double x, double y) point = calculateMidpoint(j);
                    double thisOffset = generateRandomOffset();
                    pointList.Insert(j, (point.x, point.y + thisOffset));
                }
                offset *= 1 / Math.Pow(2, decayPower);
            }
            return pointList;
        }

        public (double, double) calculateMidpoint(int i)
        {

            double midX = (pointList[i - 1].x + pointList[i].x) / 2;
            double midY = (pointList[i - 1].y + pointList[i].y) / 2;

            return (midX, midY);
        }

        public double generateRandomOffset()
        {
            Random r = new Random();

            double sign = r.Next(-(100 - positiveWeight), positiveWeight);
            if (sign == 0) { sign = 1; } //To prevent any weird terrain caused by 0 values

            return offset * Math.Sign(sign);
        }
    }


}
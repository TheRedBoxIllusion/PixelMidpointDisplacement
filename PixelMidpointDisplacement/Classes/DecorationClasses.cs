using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace PixelMidpointDisplacement {
    public class Decoration
    {
        public virtual bool generate(int x, int y, WorldContext wc)
        {
            return true;
        }

        public virtual bool generate(int x, int y, WorldGenerator wg)
        {
            return true;
        }
    }
    public class TreeGeneration : Decoration
    {

        List<BranchVariables> branches = new List<BranchVariables>();


        List<BranchEndVariables> branchEnds = new List<BranchEndVariables>();
        public int branchLength = 8;

        public double branchAngle = Math.PI / 5;
        public double branchProbability = 0.25;

        int currentSign = 1;

        public double angleIncreasePerBlockAdded = 0;

        public int blocksTilBranch = 3;
        int currentBlocksFromBranch = 3;

        public bool splitBranch = false;

        public double branchLengthDecay = 0.9;

        double currentX;
        double currentY;

        public int baseX;
        public int baseY;

        public double probabilityToGenerateLeavesOverWood = 0.1;


        WorldContext wc;
        WorldGenerator wg;

        public blockIDs treeTrunkBlock = blockIDs.treeTrunk;
        public blockIDs leafBlock = blockIDs.leaves;
        public override bool generate(int x, int y, WorldContext wc)
        {
            baseX = x;
            baseY = y;
            currentX = x;
            currentY = y;
            this.wc = wc;

            branchEnds.Clear();

            branches.Add(new BranchVariables(3 * Math.PI / 2, x, y, 0, branchLength, 0));

            while (generateBranch()) ;

            //Generate leaves from branch ends:
            generateLeaves();

            return true;
        }

        public override bool generate(int x, int y, WorldGenerator wg)
        {
            if (wg.worldArray[x, y - 1] == (int)blockIDs.air && (wg.worldArray[x, y + 1] == (int)blockIDs.dirt || wg.worldArray[x, y + 1] == (int)blockIDs.grass))
            {
                baseX = x;
                baseY = y;
                currentX = x;
                currentY = y;
                this.wg = wg;

                branchEnds.Clear();

                branches.Add(new BranchVariables(3 * Math.PI / 2, x, y, 0, branchLength, 0));

                while (generateBranchIntegerOutput()) ;

                //Generate leaves from branch ends:
                generateLeavesIntegerOutput();

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool generateBranch()
        {
            bool addedABlock = false;
            int i = branches.Count - 1;
            if (i >= 0)
            {
                //For each branch, set the current x and current y to the branches x and y
                //while (branches[i].currentLength < branchLength && currentX >= 0 && currentY >= 0 && currentX < wc.worldArray.GetLength(0) && currentY < wc.worldArray.GetLength(1))
                //{
                currentX = (currentX + Math.Cos(branches[i].angle));
                currentY = (currentY + Math.Sin(branches[i].angle));

                if (currentX >= 0 && currentY >= 0 && currentX < wc.worldArray.GetLength(0) && currentY < wc.worldArray.GetLength(1))
                {

                    branches[i].currentLength += 1;

                    if (branches[i].angle % MathHelper.TwoPi > 3 * Math.PI / 2 || branches[i].angle % MathHelper.TwoPi < MathHelper.PiOver2)
                    {
                        branches[i].angle += angleIncreasePerBlockAdded;
                    }
                    else if (branches[i].angle % MathHelper.TwoPi < 3 * Math.PI / 2 && branches[i].angle % MathHelper.TwoPi > MathHelper.PiOver2)
                    {
                        branches[i].angle -= angleIncreasePerBlockAdded;
                    }


                    wc.addBlock((int)currentX, (int)currentY, (int)treeTrunkBlock);
                    currentBlocksFromBranch += 1;
                    addedABlock = true;
                }
                else
                {
                    branchEnds.Add(new BranchEndVariables((int)currentX, (int)currentY, branches[i].branchLayerDepth));


                    currentX = branches[i].x;
                    currentY = branches[i].y;
                    currentBlocksFromBranch = 0;
                    branches.RemoveAt(i);
                    i = branches.Count - 1;
                    if (i >= 0) { addedABlock = true; }


                    return addedABlock;
                }

                Random r = new Random();

                if (i >= 0)
                {
                    if (branches[i].currentLength >= branches[i].maxBranchLength)
                    {
                        branchEnds.Add(new BranchEndVariables((int)currentX, (int)currentY, branches[i].branchLayerDepth));


                        currentX = branches[i].x;
                        currentY = branches[i].y;
                        currentBlocksFromBranch = 0;

                        //Add to the list of branch ends


                        branches.RemoveAt(i);
                        i = branches.Count - 1;
                        if (i >= 0) { addedABlock = true; }

                        return addedABlock;
                    }
                    else if (r.NextDouble() < branchProbability && blocksTilBranch <= currentBlocksFromBranch)
                    {
                        System.Diagnostics.Debug.WriteLine("Tried to branch");
                        branches[i].x = currentX;
                        branches[i].y = currentY;
                        //int angleDifferentSign = (r.Next(0, 2) * 2) - 1;
                        currentSign *= -1;
                        //A 10% angle variance
                        branches.Add(new BranchVariables(branches[i].angle + currentSign * branchAngle * (r.NextDouble() * 0.2 + 0.9), currentX, currentY, branches[i].currentLength, branches[i].maxBranchLength * branchLengthDecay, branches[i].branchLayerDepth + 1));
                        currentBlocksFromBranch = 0;
                        branches[i].angle -= Convert.ToInt32(splitBranch) * (currentSign * branchAngle);
                        //break;
                        addedABlock = true;
                    }
                }

                i = branches.Count - 1;
            }

            return addedABlock;


        }

        public void generateLeaves()
        {
            //Seed a brownian array:
            BlockGenerationVariables[,] leafArray = new BlockGenerationVariables[branchLength * 2, branchLength * 2];

            SeededBrownianMotion sbm = new SeededBrownianMotion();

            //Seed the array

            //The baseX and baseY are at the bottom centre of the array:
            for (int i = 0; i < branchEnds.Count; i++)
            {
                int relativeX = (branchEnds[i].x - baseX) + leafArray.GetLength(0) / 2;
                int relativeY = (branchEnds[i].y - baseY) + leafArray.GetLength(1);
                if (relativeX >= 0 && relativeY >= 0 && relativeX < leafArray.GetLength(0) && relativeY < leafArray.GetLength(1))
                {
                    leafArray[relativeX, relativeY] = new BlockGenerationVariables(0, new Block((int)leafBlock), 8, 10 / (branchEnds[i].branchLayerDepth + 1), (0.3, 0.05, 0.3, 0.05, 0.05, 0.05, 0.3, 0.05));
                }
            }


            //Spread the seeds
            leafArray = sbm.brownianAlgorithm(leafArray, 15, fillOutput: false);

            //Convert it to blocks in the world
            Random r = new Random();
            for (int x = 0; x < leafArray.GetLength(0); x++)
            {
                for (int y = 0; y < leafArray.GetLength(1); y++)
                {
                    if (leafArray[x, y] != null)
                    {
                        int localToGlobalX = (x - leafArray.GetLength(0) / 2) + baseX;
                        int localToGlobalY = (y - leafArray.GetLength(1)) + baseY;
                        if (localToGlobalX >= 0 && localToGlobalY >= 0 && localToGlobalX < wc.worldArray.GetLength(0) && localToGlobalY < wc.worldArray.GetLength(1))
                        {
                            if (wc.worldArray[localToGlobalX, localToGlobalY].ID == (int)blockIDs.air || r.NextDouble() < probabilityToGenerateLeavesOverWood)
                            {
                                wc.addBlock(x, y, leafArray[x, y].block.ID);
                            }
                        }
                    }
                }
            }
        }

        public bool generateBranchIntegerOutput()
        {
            bool addedABlock = false;
            int i = branches.Count - 1;
            if (i >= 0)
            {
                //For each branch, set the current x and current y to the branches x and y
                //while (branches[i].currentLength < branchLength && currentX >= 0 && currentY >= 0 && currentX < wc.worldArray.GetLength(0) && currentY < wc.worldArray.GetLength(1))
                //{
                currentX = (currentX + Math.Cos(branches[i].angle));
                currentY = (currentY + Math.Sin(branches[i].angle));

                if (currentX >= 0 && currentY >= 0 && currentX < wg.worldArray.GetLength(0) && currentY < wg.worldArray.GetLength(1))
                {

                    branches[i].currentLength += 1;

                    if (branches[i].angle % MathHelper.TwoPi > 3 * Math.PI / 2 || branches[i].angle % MathHelper.TwoPi < MathHelper.PiOver2)
                    {
                        branches[i].angle += angleIncreasePerBlockAdded;
                    }
                    else if (branches[i].angle % MathHelper.TwoPi < 3 * Math.PI / 2 && branches[i].angle % MathHelper.TwoPi > MathHelper.PiOver2)
                    {
                        branches[i].angle -= angleIncreasePerBlockAdded;
                    }


                    wg.worldArray[(int)currentX, (int)currentY] = (int)treeTrunkBlock;
                    currentBlocksFromBranch += 1;
                    addedABlock = true;
                }
                else
                {
                    branchEnds.Add(new BranchEndVariables((int)currentX, (int)currentY, branches[i].branchLayerDepth));


                    currentX = branches[i].x;
                    currentY = branches[i].y;
                    currentBlocksFromBranch = 0;
                    branches.RemoveAt(i);
                    i = branches.Count - 1;
                    if (i >= 0) { addedABlock = true; }


                    return addedABlock;
                }

                Random r = new Random();

                if (i >= 0)
                {
                    if (branches[i].currentLength >= branches[i].maxBranchLength)
                    {
                        branchEnds.Add(new BranchEndVariables((int)currentX, (int)currentY, branches[i].branchLayerDepth));


                        currentX = branches[i].x;
                        currentY = branches[i].y;
                        currentBlocksFromBranch = 0;

                        //Add to the list of branch ends


                        branches.RemoveAt(i);
                        i = branches.Count - 1;
                        if (i >= 0) { addedABlock = true; }

                        return addedABlock;
                    }
                    else if (r.NextDouble() < branchProbability && blocksTilBranch <= currentBlocksFromBranch)
                    {
                        System.Diagnostics.Debug.WriteLine("Tried to branch");
                        branches[i].x = currentX;
                        branches[i].y = currentY;
                        //int angleDifferentSign = (r.Next(0, 2) * 2) - 1;
                        currentSign *= -1;
                        //A 10% angle variance
                        branches.Add(new BranchVariables(branches[i].angle + currentSign * branchAngle * (r.NextDouble() * 0.2 + 0.9), currentX, currentY, branches[i].currentLength, branches[i].maxBranchLength * branchLengthDecay, branches[i].branchLayerDepth + 1));
                        currentBlocksFromBranch = 0;
                        branches[i].angle -= Convert.ToInt32(splitBranch) * (currentSign * branchAngle);
                        //break;
                        addedABlock = true;
                    }
                }

                i = branches.Count - 1;
            }

            return addedABlock;


        }

        public void generateLeavesIntegerOutput()
        {
            //Seed a brownian array:
            BlockGenerationVariables[,] leafArray = new BlockGenerationVariables[branchLength * 2, branchLength * 2];

            SeededBrownianMotion sbm = new SeededBrownianMotion();

            //Seed the array

            //The baseX and baseY are at the bottom centre of the array:
            for (int i = 0; i < branchEnds.Count; i++)
            {
                int relativeX = (branchEnds[i].x - baseX) + leafArray.GetLength(0) / 2;
                int relativeY = (branchEnds[i].y - baseY) + leafArray.GetLength(1);
                if (relativeX >= 0 && relativeY >= 0 && relativeX < leafArray.GetLength(0) && relativeY < leafArray.GetLength(1))
                {
                    leafArray[relativeX, relativeY] = new BlockGenerationVariables(0, new Block((int)leafBlock), 8, 10 / (branchEnds[i].branchLayerDepth + 1), (0.3, 0.05, 0.3, 0.05, 0.05, 0.05, 0.3, 0.05));
                }
            }


            //Spread the seeds
            leafArray = sbm.brownianAlgorithm(leafArray, 15, fillOutput: false);

            for (int x = 0; x < leafArray.GetLength(0); x++)
            {
                for (int y = 0; y < leafArray.GetLength(1); y++)
                {
                    if (leafArray[x, y] != null)
                    {
                        if (leafArray[x, y].block.ID == (int)blockIDs.leaves)
                        {
                            for (int checkX = x - 1; checkX <= x + 1; checkX++)
                            {
                                for (int checkY = y - 1; checkY <= y + 1; checkY++)
                                {
                                    if (checkX >= 0 && checkX < leafArray.GetLength(0) && checkY >= 0 && checkY < leafArray.GetLength(1))
                                    {
                                        if (leafArray[checkX, checkY] == null)
                                        {
                                            leafArray[checkX, checkY] = new BlockGenerationVariables(0, new SemiLeafBlock((int)blockIDs.semiLeaves), 0, 0);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //Convert it to blocks in the world
            Random r = new Random();
            for (int x = 0; x < leafArray.GetLength(0); x++)
            {
                for (int y = 0; y < leafArray.GetLength(1); y++)
                {
                    if (leafArray[x, y] != null)
                    {
                        int localToGlobalX = (x - leafArray.GetLength(0) / 2) + baseX;
                        int localToGlobalY = (y - leafArray.GetLength(1)) + baseY;
                        if (localToGlobalX >= 0 && localToGlobalY >= 0 && localToGlobalX < wg.worldArray.GetLength(0) && localToGlobalY < wg.worldArray.GetLength(1))
                        {
                            if (wg.worldArray[localToGlobalX, localToGlobalY] == (int)blockIDs.air || r.NextDouble() < probabilityToGenerateLeavesOverWood)
                            {
                                wg.worldArray[localToGlobalX, localToGlobalY] = leafArray[x, y].block.ID;
                            }
                        }
                    }
                }
            }
        }

    }

    public class BranchVariables
    {
        public double angle;
        public double x;
        public double y;
        public int currentLength;
        public double maxBranchLength;
        public int branchLayerDepth;

        public BranchVariables(double angle, double x, double y, int currentLength, double maxBranchLength, int branchLayerDepth)
        {
            this.angle = angle;
            this.x = x;
            this.y = y;
            this.currentLength = currentLength;
            this.maxBranchLength = maxBranchLength;
            this.branchLayerDepth = branchLayerDepth;
        }
    }

    public class BranchEndVariables
    {
        public int x;
        public int y;
        public int branchLayerDepth;

        public BranchEndVariables(int x, int y, int branchLayerDepth)
        {
            this.x = x;
            this.y = y;
            this.branchLayerDepth = branchLayerDepth;
        }
    }

    public class BushGeneration : Decoration
    {

        public blockIDs bushBlock = blockIDs.bush;
        public override bool generate(int x, int y, WorldContext wc)
        {
            if (wc.worldArray[x, y].ID == (int)blockIDs.air && wc.worldArray[x, y + 1].ID == (int)blockIDs.dirt || wc.worldArray[x, y + 1].ID == (int)blockIDs.grass)
            {
                wc.addBlock(x, y, (int)bushBlock);
                return true;

            }
            else
            {
                return false;
            }
        }

        public override bool generate(int x, int y, WorldGenerator wg)
        {
            if (wg.worldArray[x, y - 1] == (int)blockIDs.air && wg.worldArray[x, y + 1] == (int)blockIDs.dirt || wg.worldArray[x, y + 1] == (int)blockIDs.grass)
            {
                wg.worldArray[x, y - 1] = (int)bushBlock;
                return true;

            }
            else
            {
                return false;
            }

        }
    }

    public class BigBushGeneration : Decoration
    {

        public blockIDs bushBlock = blockIDs.bigBush;
        public override bool generate(int x, int y, WorldContext wc)
        {
            if (wc.worldArray[x, y].ID == (int)blockIDs.air && (wc.worldArray[x, y + 1].ID == (int)blockIDs.dirt || wc.worldArray[x, y + 1].ID == (int)blockIDs.grass))
            {
                wc.addBlock(x, y, (int)bushBlock);
                return true;

            }
            else
            {
                return false;
            }
        }

        public override bool generate(int x, int y, WorldGenerator wg)
        {
            if (wg.worldArray[x, y - 1] == (int)blockIDs.air && (wg.worldArray[x, y] == (int)blockIDs.dirt || wg.worldArray[x, y] == (int)blockIDs.grass) && wg.worldArray[x + 1, y - 1] == (int)blockIDs.air && (wg.worldArray[x + 1, y] == (int)blockIDs.dirt || wg.worldArray[x + 1, y] == (int)blockIDs.grass))
            {
                wg.worldArray[x, y - 1] = (int)bushBlock;
                return true;

            }
            else
            {
                return false;
            }
        }
    }
}
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace PixelMidpointDisplacement {

    /*
     * ========================================
     * 
     * Block parent classes
     * 
     *  The different possible types of blocks, and the generalised function of each
     *  
     *  Block is a generic block that has a some default values
     *  Fluid Block is a block that flows in the direction of the fluids gravity and then spreads outwards
     *  Interactive Block is a block that has functionality when right clicked on
     *  Ore Block is a generic block that drops ores instead of regular blocks. Contains an Ore ID to allow for identification
     * ========================================
    */
    public class Block : DrawnClass
    {
        public int emmissiveStrength;
        public int ID;
        public List<Vector2> faceVertices;

        public double coefficientOfFriction = 4;

        public bool isBlockTransparent = false;
        public bool hasTransparency = false;
        public Vector4 faceDirection;

        public double hardness = 15;
        public double durability = 3;
        public Block(Rectangle textureSourceRectangle, int ID)
        {
            this.sourceRectangle = textureSourceRectangle;
            this.ID = ID;
            drawRectangle = new Rectangle(0, 0, 1, 1);
        }
        public Block(Rectangle textureSourceRectangle, int emmissiveStrength, int ID)
        {
            this.sourceRectangle = textureSourceRectangle;
            this.emmissiveStrength = emmissiveStrength;
            this.ID = ID;
            drawRectangle = new Rectangle(0, 0, 1, 1);

        }
        public Block(int ID)
        {
            this.ID = ID;

        }

        public Block(Block b)
        {
            sourceRectangle = b.sourceRectangle;
            emmissiveStrength = b.emmissiveStrength;
            ID = b.ID;
            drawRectangle = b.drawRectangle;
            x = b.x;
            y = b.y;
            drawRectangle = new Rectangle(0, 0, 1, 1);
        }

        public void setLocation((int x, int y) location)
        {

            x = location.x;
            y = location.y;
        }

        //A block specific check if that block can be placed. For example, torches, chests etc.
        public virtual bool canBlockBePlaced(WorldContext worldContext, (int x, int y) location)
        {
            return true;
        }
        public virtual void onBlockPlaced(WorldContext worldContext, (int x, int y) location)
        {
            setLocation(location);
        }
        public virtual void onBlockDestroyed(Dictionary<(int x, int y), Block> exposedBlocks, WorldContext wc)
        {
            blockDestroyed(exposedBlocks);
            Random r = new Random();

            DroppedItem dropBlock = new DroppedItem(wc, new BlockItem(ID), (x * wc.pixelsPerBlock + wc.pixelsPerBlock / 2.0, y), new Vector2((float)r.NextDouble(), (float)r.NextDouble()));

            dropBlock.y = y * wc.pixelsPerBlock + wc.pixelsPerBlock / 2.0;
            dropBlock.pickupDelay = 0f;
        }
        public void blockDestroyed(Dictionary<(int x, int y), Block> exposedBlocks)
        {
            if (exposedBlocks.ContainsKey(((int)x, (int)y))) { exposedBlocks.Remove(((int)x, (int)y)); }
        }

        public virtual void updateBlock(WorldContext wc)
        {

        }
        public virtual void setupInitialData(WorldContext worldContext, int[,] worldArray, (int x, int y) blockLocation)
        {
            x = blockLocation.x;
            y = blockLocation.y;
        }

        public virtual void setupFaceVertices(Vector4 exposedFacesClockwise)
        {
            this.faceDirection = exposedFacesClockwise;
            //2 Vector2s are needed to allow for all 4 directions to be accounted for. However, this isn't the cleanest code and should be later improved
            faceVertices = new List<Vector2>();
            if (exposedFacesClockwise.X == 1)
            {
                faceVertices.Add(new Vector2((int)x, (int)y));
                faceVertices.Add(new Vector2((int)x + drawRectangle.Width, (int)y));
            }
            if (exposedFacesClockwise.Y == 1)
            {
                //Check if the vertex already exists from the previous if statement
                if (!faceVertices.Contains(new Vector2((int)x + drawRectangle.Width, (int)y)))
                {

                    faceVertices.Add(new Vector2((int)x + drawRectangle.Width, (int)y));
                }


                faceVertices.Add(new Vector2((int)x + drawRectangle.Width, (int)y + drawRectangle.Height));
            }
            if (exposedFacesClockwise.Z == 1)
            {
                if (!faceVertices.Contains(new Vector2((int)x + drawRectangle.Width, (int)y + drawRectangle.Height)))
                {
                    faceVertices.Add(new Vector2((int)x + drawRectangle.Width, (int)y + drawRectangle.Height));
                }

                faceVertices.Add(new Vector2((int)x, (int)y + drawRectangle.Height));
            }
            if (exposedFacesClockwise.W == 1)
            {
                if (!faceVertices.Contains(new Vector2((int)x, (int)y + drawRectangle.Height)))
                {
                    faceVertices.Add(new Vector2((int)x, (int)y + drawRectangle.Height));
                }

                faceVertices.Add(new Vector2((int)x, (int)y));
            }
        }

        public virtual void onCollisionWithPhysicsObject(PhysicsObject entity, PhysicsEngine physicsEngine, WorldContext wc)
        {
            Rectangle entityCollider = new Rectangle((int)entity.x, (int)entity.y, entity.collider.Width, entity.collider.Height);
            Rectangle blockRect = new Rectangle((int)x * wc.pixelsPerBlock, (int)y * wc.pixelsPerBlock, wc.pixelsPerBlock, wc.pixelsPerBlock);
            Vector2 collisionNormal = physicsEngine.computeCollisionNormal(entityCollider, blockRect);
            entity.hasCollided();

            //If the signs are unequal on either the velocity or the acceleration then the forces should cancel as the resulting motion would be counteracted by the block
            if (((Math.Sign(collisionNormal.Y) != Math.Sign(entity.velocityY) && entity.velocityY != 0) || (Math.Sign(collisionNormal.Y) != Math.Sign(entity.accelerationY) && entity.accelerationY != 0)) && collisionNormal.Y != 0)
            {
                entity.velocityY -= (1 + entity.bounceCoefficient) * entity.velocityY;
                entity.accelerationY -= entity.accelerationY;

                if (Math.Sign(collisionNormal.Y) > 0)
                {
                    entity.isOnGround = true;
                    //Set the coefficient of friction if the current block has a greater friction value than the previous maximum
                    if (entity.cummulativeCoefficientOfFriction < coefficientOfFriction + entity.objectCoefficientOfFriction)
                    {
                        entity.cummulativeCoefficientOfFriction = coefficientOfFriction + entity.objectCoefficientOfFriction;
                    }

                }

                if (Math.Sign(collisionNormal.Y) > 0)
                {
                    entity.y = blockRect.Y - entityCollider.Height + 1;
                }
                else
                {
                    entity.y = blockRect.Bottom - 1;
                }
            }

            if (((Math.Sign(collisionNormal.X) != Math.Sign(entity.velocityX) && entity.velocityX != 0) || (Math.Sign(collisionNormal.X) != Math.Sign(entity.accelerationX) && entity.accelerationX != 0)) && collisionNormal.X != 0)
            {


                entity.velocityX -= (1 + entity.bounceCoefficient) * entity.velocityX;
                entity.accelerationX -= entity.accelerationX;

                if (Math.Sign(collisionNormal.X) > 0)
                {
                    entity.x = blockRect.Right - 1;
                }
                else
                {
                    entity.x = blockRect.Left - entityCollider.Width + 1;
                }

            }

        }

        public virtual Block copyBlock()
        {
            return new Block(this);
        }
    }
    public class FluidBlock : Block
    {

        public bool isSourceBlock = true;
        public int sourceX;
        public int sourceY;

        public int distanceFromLastDown = 0;
        public int maxDistanceFromLastDown = 14;

        public bool deleteNextFrame = false;

        public bool addNextFrame = false;
        public int gridXToAdd;
        public int gridYToAdd;

        const int textureSize = 32;

        public double viscosityX = 200;
        public double viscosityY = 50;


        int leftFlowingY;
        int rightFlowingY;

        public Vector2 flowingDirection = new Vector2(0, 0);

        int gravity = 1;


        public double fluidBuoyancy = 20;
        const double maxBuoyancyVelocity = 5;


        public double fluidFlowForce = 25;
        public double maxFlowVelocity = 4;


        public FluidBlock(Rectangle textureSourceRectangle, int ID) : base(textureSourceRectangle, ID)
        {
            rightFlowingY = textureSourceRectangle.Y;
            leftFlowingY = rightFlowingY + textureSize;
            isBlockTransparent = true;
        }

        public FluidBlock(Rectangle textureSourceRectangle, int emmissiveStrength, int ID) : base(textureSourceRectangle, emmissiveStrength, ID)
        {
            isBlockTransparent = true;
            rightFlowingY = textureSourceRectangle.Y;
            leftFlowingY = rightFlowingY + textureSize;
        }

        public FluidBlock(int ID) : base(ID)
        {
            isBlockTransparent = true;
        }

        public FluidBlock(Block b) : base(b)
        {
            isBlockTransparent = true;
            rightFlowingY = b.sourceRectangle.Y;
            leftFlowingY = rightFlowingY + textureSize;
        }

        public void setSource(int x, int y)
        {
            sourceX = x;
            sourceY = y;
        }

        public override void onCollisionWithPhysicsObject(PhysicsObject entity, PhysicsEngine physicsEngine, WorldContext wc)
        {
            //I think all I want, is if it collides with the smaller rectangle, apply a bouyant force:
            Rectangle entityCollider = new Rectangle((int)entity.x, (int)entity.y, entity.collider.Width, entity.collider.Height);

            int fluidHeight = wc.pixelsPerBlock / (distanceFromLastDown + 1);

            Rectangle blockRect = new Rectangle(((int)(x + 1) * wc.pixelsPerBlock) - fluidHeight, (int)y * wc.pixelsPerBlock, wc.pixelsPerBlock, fluidHeight);

            Vector2 collisionNormal = physicsEngine.computeCollisionNormal(entityCollider, blockRect);


            if (entityCollider.Intersects(blockRect))
            {


                if (entity.kX < viscosityX * entity.defaultkX)
                {
                    entity.kX = viscosityX * entity.defaultkX;
                }
                if (entity.kY < viscosityY * entity.defaultkY)
                {
                    entity.kY = viscosityY * entity.defaultkY;
                }

                if (entity.velocityY < maxBuoyancyVelocity && !entity.isInFluid)
                {
                    entity.accelerationY += fluidBuoyancy * entity.buoyancyCoefficient;
                }
                if (entity.velocityX < maxFlowVelocity)
                {
                    entity.accelerationX += flowingDirection.X * fluidFlowForce;
                    entity.accelerationY += flowingDirection.Y * fluidFlowForce;
                }
                entity.isInFluid = true;

            }
        }

        public void setDistanceFromLastDown(int distance)
        {
            distanceFromLastDown = distance;

            sourceRectangle.X = distance * textureSize;
        }

        public override void onBlockPlaced(WorldContext worldContext, (int x, int y) location)
        {
            base.onBlockPlaced(worldContext, location);
            setSource(location.x, location.y);
        }

        public override void onBlockDestroyed(Dictionary<(int x, int y), Block> exposedBlocks, WorldContext wc)
        {
            if (isSourceBlock)
            {
                base.onBlockDestroyed(exposedBlocks, wc);
            }
        }

        public void tickFluid(WorldContext wc)
        {
            if (deleteNextFrame)
            {
                wc.deleteBlock((int)x, (int)y);
            }
            else
            {
                if (addNextFrame)
                {
                    wc.deleteBlock(gridXToAdd, gridYToAdd);
                    wc.addBlock(gridXToAdd, gridYToAdd, ID);
                    if (wc.worldArray[gridXToAdd, gridYToAdd] is FluidBlock f)
                    {
                        if (gridXToAdd < x)
                        {
                            f.sourceRectangle.Y = leftFlowingY;
                            f.flowingDirection.X = -1;
                        }
                        if (gridXToAdd > x)
                        {
                            f.flowingDirection.X = 1;
                        }
                        f.isSourceBlock = false;
                        if (gravity > 0 ? gridYToAdd > y : gridYToAdd < y)
                        {
                            f.setDistanceFromLastDown(0);
                            f.flowingDirection.X = 0;
                        }
                        else
                        {
                            f.setDistanceFromLastDown(distanceFromLastDown + 1);
                        }
                        f.setSource((int)x, (int)y);
                        addNextFrame = false;
                    }
                }
                if (!isSourceBlock)
                {
                    //Find if any block around them is a fluid: if not, then terminate itself (ID == 0)
                    bool hasAdjacentFluid = false;

                    if (wc.worldArray[sourceX, sourceY].ID == ID)
                    {
                        hasAdjacentFluid = true;
                    }


                    if (hasAdjacentFluid == false)
                    {
                        deleteNextFrame = true;
                    }
                }

                if (y < wc.worldArray.GetLength(1) - gravity && distanceFromLastDown < maxDistanceFromLastDown)
                {
                    if ((wc.worldArray[(int)x, (int)y + gravity].ID == (int)blockIDs.air || wc.worldArray[(int)x, (int)y + gravity].isBlockTransparent) && wc.worldArray[(int)x, (int)y + gravity].ID != (int)blockIDs.chest && wc.worldArray[(int)x, (int)y + gravity] is not FluidBlock)
                    {
                        //spread downwards
                        addNextFrame = true;
                        gridXToAdd = (int)x;
                        gridYToAdd = (int)y + gravity;
                    }
                    else
                    {
                        if (wc.worldArray[(int)x, (int)y + gravity].ID != ID)
                        {
                            //Check either side
                            if (x < wc.worldArray.GetLength(0) - 1)
                            {
                                if ((wc.worldArray[(int)x + 1, (int)y].ID == (int)blockIDs.air || wc.worldArray[(int)x + 1, (int)y].isBlockTransparent) && wc.worldArray[(int)x + 1, (int)y].ID != (int)blockIDs.chest && wc.worldArray[(int)x + 1, (int)y] is not FluidBlock)
                                {
                                    addNextFrame = true;
                                    gridXToAdd = (int)x + 1;
                                    gridYToAdd = (int)y;
                                }
                            }
                            if (x > 0)
                            {
                                if ((wc.worldArray[(int)x - 1, (int)y].ID == (int)blockIDs.air || wc.worldArray[(int)x - 1, (int)y].isBlockTransparent) && wc.worldArray[(int)x - 1, (int)y].ID != (int)blockIDs.chest && wc.worldArray[(int)x - 1, (int)y] is not FluidBlock)
                                {
                                    addNextFrame = true;
                                    gridXToAdd = (int)x - 1;
                                    gridYToAdd = (int)y;

                                }
                            }
                        }

                    }
                }

            }
        }

        public override Block copyBlock()
        {
            return new FluidBlock(sourceRectangle, ID);
        }
    }
    public class InteractiveBlock : Block
    {
        public double secondsSinceAction;
        public double maximumCooldown;

        public InteractiveBlock(Rectangle textureSourceRectangle, int ID) : base(textureSourceRectangle, ID) { }

        //This one is slightly outdated?
        public InteractiveBlock(Rectangle textureSourceRectangle, int emmissiveStrength, int ID) : base(textureSourceRectangle, emmissiveStrength, ID)
        { }

        public InteractiveBlock(int ID) : base(ID)
        {

        }

        public InteractiveBlock(Block b) : base(b)
        {

        }
        public virtual void onRightClick(WorldContext worldContext, GameTime gameTime)
        {
            //Execute some code here. Perhaps pass in some variable data, such as world context or whatever, we'll just add what's needed
        }
    }
    public class OreBlock : Block
    {
        int oreID;



        public OreBlock(Rectangle textureSourceRectangle, int ID, int oreID) : base(textureSourceRectangle, ID)
        {
            this.sourceRectangle = textureSourceRectangle;
            this.oreID = oreID;
        }
        public OreBlock(Rectangle textureSourceRectangle, int emmissiveStrength, int ID, int oreID) : base(textureSourceRectangle, emmissiveStrength, ID)
        {
            this.sourceRectangle = textureSourceRectangle;
            this.emmissiveStrength = emmissiveStrength;
            this.oreID = oreID;
        }
        public OreBlock(int ID) : base(ID)
        {
            this.ID = ID;
        }

        public OreBlock(Block b) : base(b)
        {
            sourceRectangle = b.sourceRectangle;
            emmissiveStrength = b.emmissiveStrength;
            ID = b.ID;
            hardness = 15;
            durability = 5;
            if (b is OreBlock ob)
            {
                oreID = ob.oreID;
            }
        }

        public override void onBlockDestroyed(Dictionary<(int x, int y), Block> exposedBlocks, WorldContext wc)
        {
            blockDestroyed(exposedBlocks);
            Random r = new Random();
            DroppedItem dropBlock = new DroppedItem(wc, new OreItem(oreID), (x, y), new Vector2((float)r.NextDouble(), (float)r.NextDouble()));

            dropBlock.x = x * wc.pixelsPerBlock + wc.pixelsPerBlock / 2.0;
            dropBlock.y = y * wc.pixelsPerBlock + wc.pixelsPerBlock / 2.0;
            dropBlock.pickupDelay = 0f;
        }

        public override Block copyBlock()
        {
            return new OreBlock(this);
        }
    }

    /*
     * ========================================
     * 
     * Blocks
     *  
     *  Generic block classes that contain their own special functionality
     *  
     *  GrassBlock
     *  TorchBlock
     *  TreeBlock
     *  LeafBlock
     *  SemiLeafBlock
     *  BushBlock
     *  BigBushBlock
     * ========================================
    */
    public class GrassBlock : Block
    {


        public GrassBlock(Rectangle textureSourceRectangle, int ID) : base(textureSourceRectangle, ID)
        {
            this.sourceRectangle = textureSourceRectangle;
        }
        public GrassBlock(Rectangle textureSourceRectangle, int emmissiveStrength, int ID) : base(textureSourceRectangle, emmissiveStrength, ID)
        {
            this.sourceRectangle = textureSourceRectangle;
            this.emmissiveStrength = emmissiveStrength;
        }
        public GrassBlock(int ID) : base(ID)
        {
            this.ID = ID;
        }

        public GrassBlock(Block b) : base(b)
        {
            sourceRectangle = b.sourceRectangle;
            emmissiveStrength = b.emmissiveStrength;
            ID = b.ID;
            hardness = 15;
            durability = 5;
        }

        public override void onBlockDestroyed(Dictionary<(int x, int y), Block> exposedBlocks, WorldContext wc)
        {
            blockDestroyed(exposedBlocks);
            Random r = new Random();
            DroppedItem dropBlock = new DroppedItem(wc, new BlockItem((int)blockIDs.dirt), (x, y), new Vector2((float)r.NextDouble(), (float)r.NextDouble()));

            dropBlock.x = x * wc.pixelsPerBlock + wc.pixelsPerBlock / 2.0;
            dropBlock.y = y * wc.pixelsPerBlock + wc.pixelsPerBlock / 2.0;
            dropBlock.pickupDelay = 0f;
        }

        public override void setupInitialData(WorldContext worldContext, int[,] worldArray, (int x, int y) blockLocation)
        {
            bool emptyAbove = false;
            bool emptyRight = false;
            bool emptyLeft = false;

            int xOffset = 2; //Set it to the default upwards block
            //sprite sheet is as follows: |, |-, _, -|, |

            if (blockLocation.x > 0 && blockLocation.y > 0 && blockLocation.x < worldArray.GetLength(0) - 1 && blockLocation.y < worldArray.GetLength(1) - 1)
            {



                if (worldArray[blockLocation.x, blockLocation.y - 1] == 0)
                {
                    emptyAbove = true;
                }
                if (worldArray[blockLocation.x - 1, blockLocation.y] == 0)
                {
                    emptyLeft = true;
                }
                if (worldArray[blockLocation.x + 1, blockLocation.y] == 0)
                {
                    emptyRight = true;
                }
                if (emptyRight && !emptyLeft && !emptyAbove)
                {
                    xOffset = 4;
                }
                else if (emptyLeft && !emptyRight && !emptyAbove)
                {
                    xOffset = 0;
                }

                if (emptyAbove)
                {
                    if (emptyRight)
                    {
                        xOffset = 3;
                    }
                    else if (emptyLeft)
                    {
                        xOffset = 1;
                    }
                }
            }

            sourceRectangle = new Rectangle(sourceRectangle.X + xOffset * 32, sourceRectangle.Y, 32, 32);

            base.setupInitialData(worldContext, worldArray, blockLocation);
        }


        public override Block copyBlock()
        {
            return new GrassBlock(this);
        }
    }

    public class TorchBlock : Block, IEmissiveBlock
    {

        public Vector3 lightColor { get; set; }
        public float luminosity { get; set; }
        public float range { get; set; }
        public RenderTarget2D shadowMap { get; set; }
        public RenderTarget2D lightMap { get; set; }



        public TorchBlock(Rectangle textureSourceRectangle, int ID) : base(textureSourceRectangle, ID)
        {
            this.sourceRectangle = textureSourceRectangle;
            setData();
        }
        public TorchBlock(Rectangle textureSourceRectangle, int emmissiveStrength, int ID) : base(textureSourceRectangle, emmissiveStrength, ID)
        {
            this.sourceRectangle = textureSourceRectangle;
            this.emmissiveStrength = emmissiveStrength;
            setData();
        }
        public TorchBlock(int ID) : base(ID)
        {
            this.ID = ID;
            setData();
        }

        public TorchBlock(Block b) : base(b)
        {
            sourceRectangle = b.sourceRectangle;
            emmissiveStrength = b.emmissiveStrength;
            ID = b.ID;
            setData();
        }
        public void setData()
        {
            isBlockTransparent = true;
            lightColor = new Vector3(1, 0.2f, 0.1f);
            luminosity = 1000f;
            range = 496;
        }


        public override bool canBlockBePlaced(WorldContext worldContext, (int x, int y) location)
        {
            bool isASolidBlockPresent = false;


            if (worldContext.worldArray[location.x - 1, location.y].ID != (int)blockIDs.air && !worldContext.worldArray[location.x - 1, location.y].isBlockTransparent)
            {
                isASolidBlockPresent = true;
            }
            else if (worldContext.worldArray[location.x + 1, location.y].ID != (int)blockIDs.air && !worldContext.worldArray[location.x + 1, location.y].isBlockTransparent)
            {
                isASolidBlockPresent = true;
            }
            else if (worldContext.worldArray[location.x, location.y + 1].ID != (int)blockIDs.air && !worldContext.worldArray[location.x, location.y + 1].isBlockTransparent)
            {
                isASolidBlockPresent = true;
            }
            else if (worldContext.backgroundArray[location.x, location.y] != (int)backgroundBlockIDs.air)
            {
                isASolidBlockPresent = true;
            }

            return isASolidBlockPresent;
        }
        public override void onBlockPlaced(WorldContext worldContext, (int x, int y) location)
        {
            base.onBlockPlaced(worldContext, location);
            sourceRectangle = new Rectangle(0, 96, 32, 32);
            calculateVariant(worldContext.worldArray, location.x, location.y);
            shadowMap = new RenderTarget2D(worldContext.engineController.lightingSystem.graphics.GraphicsDevice, (int)(worldContext.engineController.lightingSystem.graphics.PreferredBackBufferWidth * worldContext.engineController.lightingSystem.shaderPrecision), (int)(worldContext.engineController.lightingSystem.graphics.PreferredBackBufferHeight * worldContext.engineController.lightingSystem.shaderPrecision));
            lightMap = new RenderTarget2D(worldContext.engineController.lightingSystem.graphics.GraphicsDevice, (int)(worldContext.engineController.lightingSystem.graphics.PreferredBackBufferWidth * worldContext.engineController.lightingSystem.shaderPrecision), (int)(worldContext.engineController.lightingSystem.graphics.PreferredBackBufferHeight * worldContext.engineController.lightingSystem.shaderPrecision));
            setData();
            if (!worldContext.engineController.lightingSystem.emissiveBlocks.Contains(this))
            {
                worldContext.engineController.lightingSystem.emissiveBlocks.Add(this);
            }
        }

        public override void setupInitialData(WorldContext worldContext, int[,] worldArray, (int x, int y) blockLocation)
        {
            base.setupInitialData(worldContext, worldArray, blockLocation);
            sourceRectangle = new Rectangle(0, 96, 32, 32);
            shadowMap = new RenderTarget2D(worldContext.engineController.lightingSystem.graphics.GraphicsDevice, (int)(worldContext.engineController.lightingSystem.graphics.PreferredBackBufferWidth * worldContext.engineController.lightingSystem.shaderPrecision), (int)(worldContext.engineController.lightingSystem.graphics.PreferredBackBufferHeight * worldContext.engineController.lightingSystem.shaderPrecision));
            lightMap = new RenderTarget2D(worldContext.engineController.lightingSystem.graphics.GraphicsDevice, (int)(worldContext.engineController.lightingSystem.graphics.PreferredBackBufferWidth * worldContext.engineController.lightingSystem.shaderPrecision), (int)(worldContext.engineController.lightingSystem.graphics.PreferredBackBufferHeight * worldContext.engineController.lightingSystem.shaderPrecision));
            setData();
            if (!worldContext.engineController.lightingSystem.emissiveBlocks.Contains(this))
            {
                worldContext.engineController.lightingSystem.emissiveBlocks.Add(this);
            }
        }
        public override void setupFaceVertices(Vector4 exposedFacesClockwise)
        {
            //the block is transparent...
        }
        public override void onBlockDestroyed(Dictionary<(int x, int y), Block> exposedBlocks, WorldContext wc)
        {
            wc.engineController.lightingSystem.emissiveBlocks.Remove(this);
            base.onBlockDestroyed(exposedBlocks, wc);
        }

        public void calculateVariant(Block[,] worldArray, int x, int y)
        {
            //Presumes that the torch can in fact be placed
            bool isSolidBelow = false;
            bool isSolidLeft = false;
            bool isSolidRight = false;


            if (worldArray[x - 1, y].ID != (int)blockIDs.air && !worldArray[x - 1, y].isBlockTransparent)
            {
                isSolidLeft = true;
            }
            if (worldArray[x + 1, y].ID != (int)blockIDs.air && !worldArray[x + 1, y].isBlockTransparent)
            {
                isSolidRight = true;
            }
            if (worldArray[x, y + 1].ID != (int)blockIDs.air && !worldArray[x, y + 1].isBlockTransparent)
            {
                isSolidBelow = true;
            }

            if (isSolidBelow)
            {
                //Don't change the source rect, as the default is towards the bottom
            }
            else if (isSolidLeft)
            {
                sourceRectangle.X = 32;
            }
            else if (isSolidRight)
            {
                sourceRectangle.X = 64;
            }


        }
        public override void onCollisionWithPhysicsObject(PhysicsObject entity, PhysicsEngine physicsEngine, WorldContext wc)
        {
            //Null the default collision logic
        }

        public override Block copyBlock()
        {
            return new TorchBlock(this);
        }
    }
    public class TreeBlock : Block
    {
        public TreeBlock(Rectangle textureSourceRectangle, int ID) : base(textureSourceRectangle, ID)
        {
            this.sourceRectangle = textureSourceRectangle;
        }
        public TreeBlock(Rectangle textureSourceRectangle, int emmissiveStrength, int ID) : base(textureSourceRectangle, emmissiveStrength, ID)
        {
            this.sourceRectangle = textureSourceRectangle;
            this.emmissiveStrength = emmissiveStrength;
        }
        public TreeBlock(int ID) : base(ID)
        {
            this.ID = ID;
        }

        public TreeBlock(Block b) : base(b)
        {
            sourceRectangle = b.sourceRectangle;
            emmissiveStrength = b.emmissiveStrength;
            ID = b.ID;
            hardness = 15;
            durability = 4;

        }

        public override void onCollisionWithPhysicsObject(PhysicsObject entity, PhysicsEngine physicsEngine, WorldContext wc)
        {

        }

        public override Block copyBlock()
        {
            return new TreeBlock(this);
        }
    }

    public class LeafBlock : Block
    {
        public LeafBlock(Rectangle textureSourceRectangle, int ID) : base(textureSourceRectangle, ID)
        {
            this.sourceRectangle = textureSourceRectangle;
        }
        public LeafBlock(Rectangle textureSourceRectangle, int emmissiveStrength, int ID) : base(textureSourceRectangle, emmissiveStrength, ID)
        {
            this.sourceRectangle = textureSourceRectangle;
            this.emmissiveStrength = emmissiveStrength;
        }
        public LeafBlock(int ID) : base(ID)
        {
            this.ID = ID;
        }

        public LeafBlock(Block b) : base(b)
        {
            sourceRectangle = b.sourceRectangle;
            emmissiveStrength = b.emmissiveStrength;
            ID = b.ID;
            hardness = 1;
            durability = 1;
        }

        public override void onCollisionWithPhysicsObject(PhysicsObject entity, PhysicsEngine physicsEngine, WorldContext wc)
        {

        }
        
        public override void onBlockDestroyed(Dictionary<(int x, int y), Block> exposedBlocks, WorldContext wc)
        {
            blockDestroyed(exposedBlocks);
        }

        public override Block copyBlock()
        {
            return new LeafBlock(this);
        }
    }

    public class SemiLeafBlock : Block
    {
        public SemiLeafBlock(Rectangle textureSourceRectangle, int ID) : base(textureSourceRectangle, ID)
        {
            this.sourceRectangle = textureSourceRectangle;
            isBlockTransparent = true;

        }
        public SemiLeafBlock(Rectangle textureSourceRectangle, int emmissiveStrength, int ID) : base(textureSourceRectangle, emmissiveStrength, ID)
        {
            this.sourceRectangle = textureSourceRectangle;
            this.emmissiveStrength = emmissiveStrength;
            isBlockTransparent = true;

        }
        public SemiLeafBlock(int ID) : base(ID)
        {
            this.ID = ID;
            isBlockTransparent = true;

        }

        public SemiLeafBlock(Block b) : base(b)
        {
            sourceRectangle = b.sourceRectangle;
            emmissiveStrength = b.emmissiveStrength;
            ID = b.ID;
            hardness = 1;
            durability = 1;
            isBlockTransparent = true;
        }

        public override void onCollisionWithPhysicsObject(PhysicsObject entity, PhysicsEngine physicsEngine, WorldContext wc)
        {

        }

        public override void setupInitialData(WorldContext worldContext, int[,] worldArray, (int x, int y) blockLocation)
        {
            //check all the surrounding blocks to see if they're leaves:
            bool[] directions = new bool[8];
            int i = 0;
            for (int y = -1; y <= +1; y++)
            {
                for (int x = -1; x <= +1; x++)
                {
                    if (!(x == 0 && y == 0))
                    {
                        if (x + blockLocation.x >= 0 && x + blockLocation.x < worldArray.GetLength(0) && y + blockLocation.y >= 0 && y + blockLocation.y < worldArray.GetLength(1))
                        {
                            directions[i] = worldArray[x + blockLocation.x, y + blockLocation.y] == (int)blockIDs.leaves;
                        }
                        i += 1;
                    }
                }
            }



            /*  0 | 1 | 2
             *  3 |   | 4
             *  5 | 6 | 7
             * */

            //Do all the checks for the different possiblilities:

            if (directions[1] && directions[3] && directions[4] && directions[6])
            {
                //Fully surrounded:
                sourceRectangle.X = 544;
            }
            //Threes
            else if (directions[3] && directions[4] && directions[6])
            {
                sourceRectangle.X = 416;
            }
            else if (directions[1] && directions[4] && directions[6])
            {
                sourceRectangle.X = 512;

            }
            else if (directions[1] && directions[3] && directions[6])
            {
                sourceRectangle.X = 448;

            }
            else if (directions[1] && directions[3] && directions[4])
            {
                sourceRectangle.X = 480;
            }
            //Twos
            else if (directions[4] && directions[6])
            {
                sourceRectangle.X = 384;

            }
            else if (directions[1] && directions[4])
            {
                sourceRectangle.X = 352;

            }
            else if (directions[3] && directions[6])
            {
                sourceRectangle.X = 288;

            }
            else if (directions[1] && directions[3])
            {
                sourceRectangle.X = 320;
            }
            //Ones

            else if (directions[1])
            {
                sourceRectangle.X = 160;

            }

            else if (directions[3])
            {
                sourceRectangle.X = 96;

            }
            else if (directions[4])
            {
                sourceRectangle.X = 224;

            }

            else if (directions[6])
            {
                sourceRectangle.X = 32;
            }

            else if (directions[0])
            {
                sourceRectangle.X = 128;
            }
            else if (directions[2])
            {
                sourceRectangle.X = 192;

            }
            else if (directions[5])
            {
                sourceRectangle.X = 64;
            }
            else if (directions[7])
            {
                sourceRectangle.X = 256;
            }

            base.setupInitialData(worldContext, worldArray, blockLocation);
        }

        public override void onBlockPlaced(WorldContext worldContext, (int x, int y) location)
        {
            //check all the surrounding blocks to see if they're leaves:
            bool[] directions = new bool[8];
            int i = 0;
            for (int y = -1; y <= +1; y++)
            {
                for (int x = -1; x <= +1; x++)
                {
                    if (!(x == 0 && y == 0))
                    {
                        if (x + location.x >= 0 && x + location.x < worldContext.worldArray.GetLength(0) && y + location.y >= 0 && y + location.y < worldContext.worldArray.GetLength(1))
                        {
                            System.Diagnostics.Debug.WriteLine(worldContext.worldArray[x + location.x, y + location.y].ID);
                            directions[i] = worldContext.worldArray[x + location.x, y + location.y].ID == (int)blockIDs.leaves;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("was at the edge");
                        }
                        i += 1;
                    }
                }
            }

            for (int y = 0; y < directions.Length; y++)
            {
                System.Diagnostics.Debug.WriteLine(y + " : " + directions[y]);
            }


            /*  0 | 1 | 2
             *  3 |   | 4
             *  5 | 6 | 7
             * */

            //Do all the checks for the different possiblilities:

            if (directions[1] && directions[3] && directions[4] && directions[6])
            {
                //Fully surrounded:
                sourceRectangle.X = 544;
            }
            //Threes
            else if (directions[3] && directions[4] && directions[6])
            {
                sourceRectangle.X = 416;
            }
            else if (directions[1] && directions[4] && directions[6])
            {
                sourceRectangle.X = 512;

            }
            else if (directions[1] && directions[3] && directions[6])
            {
                sourceRectangle.X = 448;

            }
            else if (directions[1] && directions[3] && directions[4])
            {
                sourceRectangle.X = 480;
            }
            //Twos
            else if (directions[4] && directions[6])
            {
                sourceRectangle.X = 384;

            }
            else if (directions[1] && directions[4])
            {
                sourceRectangle.X = 352;

            }
            else if (directions[3] && directions[6])
            {
                sourceRectangle.X = 288;

            }
            else if (directions[1] && directions[3])
            {
                sourceRectangle.X = 320;
            }
            //Ones

            else if (directions[1])
            {
                sourceRectangle.X = 160;

            }

            else if (directions[3])
            {
                sourceRectangle.X = 96;

            }
            else if (directions[4])
            {
                sourceRectangle.X = 224;

            }

            else if (directions[6])
            {
                sourceRectangle.X = 32;
            }

            else if (directions[0])
            {
                sourceRectangle.X = 128;
            }
            else if (directions[2])
            {
                sourceRectangle.X = 192;

            }
            else if (directions[5])
            {
                sourceRectangle.X = 64;
            }
            else if (directions[7])
            {
                sourceRectangle.X = 256;
            }
            base.onBlockPlaced(worldContext, location);
        }
        public override void onBlockDestroyed(Dictionary<(int x, int y), Block> exposedBlocks, WorldContext wc)
        {
            blockDestroyed(exposedBlocks);
        }

        public override Block copyBlock()
        {
            return new SemiLeafBlock(this);
        }
    }

    public class BushBlock : Block
    {
        public BushBlock(Rectangle textureSourceRectangle, int ID) : base(textureSourceRectangle, ID)
        {
            this.sourceRectangle = textureSourceRectangle;
            isBlockTransparent = true;
        }
        public BushBlock(Rectangle textureSourceRectangle, int emmissiveStrength, int ID) : base(textureSourceRectangle, emmissiveStrength, ID)
        {
            this.sourceRectangle = textureSourceRectangle;
            this.emmissiveStrength = emmissiveStrength;
            isBlockTransparent = true;

        }
        public BushBlock(int ID) : base(ID)
        {
            this.ID = ID;
            isBlockTransparent = true;

        }

        public BushBlock(Block b) : base(b)
        {
            sourceRectangle = b.sourceRectangle;
            emmissiveStrength = b.emmissiveStrength;
            ID = b.ID;
            hardness = 1;
            durability = 1;
            isBlockTransparent = true;
        }

        public override void onCollisionWithPhysicsObject(PhysicsObject entity, PhysicsEngine physicsEngine, WorldContext wc)
        {

        }

        public override void onBlockDestroyed(Dictionary<(int x, int y), Block> exposedBlocks, WorldContext wc)
        {
            blockDestroyed(exposedBlocks);
        }

        public override void setupInitialData(WorldContext worldContext, int[,] worldArray, (int x, int y) blockLocation)
        {
            Random r = new Random();
            sourceRectangle.X += 32 * r.Next(3);
            base.setupInitialData(worldContext, worldArray, blockLocation);
        }
        public override Block copyBlock()
        {
            return new BushBlock(this);
        }
    }

    public class BigBushBlock : Block
    {

        public int blockPairX;
        public int blockPairY;

        bool attemptedToDelete = false;
        public BigBushBlock(Rectangle textureSourceRectangle, int ID) : base(textureSourceRectangle, ID)
        {
            this.sourceRectangle = textureSourceRectangle;
            isBlockTransparent = true;

        }
        public BigBushBlock(Rectangle textureSourceRectangle, int emmissiveStrength, int ID) : base(textureSourceRectangle, emmissiveStrength, ID)
        {
            this.sourceRectangle = textureSourceRectangle;
            this.emmissiveStrength = emmissiveStrength;
            isBlockTransparent = true;

        }
        public BigBushBlock(int ID) : base(ID)
        {
            this.ID = ID;
            isBlockTransparent = true;

        }

        public BigBushBlock(Block b) : base(b)
        {
            sourceRectangle = b.sourceRectangle;
            emmissiveStrength = b.emmissiveStrength;
            ID = b.ID;
            hardness = 1;
            durability = 1;
            isBlockTransparent = true;
        }

        public override void onCollisionWithPhysicsObject(PhysicsObject entity, PhysicsEngine physicsEngine, WorldContext wc)
        {

        }

        public override void onBlockDestroyed(Dictionary<(int x, int y), Block> exposedBlocks, WorldContext wc)
        {
            if (attemptedToDelete == false)
            {
                attemptedToDelete = true;

                if (wc.worldArray[blockPairX, blockPairY].ID == ID)
                {
                    wc.deleteBlock(blockPairX, blockPairY);
                }

                blockDestroyed(exposedBlocks);
            }

        }



        public override void setupInitialData(WorldContext worldContext, int[,] worldArray, (int x, int y) blockLocation)
        {
            if (worldArray[blockLocation.x - 1, blockLocation.y] != ID)
            {
                worldArray[blockLocation.x + 1, blockLocation.y] = ID;
                if (worldArray[blockLocation.x + 1, blockLocation.y] == ID)
                {
                    this.blockPairX = blockLocation.x + 1;
                    this.blockPairY = blockLocation.y;
                }

            }
            else
            {

                sourceRectangle.X += 32;

                blockPairX = blockLocation.x - 1;
                blockPairY = blockLocation.y;

            }

            base.setupInitialData(worldContext, worldArray, blockLocation);

        }

        public override void updateBlock(WorldContext wc)
        {
        }


        public override Block copyBlock()
        {
            return new BigBushBlock(this);
        }

    }

    /*
     * ========================================
     * 
     * Interactive Blocks
     * 
     *  Blocks that contain special functionality when clicked upon
     *  
     *  Chest Block
     * ========================================
    */

    public class ChestBlock : InteractiveBlock, IInventory
    {
        public UIItem[,] inventory { get; set; }
        public UIElement inventoryBackground { get; set; }

        LootTable lootTable;

        public ChestBlock(Rectangle textureSourceRectangle, int ID) : base(textureSourceRectangle, ID)
        {
            maximumCooldown = 0.1f;
            isBlockTransparent = true;
        }
        public ChestBlock(Rectangle textureSourceRectangle, int emissiveStrength, int ID) : base(textureSourceRectangle, emissiveStrength, ID)
        {
            maximumCooldown = 0.1f;
            isBlockTransparent = true;
        }

        public ChestBlock(Block b) : base(b) { isBlockTransparent = true; }
        public ChestBlock(int ID) : base(ID) { isBlockTransparent = true; }
        public override void onBlockPlaced(WorldContext worldContext, (int x, int y) location)
        {
            base.onBlockPlaced(worldContext, location);

            initialiseChestData(worldContext);

        }

        public void initialiseChestData(WorldContext worldContext)
        {
            inventoryBackground = new InventoryBackground();
            inventoryBackground.drawRectangle.Y += 450;
            int inventoryWidth = 9;
            int inventoryHeight = 4;
            ((IInventory)this).initialiseInventory(worldContext, inventoryWidth, inventoryHeight);
            ((IInventory)this).showInventory();
            ((IInventory)this).hideInventory();
            maximumCooldown = 0.1f;
            isBlockTransparent = true;
        }

        public override void onBlockDestroyed(Dictionary<(int x, int y), Block> exposedBlocks, WorldContext wc)
        {
            Random r = new Random();
            //Destroy the inventory, and randomise the item drop locations a 'lil
            ((IInventory)this).destroyInventory(wc, (int)x * wc.pixelsPerBlock + r.Next(wc.pixelsPerBlock), (int)y * wc.pixelsPerBlock + r.Next(wc.pixelsPerBlock));
            base.onBlockDestroyed(exposedBlocks, wc);
        }
        public override void setupInitialData(WorldContext worldContext, int[,] worldArray, (int x, int y) blockLocation)
        {
            base.setupInitialData(worldContext, worldArray, blockLocation);
            initialiseChestData(worldContext);
            initialiseLootTables(worldContext);
            generateLootFromLootTable();
        }
        private void initialiseLootTables(WorldContext worldContext)
        {
            //A crappy way of determining what structure the chest is in. If it's the shrine then the block below is dirt. I'll adjust everything later.

            if (worldContext.backgroundArray[(int)x, (int)y - 1] == (int)backgroundBlockIDs.stone)
            {
                lootTable = new MountainChestLootTable();
            }
            else
            {
                lootTable = new WoodenChestLootTable();
            }
        }
        private void generateLootFromLootTable()
        {
            Random r = new Random();
            //Pick loot table to generate from:

            foreach (Item item in lootTable.generateLoot())
            {
                //Add it to a random, empty slot in the chests inventory
                bool foundASlot = false;
                int maxAttempts = 20;
                int attempts = 0;
                while (!foundASlot && maxAttempts > attempts)
                {
                    int slotX = r.Next(inventory.GetLength(0));
                    int slotY = r.Next(inventory.GetLength(1));
                    if (inventory[slotX, slotY].item == null)
                    {
                        inventory[slotX, slotY].setItem(item);
                        foundASlot = true;
                    }
                    attempts += 1;
                }
            }

        }
        public override void onCollisionWithPhysicsObject(PhysicsObject entity, PhysicsEngine physicsEngine, WorldContext wc)
        {
            //Don't collide
        }
        public override void onRightClick(WorldContext worldContext, GameTime gameTime)
        {
            if (gameTime.TotalGameTime.TotalSeconds - secondsSinceAction > maximumCooldown)
            {
                secondsSinceAction = gameTime.TotalGameTime.TotalSeconds;
                if (inventory[0, 0].isUIElementActive)
                {
                    ((IInventory)this).hideInventory();
                    worldContext.player.hideInventory();
                }
                else
                {
                    ((IInventory)this).showInventory();
                    worldContext.player.showInventory();
                    if (!worldContext.player.activeInventories.Contains(this))
                    {
                        worldContext.player.activeInventories.Add(this);
                    }
                }
            }
        }
        public override Block copyBlock()
        {
            return new ChestBlock(sourceRectangle, ID);
        }

    }

}
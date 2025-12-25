using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;


using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;
namespace PixelMidpointDisplacement
{
    public interface ICollider
    {
        bool isActive { get; set; }

        double x { get; set; }
        double y { get; set; }

        double invincibilityCooldown { get; set; }
        double maxInvincibilityCooldown { get; set; }

        public void onCollision(ICollider otherCollider)
        {
        }

        public void startInvincibilityFrames()
        {
            isActive = false;
            invincibilityCooldown = maxInvincibilityCooldown;
        }

        //It might be good to add a "updateInvincibilityFramesCount" function, but I think that would get annoying to call every inputUpdate within entities
    }

    public interface IActiveCollider : ICollider
    {

        Player owner { get; set; }

        Rectangle collider { get; set; }
        public virtual void calculateCollision(IPassiveCollider externalCollider)
        {
            if (new Rectangle((int)externalCollider.x + externalCollider.collider.X, (int)externalCollider.y + externalCollider.collider.Y, externalCollider.collider.Width, externalCollider.collider.Height).Intersects(new Rectangle(collider.X + (int)x, collider.Y + (int)y, collider.Width, collider.Height)))
            {
                //Collision happened!
                onCollision(externalCollider);
                externalCollider.onCollision(this);
            }
        }
    }

    public interface INonAxisAlignedActiveCollider : IActiveCollider
    {

        public Vector2[] rotatedPoints { get; set; }

        public Vector2[] originalPoints { get; set; }

        public Vector2 rotationOrigin { get; set; }

        public int colliderWidth { get; set; }
        public int colliderHeight { get; set; }



        public Animator itemAnimator { get; set; }

        public bool hasCollided { get; set; }



        public virtual void calculateCollision(IPassiveCollider externalCollider)
        {
            nonAxisAlignedCollisionDetection(externalCollider);
        }

        public void nonAxisAlignedCollisionDetection(IPassiveCollider externalCollider)
        {
            hasCollided = false;
            double theta = itemAnimator.currentPosition.rotation;


            Rectangle externalColliderInLocalSpace = new Rectangle((int)(externalCollider.x - x), (int)(externalCollider.y - y), externalCollider.collider.Width, externalCollider.collider.Height);

            //find the direction of the secondary object and determine which axis to project onto. I can definitely project solely onto an x-y plane given that I'm working with rectangular colliders for now.
            Vector2 seperatingAxis = calculateSeperationAxis(externalColliderInLocalSpace);
            //From the seperating axis, project the collider's shadow onto that axis, then see if there's a gap between the closest points... How shall I do that. Based on the axis, I can tell 
            //The center of the weapon is at 0,0 so that's to note. The seperating axis indicates what corner of the local and external colliders to use. I can take the weapons rectangle, then use a matrix transformation to rotate them, then find the point that has the greatest value along the seperating axis, which is also what it would be like projected, so i can ignore having to do vector dot products and merely take the appropriate component out of the transformed vectors.
            calculateRotation((float)itemAnimator.currentPosition.rotation);

            //Multiply the vectors by the seperating axis to get the proj onto that axis, I can then take the largest (or most negative) one and use that for the shadow. But how do I determine if the two shadows are overlapping? I can get the shadow length,
            //I can calculate the distance (based on the externalColliderInLocalSpace) it's shadow is literally just half the dimension in whatever axis, and the distance is the position of the collider in local space.

            double shadow = 0;
            for (int i = 0; i < rotatedPoints.Length; i++)
            {

                if (seperatingAxis.X != 0)
                {
                    if (seperatingAxis.X > 0 ? shadow < rotatedPoints[i].X : shadow > rotatedPoints[i].X) { shadow = rotatedPoints[i].X; }

                }
                else if (seperatingAxis.Y != 0)
                {
                    if (seperatingAxis.Y > 0 ? shadow < seperatingAxis.Y : shadow > seperatingAxis.Y) { shadow = rotatedPoints[i].Y; }
                }
            }




            if (seperatingAxis.X != 0)
            {
                if (externalColliderInLocalSpace.X * seperatingAxis.X < Math.Abs(shadow) + externalColliderInLocalSpace.Width / 2)
                {
                    //Has collided
                    hasCollided = true;
                    onCollision(externalCollider);
                    externalCollider.onCollision(this);
                }
            }
            else if (seperatingAxis.Y != 0)
            {
                if (externalColliderInLocalSpace.Y * seperatingAxis.Y < Math.Abs(shadow) + externalColliderInLocalSpace.Height / 2)
                {
                    //Has collided
                    hasCollided = true;
                    onCollision(externalCollider);
                    externalCollider.onCollision(this);
                }
            }


        }

        public Vector2 calculateSeperationAxis(Rectangle externalColliderInLocalSpace)
        {
            Vector2 seperatingAxis;
            if (externalColliderInLocalSpace.X > 0) //Is to the right
            {
                if (externalColliderInLocalSpace.Y > 0) //Is downwards
                {
                    //Determine which axis is overlapping more, then use that to determine the appropriate seperatingAxis. Ensure that it's the one with no/little overlap. EG using the x-axis for things on top of eachother will produce false positives
                    if (externalColliderInLocalSpace.Y - externalColliderInLocalSpace.X > 0)
                    { //Is more below than to the right. So use the y axis to determine collision.
                        seperatingAxis = new Vector2(0, 1);
                    }
                    else
                    {
                        seperatingAxis = new Vector2(1, 0);
                    }
                }
                else //Is upwards
                {
                    if (externalColliderInLocalSpace.X + externalColliderInLocalSpace.Y > 0) //More right than up
                    {
                        seperatingAxis = new Vector2(1, 0);
                    }
                    else
                    {
                        seperatingAxis = new Vector2(0, -1);
                    }

                }
            }
            else  //Is to the left
            {
                if (externalColliderInLocalSpace.Y > 0) //Is downwards
                {
                    //Determine which axis is overlapping more, then use that to determine the appropriate seperatingAxis. Ensure that it's the one with no/little overlap. EG using the x-axis for things on top of eachother will produce false positives
                    if (externalColliderInLocalSpace.Y + externalColliderInLocalSpace.X > 0)
                    { //Is more below than to the left. So use the y axis to determine collision.
                        seperatingAxis = new Vector2(0, 1);
                    }
                    else
                    {
                        seperatingAxis = new Vector2(-1, 0);
                    }
                }
                else //Is upwards
                {
                    if (externalColliderInLocalSpace.X - externalColliderInLocalSpace.Y < 0) //More left than up
                    {
                        seperatingAxis = new Vector2(-1, 0);
                    }
                    else
                    {
                        seperatingAxis = new Vector2(0, -1);
                    }

                }

            }

            return seperatingAxis;
        }

        public void calculateRotation(float rotation)
        {
            rotation *= owner.playerDirection;
            rotatedPoints[0] = Vector2.RotateAround(originalPoints[0], new Vector2(0, 0), rotation);
            rotatedPoints[1] = Vector2.RotateAround(originalPoints[1], new Vector2(0, 0), rotation);
            rotatedPoints[2] = Vector2.RotateAround(originalPoints[2], new Vector2(0, 0), rotation);
            rotatedPoints[3] = Vector2.RotateAround(originalPoints[3], new Vector2(0, 0), rotation);
        }


    }

    public interface IPassiveCollider : ICollider
    {

        Rectangle collider { get; set; }
        //External colliders are colliders that don't compute their own collisions, they only react to collisions. Lets say that monsters have IExternalColliders, when the player collides with the monster, the collision function is run, but the monster doesn't also compute if it collided.
        //I think this will just make it a bit easier to seperate player based colliders from entity colliders. Weapons, including arrows, will have actual colliders that compute collisions with external colliders. This way weapons can 
    }

    public interface IEmissive
    {
        public double x { get; set; }
        public double y { get; set; }
        public Vector3 lightColor { get; set; }
        public float luminosity { get; set; }

        public float range { get; set; }


        public RenderTarget2D shadowMap { get; set; }
        public RenderTarget2D lightMap { get; set; }
    }

    public interface IEmissiveBlock
    {
        public double x { get; set; }
        public double y { get; set; }

        public Vector3 lightColor { get; set; }
        public float luminosity { get; set; }

        public float range { get; set; }


        public RenderTarget2D shadowMap { get; set; }
        public RenderTarget2D lightMap { get; set; }

    }

    public interface IInventory
    {
        public UIItem[,] inventory { get; set; }
        public UIElement inventoryBackground { get; set; }

        public void initialiseInventory(WorldContext worldContext, int inventoryWidth, int inventoryHeight)
        {
            inventory = new UIItem[inventoryWidth, inventoryHeight];
            worldContext.engineController.UIController.addUIElement(4, inventoryBackground);
            worldContext.engineController.UIController.inventoryBackgrounds.Add(inventoryBackground);
            for (int x = 0; x < inventory.GetLength(0); x++)
            {
                for (int y = 0; y < inventory.GetLength(1); y++)
                {
                    inventory[x, y] = new UIItem(x, y, inventoryBackground.drawRectangle.X, inventoryBackground.drawRectangle.Y, worldContext, this);
                    worldContext.engineController.UIController.addUIElement(5, inventory[x, y]);
                }
            }
        }

        public void destroyInventory(WorldContext worldContext, int xLoc, int yLoc)
        {
            for (int x = 0; x < inventory.GetLength(0); x++)
            {
                for (int y = 0; y < inventory.GetLength(1); y++)
                {
                    (int, UIElement) UIListElement = worldContext.engineController.UIController.UIElements.Find(i => i.uiElement == inventory[x, y]);
                    worldContext.engineController.UIController.removeUIElement(UIListElement.Item1, UIListElement.Item2);
                    Random r = new Random();
                    if (inventory[x, y].item != null)
                    {

                        DroppedItem dropItem = new DroppedItem(worldContext, inventory[x, y].item, (xLoc, yLoc), new Vector2((float)r.NextDouble(), (float)r.NextDouble()));
                        dropItem.pickupDelay = 0f;
                        worldContext.engineController.entityController.addEntity(dropItem);
                    }
                }
            }
            (int, UIElement) InventoryBackgroundElement = worldContext.engineController.UIController.UIElements.Find(i => i.uiElement == inventoryBackground);
            worldContext.engineController.UIController.removeUIElement(InventoryBackgroundElement.Item1, InventoryBackgroundElement.Item2);
        }

        public bool addItemToInventory(Item item)
        {
            bool foundASlot = false;
            //Check for any stacks to add the item to
            for (int y = 0; y < inventory.GetLength(1); y++)
            {
                for (int x = 0; x < inventory.GetLength(0); x++)
                {
                    if (!foundASlot && inventory[x, y].item != null && item.currentStackSize > 0)
                    {

                        if (inventory[x, y].item.isItemIdentical(item))
                        {
                            //Class specific checks:
                            foundASlot = combineItemStacks(item, x, y);
                        }
                    }
                }
            }
            if (!foundASlot)
            {
                for (int y = 0; y < inventory.GetLength(1); y++)
                {
                    for (int x = 0; x < inventory.GetLength(0); x++)
                    {
                        if (!foundASlot)
                        {
                            if (inventory[x, y].item == null)
                            {
                                inventory[x, y].setItem(item);
                                foundASlot = true;
                            }
                        }
                    }
                }
            }
            return foundASlot;
        }
        public bool combineItemStacks(Item item, int x, int y)
        {
            bool foundASlot = false;
            bool isTheRightItem = item.isItemIdentical(inventory[x, y].item);

            if (isTheRightItem)
            {
                int amountUntilMaxStack = inventory[x, y].item.maxStackSize - inventory[x, y].item.currentStackSize;
                if (amountUntilMaxStack > 0 && item.currentStackSize > 0)
                {
                    int stackSizeToAdd = item.currentStackSize;
                    if (stackSizeToAdd > amountUntilMaxStack) { stackSizeToAdd = amountUntilMaxStack; }
                    inventory[x, y].item.currentStackSize += stackSizeToAdd;

                    item.currentStackSize -= stackSizeToAdd;
                    if (item.currentStackSize <= 0)
                    {
                        foundASlot = true;
                    }
                }

            }
            return foundASlot;
        }
        public void showInventory()
        {
            if (!inventory[0, 1].isUIElementActive)
            {
                for (int x = 0; x < inventory.GetLength(0); x++)
                {
                    for (int y = 0; y < inventory.GetLength(1); y++)
                    {
                        inventory[x, y].isUIElementActive = true;
                    }
                }

                inventoryBackground.isUIElementActive = true;
            }

        }
        public void hideInventory()
        {
            if (inventory[0, 1].isUIElementActive)
            {
                for (int x = 0; x < inventory.GetLength(0); x++)
                {
                    //Only hide the second row of the inventory, keep the hotbar
                    for (int y = 0; y < inventory.GetLength(1); y++)
                    {
                        inventory[x, y].isUIElementActive = false;
                    }
                }
                inventoryBackground.isUIElementActive = false;
            }
        }

        public (Item, int, int) findItemInInventory(Item item)
        {
            Item foundItem = null;
            int indexX = 0;
            int indexY = 0;

            if (item != null)
            {
                for (int y = 0; y < inventory.GetLength(1); y++)
                {
                    for (int x = 0; x < inventory.GetLength(0); x++)
                    {
                        if (inventory[x, y].item != null)
                        {
                            if (inventory[x, y].item.isItemIdentical(item))
                            {
                                foundItem = inventory[x, y].item;
                                indexX = x;
                                indexY = y;
                            }
                        }
                    }
                }
            }


            return (foundItem, indexX, indexY);
        }
    }

    public interface IGroundTraversalAlgorithm
    {
        public double x { get; set; }
        public double y { get; set; }

        public double targetX { get; set; }
        public double targetY { get; set; }

        public double percievedX { get; set; }
        public double percievedY { get; set; }

        public double perceptionDistance { get; set; }

        public double xDifferenceThreshold { get; set; }
        public WorldContext worldContext { get; set; }

        public double height { get; set; }

        public double notJumpThreshold { get; set; }
        public double jumpWhenWithinXRange { get; set; }

        public (int horizontal, int vertical) traverseTerrain()
        {

            //Update perception: 
            if (percievedX == 0 && percievedY == 0)
            {
                //Setup initial perception if the player isn't within the radius
                percievedX = x;
                percievedY = y;
            }
            if (Math.Pow(Math.Pow(targetX - x, 2) + Math.Pow(targetY - y, 2), 0.5) < perceptionDistance)
            {
                percievedX = targetX;
                percievedY = targetY;
            }


            int leftRight = 0;
            int upDown = 0;
            if (Math.Abs(x - percievedX) > xDifferenceThreshold)
            {
                if (x > percievedX)
                {
                    leftRight = 2;
                }
                else if (x < percievedX)
                {
                    leftRight = 1;
                }

                //Jump on thre conditions: 
                //Theres a hole in front of them, try to jump over it given that the player is above or approximately on the same level as them 
                //The entity is close enough to the player but isn't colliding, so jump instead
                //There's a wall in front of the entity

                int playerBlockX = (int)Math.Floor(x / worldContext.pixelsPerBlock);
                int playerBlockY = (int)Math.Floor(y / worldContext.pixelsPerBlock);

                //If there's a hole in front of them:
                if (y - percievedY > notJumpThreshold)
                {
                    if (leftRight == 2)
                    {
                        //Moving left

                        if (playerBlockX > 0 && playerBlockX < worldContext.worldArray.GetLength(0) && playerBlockY > 0 && playerBlockY + (int)Math.Round(height) < worldContext.worldArray.GetLength(1))
                        {
                            //If the block to the left and at the floor level is either transparent or air, jump

                            if (worldContext.worldArray[playerBlockX - 1, playerBlockY + (int)Math.Round(height)].isBlockTransparent || worldContext.worldArray[playerBlockX - 1, playerBlockY + (int)Math.Round(height)].ID == 0)
                            {
                                upDown = 1;
                            }
                        }
                    }
                    else if (leftRight == 1)
                    {
                        //Moving right

                        if (playerBlockX >= 0 && playerBlockX + 1 < worldContext.worldArray.GetLength(0) && playerBlockY > 0 && playerBlockY + (int)height < worldContext.worldArray.GetLength(1))
                        {
                            if (worldContext.worldArray[playerBlockX + 1, playerBlockY + (int)Math.Round(height)].isBlockTransparent || worldContext.worldArray[playerBlockX + 1, playerBlockY + (int)Math.Round(height)].ID == 0)
                            {
                                upDown = 1;
                            }

                        }
                    }
                }

                //There's a wall in front of them and they should jump over: 
                if (leftRight == 2)
                {
                    //Moving left
                    for (int y = 0; y < Math.Ceiling(height); y++)
                    {
                        //Check every block that could collide with the entity
                        if (playerBlockX > 0 && playerBlockX < worldContext.worldArray.GetLength(0) && playerBlockY > 0 && playerBlockY + (int)y < worldContext.worldArray.GetLength(1))
                        {
                            //If the block is solid, then jump
                            if (!worldContext.worldArray[playerBlockX - 1, playerBlockY + (int)y].isBlockTransparent && worldContext.worldArray[playerBlockX - 1, playerBlockY + (int)y].ID != 0)
                            {
                                upDown = 1;
                            }
                        }
                    }

                }
                else if (leftRight == 1)
                {
                    //Moving right
                    for (int y = 0; y < Math.Ceiling(height); y++)
                    {
                        if (playerBlockX >= 0 && playerBlockX + 1 < worldContext.worldArray.GetLength(0) && playerBlockY > 0 && playerBlockY + (int)y < worldContext.worldArray.GetLength(1))
                        {
                            if (!worldContext.worldArray[playerBlockX + 1, playerBlockY + (int)y].isBlockTransparent && worldContext.worldArray[playerBlockX + 1, playerBlockY + (int)y].ID != 0)
                            {
                                upDown = 1;
                            }

                        }
                    }
                }
            }

            return (leftRight, upDown);
        }
    }

    public interface IEntityActionListener
    {
        //An interface that allows a class to listen and respond to events within an entity
        public double onDamage(Object source, DamageType damageType, double damage)
        {
            return damage;
        }

        public void onInput(double elapsedTime) { }
        public void onEntityCollision() { }
        public void onBlockCollision() { }

    }
}
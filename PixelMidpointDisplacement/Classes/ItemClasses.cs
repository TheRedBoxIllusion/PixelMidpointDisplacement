using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

using System.Collections.Generic;

using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Vector4 = Microsoft.Xna.Framework.Vector4;

namespace PixelMidpointDisplacement {

    /*
     * ========================================
     *  Parent Classes:
     *  
     *  Item is just a base item that has functionality when equipped, left clicked or picked up
     *  Equipable Item extends Item, but contains some additional functionality, such as being able to tie into player events (such as taking damage) through inheritable functions
     *  
     * ========================================
     */
    public class Item : DrawnClass
    {
        //Two ways of going about dropping and picking items up. One: Make each item a physics object, and activate the physics when they are dropped.Seems needlessly bulky
        //Two: Have a "dropped item" class which is itself an entity, and make it point to an Item Class. On the "input" update function of entities, if the player is within
        //A set range, apply a force towards the player. Items float towards the player, and when they get within a smaller range, they add the item to the players inventory
        //and destroy the "dropped item" class

        public string id { get; set; }

        public string jsonSourceFilePath {get;set;}

        public int maxStackSize { get; set; }

        public int currentStackSize { get; set; }

        public string tooltip;
        public StringRenderer tooltipRenderer;

        public Animator itemAnimator { get; set; }
        public AnimationController animationController { get; set; }
        public Vector2 origin { get; set; }
        public int verticalDirection { get; set; }
        public double constantRotationOffset { get; set; }

        public int colliderWidth { get; set; }
        public int colliderHeight { get; set; }

        public Vector2 offsetFromEntity { get; set; }

        public float useCooldown;

        public Player owner { get; set; }

        public Item()
        {

            useCooldown = 0f;
            verticalDirection = 1;

            currentStackSize = 1;

            drawEffect = SpriteEffects.None;

            tooltipRenderer = new StringRenderer(Scene.Game, UIAlignOffset.TopLeft, 11, true);

        }

        
        public virtual void onLeftClick() { }
        public virtual void onEquip() { }
        public virtual void onUnequip() { }
        public virtual void animationFinished()
        {
            itemAnimator = null;
        }

        public virtual void onPickup(Entity owner) {
            if(owner is Player p)
            {
                this.owner = p;
                animationController = p.worldContext.animationController;
                tooltipRenderer.setWorldContext(owner.worldContext);
            }
        }

        public virtual bool isItemIdentical(Item otherItem)
        {
            return otherItem.GetType() == this.GetType();
        }

        public virtual Item itemCopy(int stackSize)
        {
            Item i = new Item();
            i.owner = owner;
            i.currentStackSize = stackSize;
            return i;
        }

    }
    public class EquipableItem : Item
    {
        public EquipableItem()
        {
            verticalDirection = 1;
            constantRotationOffset = 0;
            origin = new Vector2(-2f, 18f);

            maxStackSize = 1;
            currentStackSize = 1;

            useCooldown = 0f;
        }


        public virtual void onEquipToSlot() { }
        public virtual void onUnequipFromSlot() { }

        //Just have a crap ton of functions that get called in different spots

        public virtual void onInput(double elapsedTime) { }

        public virtual double onDamageTaken(DamageType damageType, double damage, object source) { return damage; }
    }

    /*
     * ========================================
     * 
     * Specific equipable item parent classes:
     * 
     * Accessory
     * Equipment
     * 
     * ========================================
     */
    public class Accessory : EquipableItem
    {
        public Accessory() { }

        public override void onLeftClick()
        {
            for (int y = 0; y < owner.equipmentInventory.GetLength(1); y++)
            {
                if (owner.equipmentInventory[0, y].item == null)
                {
                    owner.equipmentInventory[0, y].setItem(this);
                    onEquipToSlot();
                    owner.inventory[owner.mainHandIndex, 0].setItem(null);
                    owner.mainHand = null;
                    break;
                }

            }
        }


    }
    public class Equipment : EquipableItem
    {

        //I probably could exchange the armourtype enum for different subclasses...
        public ArmorType equipmentType;
        public Equipment()
        {

        }

        public override void onLeftClick()
        {
            Equipment previouslyEquipped = (Equipment)owner.equipmentInventory[1, (int)equipmentType].item;
            owner.equipmentInventory[1, (int)equipmentType].setItem(this);
            owner.inventory[owner.mainHandIndex, 0].setItem(previouslyEquipped);
            owner.mainHand = previouslyEquipped;
            if (previouslyEquipped != null)
            {
                previouslyEquipped.onUnequipFromSlot();
            }
            onEquipToSlot();
        }


        public override Item itemCopy(int stackSize)
        {
            Equipment e = new Equipment();
            e.owner = owner;
            e.currentStackSize = stackSize;
            return e;
        }
    }

    /*
     * ========================================
     * 
     * Tool parent classes:
     * 
     *  Items that have a fundamental aspect / function within the game. 
     *    
     *  Weapon
     *  Pickaxe
     * 
     * ========================================
     */
    public class Weapon : Item, INonAxisAlignedActiveCollider
    {
        //I need to make the x and y position update constantly when the item is swung, not just on left click

        bool swungDownwardsLastIteration = false;

        public Vector2[] rotatedPoints { get; set; }

        public Vector2[] originalPoints { get; set; }

        public Vector2 rotationOrigin { get; set; }

        //A null field as the collider is non-axis aligned
        public Rectangle collider { get; set; }

        public double weaponDamage = 20;

        public double x { get; set; }
        public double y { get; set; }

        public double invincibilityCooldown { get; set; }
        public double maxInvincibilityCooldown { get; set; }

        public bool isActive { get; set; }

        public bool hasCollided { get; set; }

        public Weapon()
        {
            spriteSheetID = (int)spriteSheetIDs.weapons;

            constantRotationOffset = -Math.PI / 4;


            origin = new Vector2(-2f, 18f);
            rotationOrigin = new Vector2(-2, 18f);

            sourceRectangle = new Rectangle(0, 0, 16, 16);
            drawRectangle = new Rectangle(0,0, 48, 48);



            rotatedPoints = new Vector2[4];
            originalPoints = new Vector2[4];


            colliderWidth = 8;
            colliderHeight = 48;

            useCooldown = 0.2f;

            maxStackSize = 1;

            tooltip =
                "<h2>Iron Sword</h2>\n\n" +
                "Damage: <gold>" + weaponDamage + "</gold>\n" +
                "<grey>A weapon designed in the olde' days \n" +
                "made for one purpose: Death </grey>";

            tooltipRenderer.stringToRender = tooltip;
        }
        //Adjusted to define the rectangular vertices only once, this should be a bit more efficient
        public override void onLeftClick()
        {
            if (itemAnimator == null)
            {
                offsetFromEntity = new Vector2(owner.playerDirection * 8, 48);
                isActive = true;
                if (!swungDownwardsLastIteration)
                {
                    drawEffect = SpriteEffects.None;
                    verticalDirection = 1;
                    origin = new Vector2(-2f, 18f);
                    x = (owner.x - origin.X);
                    y = (owner.y - origin.Y); 
                    constantRotationOffset = -Math.PI / 4;

                    float initialRotation = (float)0;
                    itemAnimator = new Animator(animationController, this, 0.2, (0, 0, initialRotation), (0, 0, 2 * Math.PI / 3), constantRotationOffset, offsetFromEntity);
                    swungDownwardsLastIteration = true;

                    initialiseColliderVectors(1, initialRotation);
                }
                else
                {

                    drawEffect = SpriteEffects.FlipVertically;

                    verticalDirection = -1;
                    x = (owner.x - origin.X);
                    y = (owner.y - origin.Y);
                    constantRotationOffset = Math.PI / 4;

                    float initialRotation = (float)-Math.PI / 6;
                    itemAnimator = new Animator(animationController, this, 0.15, (0, 0, initialRotation), (0, 0, -Math.PI / 2), constantRotationOffset, offsetFromEntity);
                    swungDownwardsLastIteration = false;
                    initialiseColliderVectors(-1, initialRotation);

                }
                animationController.addAnimator(itemAnimator);
            }
        }
        public override void onEquip()
        {
            owner.worldContext.engineController.collisionController.addActiveCollider((INonAxisAlignedActiveCollider)this);
        }
        public override void animationFinished()
        {
            isActive = false;
            itemAnimator = null;
        }

        private void initialiseColliderVectors(int multiplier, float initialRotation)
        {
            originalPoints[0] = new Vector2(-colliderWidth, -colliderHeight) - rotationOrigin; //The rotation origin doesn't adjust with the origin that is used for drawing. This is because the drawing system and the collision system operate under different grid spacesgit
            originalPoints[1] = new Vector2(0, -colliderHeight) - rotationOrigin;
            originalPoints[2] = new Vector2(-colliderWidth, 0) - rotationOrigin;
            originalPoints[3] = new Vector2(0, 0) - rotationOrigin;

            originalPoints[0] *= multiplier;
            originalPoints[1] *= multiplier;
            originalPoints[2] *= multiplier;
            originalPoints[3] *= multiplier;


            //Initialise the rotated points a
            rotatedPoints[0] = new Vector2(originalPoints[0].X, originalPoints[0].Y);
            rotatedPoints[1] = new Vector2(originalPoints[1].X, originalPoints[1].Y);
            rotatedPoints[2] = new Vector2(originalPoints[2].X, originalPoints[2].Y);
            rotatedPoints[3] = new Vector2(originalPoints[3].X, originalPoints[3].Y);

            ((INonAxisAlignedActiveCollider)this).calculateRotation(initialRotation);
        }

        public void onCollision(ICollider externalCollider)
        {
            if (externalCollider is Entity e)
            {
                e.velocityX = 7 * owner.playerDirection;
                e.velocityY += 7;
                //Have to move the player up, because of the slight overlap with the lower block, it causes a collision to detect and counteract the velocity?
                e.y -= 12;
                e.applyDamage(owner, DamageType.EntityAttack, weaponDamage * owner.entityDamageMultiplier);
                e.knockbackStunDuration = 0.5f;
                ((ICollider)e).startInvincibilityFrames();
            }
        }

        public override Item itemCopy(int stackSize)
        {
            Weapon i = new Weapon();
            i.animationController = animationController;
            i.owner = owner;
            if (stackSize < maxStackSize)
            {
                i.currentStackSize = stackSize;
            }
            else { i.currentStackSize = maxStackSize; }
            return i;
        }
    }

    public class Pickaxe : Item
    {
        int digSize = 1;

        public double pickaxeStrength = 15;
        public double durabilityPerHit = 1;
        public Pickaxe()
        {
            spriteSheetID = (int)spriteSheetIDs.weapons;

            constantRotationOffset = -MathHelper.PiOver4;
            origin = new Vector2(-2f, 18f);


            sourceRectangle = new Rectangle(32, 0, 16, 16);
            drawRectangle = new Rectangle (0,0,40, 40);


            maxStackSize = 1;

            tooltip = "<gold><h2>Pickaxe</gold></h2> \n \n" +
                "Pickaxe Strength: <gold>" + pickaxeStrength + "%</gold>\n" +
                "Used to mine the world \n \n" +
                "<grey>This pickaxe holds the secrets \n to many deep caverns</grey>";

            tooltipRenderer.stringToRender = tooltip;
        }

        public override void onLeftClick()
        {
            if (itemAnimator == null)
            {
                itemAnimator = new Animator(animationController, this, 0.15, (0, 0, 0), (0, 0, 2 * Math.PI / 3), constantRotationOffset, new Vector2(owner.playerDirection * 8f, 25f));
                animationController.addAnimator(itemAnimator);
                if (Mouse.GetState().ScrollWheelValue / 120 != digSize - 1)
                {
                    digSize = Mouse.GetState().ScrollWheelValue / 120 + 1;
                }
                double mouseXPixelSpace = Mouse.GetState().X - owner.worldContext.screenSpaceOffset.x;
                double mouseYPixelSpace = Mouse.GetState().Y - owner.worldContext.screenSpaceOffset.y;

                int mouseXGridSpace = (int)Math.Floor(mouseXPixelSpace / owner.worldContext.pixelsPerBlock);
                int mouseYGridSpace = (int)Math.Floor(mouseYPixelSpace / owner.worldContext.pixelsPerBlock);

                //Delete Block at that location
                for (int x = 0; x < digSize; x++)
                {
                    for (int y = 0; y < digSize; y++)
                    {
                        int usedX = x - (int)Math.Floor(digSize / 2.0);
                        int usedY = y - (int)Math.Floor(digSize / 2.0);
                        if (mouseXGridSpace + usedX > 0 && mouseXGridSpace + usedX < owner.worldContext.worldArray.GetLength(0) && mouseYGridSpace + usedY > 0 && mouseYGridSpace + usedY < owner.worldContext.worldArray.GetLength(1))
                        {
                            owner.worldContext.damageBlock(pickaxeStrength, durabilityPerHit, mouseXGridSpace + usedX, mouseYGridSpace + usedY);
                            if (owner.worldContext.worldArray[mouseXGridSpace + usedX, mouseYGridSpace + usedY].ID != 0 && owner.worldContext.worldArray[mouseXGridSpace + usedX, mouseYGridSpace + usedY].durability <= 0)
                            {
                                onBlockDeleted(owner.worldContext.worldArray[mouseXGridSpace + usedX, mouseYGridSpace + usedY], mouseXGridSpace + usedX, mouseYGridSpace + usedY);
                                owner.worldContext.deleteBlock(mouseXGridSpace + usedY, mouseYGridSpace + usedY);
                            }
                        }
                    }
                }
            }
        }

        public virtual void onBlockDeleted(Block b, int x, int y)
        {
        }
        public override Item itemCopy(int stackSize)
        {
            Pickaxe p = new Pickaxe();
            p.animationController = animationController;
            p.owner = owner;
            return p;
        }
    }

    /*
     * ========================================
     * 
     * Class Item types:
     * 
     *  Items that represent an inventory version of a resource.
     *  
     *  BlockItem
     *  OreItem
     *  IngotItem
     *  BackgroundBlockItem
     * 
     * ========================================
    */
    public class BlockItem : Item
    {
        public int blockID;


        int semiAnimationAdditions = 0;
        int maxSemiAdditions = 3;


        public BlockItem(int BlockID)
        {
            blockID = BlockID;

            spriteSheetID = (int)spriteSheetIDs.blockItems;

            sourceRectangle = new Rectangle(0, (blockID - 1) * 8, 8, 8);

            drawRectangle = new Rectangle(0,0,16, 16); //J

            origin = new Vector2(-2, 18f); //J
            constantRotationOffset = 0; //J

            maxStackSize = 999; //J

            tooltip =
                "<h2>" + (blockIDs)BlockID + " Block</h2>\n\n" +

                "<grey>The humble objects you \n" +
                "dedicate your life to destroying </grey>";

            tooltipRenderer.stringToRender = tooltip;
            //When you pick up an item, you check the inventory for an item of the same type. If that item already exists, check the stack size. If the stack size
            //is less than the max, just add to the current stacksize, otherwise add the item to the inventory in the next empty slot
        }

        public override void onLeftClick()
        {
            if (itemAnimator == null)
            {
                semiAnimationAdditions = 0;
                offsetFromEntity = new Vector2(owner.playerDirection * 8, 16);
                itemAnimator = new Animator(animationController, this, 0.15, (0, 0, 0), (0, 0, 2 * Math.PI / 3), constantRotationOffset, offsetFromEntity);

                animationController.addAnimator(itemAnimator);

                int mouseX = (int)Math.Floor((double)(Mouse.GetState().X - owner.worldContext.screenSpaceOffset.x) / owner.worldContext.pixelsPerBlock);
                int mouseY = (int)Math.Floor((double)(Mouse.GetState().Y - owner.worldContext.screenSpaceOffset.y) / owner.worldContext.pixelsPerBlock);

                if (owner.worldContext.addBlock(mouseX, mouseY, blockID))
                {
                    currentStackSize -= 1;
                }
            }
            if (semiAnimationAdditions < maxSemiAdditions)
            {
                int mouseX = (int)Math.Floor((double)(Mouse.GetState().X - owner.worldContext.screenSpaceOffset.x) / owner.worldContext.pixelsPerBlock);
                int mouseY = (int)Math.Floor((double)(Mouse.GetState().Y - owner.worldContext.screenSpaceOffset.y) / owner.worldContext.pixelsPerBlock);

                if (owner.worldContext.addBlock(mouseX, mouseY, blockID))
                {
                    semiAnimationAdditions += 1;
                    currentStackSize -= 1;
                }
            }
        }

        public override bool isItemIdentical(Item otherItem)
        {
            if (otherItem is BlockItem b)
            {
                return b.blockID == blockID;
            }
            else
            {
                return false;
            }
        }
        public override Item itemCopy(int stackSize)
        {
            BlockItem i = new BlockItem(blockID);
            i.animationController = animationController;
            i.owner = owner;
            i.currentStackSize = stackSize;
            return i;
        }
    }
    public class OreItem : Item
    {
        public int oreID;
        const int textureHeight = 16;
        public OreItem(int oreID)
        {
            spriteSheetID = (int)spriteSheetIDs.oreSpriteSheet;
            this.oreID = oreID;
            sourceRectangle = new Rectangle(0, textureHeight * oreID, textureHeight, textureHeight);

            spriteSheetID = (int)spriteSheetIDs.oreSpriteSheet;


            drawRectangle = new Rectangle(0,0,24, 24);

            origin = new Vector2(-2, 18f);


            maxStackSize = 999;

            tooltip =
                "<h2>" + (oreIDs)(oreID) + " ore </h2>\n\n" +

                "<grey>A precious metal found within\n" +
                "the Earth.</grey>";

            tooltipRenderer.stringToRender = tooltip;
        }

        public override void onLeftClick()
        {
            offsetFromEntity = new Vector2(owner.playerDirection * 8, 16);
            itemAnimator = new Animator(animationController, this, 0.15, (0, 0, 0), (0, 0, 2 * Math.PI / 3), constantRotationOffset, offsetFromEntity);

            animationController.addAnimator(itemAnimator);
        }

        public override bool isItemIdentical(Item otherItem)
        {
            if (otherItem is OreItem ore)
            {
                return ore.oreID == oreID;
            }
            else
            {
                return false;
            }
        }

        public override Item itemCopy(int stackSize)
        {
            OreItem ore = new OreItem(oreID);
            ore.owner = owner;
            ore.animationController = animationController;
            ore.currentStackSize = stackSize;
            return ore;
        }
    }
    public class IngotItem : Item
    {
        public int ingotID;
        const int textureHeight = 16;
        public IngotItem(int ingotID)
        {
            spriteSheetID = (int)spriteSheetIDs.oreSpriteSheet;
            this.ingotID = ingotID;
            sourceRectangle = new Rectangle(0, textureHeight * ingotID, textureHeight, textureHeight);

            spriteSheetID = (int)spriteSheetIDs.ingotSpriteSheet;


            drawRectangle = new Rectangle(0,0,24, 24);

            origin = new Vector2(-2, 18f);


            maxStackSize = 999;

            tooltip =
                "<h2>" + (oreIDs)(ingotID) + " ingot </h2>\n\n" +

                "<grey>A now refined precious metal \n" +
                "found within the Earth.</grey>";

            tooltipRenderer.stringToRender = tooltip;
        }

        public override void onLeftClick()
        {
            offsetFromEntity = new Vector2(owner.playerDirection * 8, 16);
            itemAnimator = new Animator(animationController, this, 0.15, (0, 0, 0), (0, 0, 2 * Math.PI / 3), constantRotationOffset, offsetFromEntity);

            animationController.addAnimator(itemAnimator);
        }

        public override bool isItemIdentical(Item otherItem)
        {
            if (otherItem is IngotItem ore)
            {
                return ore.ingotID == ingotID;
            }
            else
            {
                return false;
            }
        }

        public override Item itemCopy(int stackSize)
        {
            IngotItem ore = new IngotItem(ingotID);
            ore.animationController = animationController;
            ore.owner = owner;
            ore.currentStackSize = stackSize;
            return ore;
        }
    }
    public class BackgroundBlockItem : Item
    {
        public int blockID;


        int semiAnimationAdditions = 0;
        int maxSemiAdditions = 3;


        public BackgroundBlockItem(int BlockID)
        {
            blockID = BlockID;
            spriteSheetID = (int)spriteSheetIDs.blockBackground;

            sourceRectangle = new Rectangle(0, (blockID) * 32, 32, 32);

            drawRectangle = new Rectangle(0,0,16, 16);

            origin = new Vector2(-2, 18f);



            maxStackSize = 999;

            tooltip =
                "<h2>" + (backgroundBlockIDs)BlockID + " Background</h2>\n\n" +

                "<grey>The humble objects you \n" +
                "dedicate your life to destroying </grey>";

            tooltipRenderer.stringToRender = tooltip;

            //When you pick up an item, you check the inventory for an item of the same type. If that item already exists, check the stack size. If the stack size
            //is less than the max, just add to the current stacksize, otherwise add the item to the inventory in the next empty slot
        }

        public override void onLeftClick()
        {
            if (itemAnimator == null)
            {
                semiAnimationAdditions = 0;
                offsetFromEntity = new Vector2(owner.playerDirection * 8, 16);
                itemAnimator = new Animator(animationController, this, 0.15, (0, 0, 0), (0, 0, 2 * Math.PI / 3), constantRotationOffset, offsetFromEntity);

                animationController.addAnimator(itemAnimator);

                int mouseX = (int)Math.Floor((double)(Mouse.GetState().X - owner.worldContext.screenSpaceOffset.x) / owner.worldContext.pixelsPerBlock);
                int mouseY = (int)Math.Floor((double)(Mouse.GetState().Y - owner.worldContext.screenSpaceOffset.y) / owner.worldContext.pixelsPerBlock);

                if (owner.worldContext.setBackground(mouseX, mouseY, blockID))
                {

                    currentStackSize -= 1;
                }

            }
            if (semiAnimationAdditions < maxSemiAdditions)
            {
                int mouseX = (int)Math.Floor((double)(Mouse.GetState().X - owner.worldContext.screenSpaceOffset.x) / owner.worldContext.pixelsPerBlock);
                int mouseY = (int)Math.Floor((double)(Mouse.GetState().Y - owner.worldContext.screenSpaceOffset.y) / owner.worldContext.pixelsPerBlock);

                if (owner.worldContext.setBackground(mouseX, mouseY, blockID))
                {
                    currentStackSize -= 1;
                    semiAnimationAdditions += 1;
                }

            }
        }

        public override bool isItemIdentical(Item otherItem)
        {
            if (otherItem is BackgroundBlockItem b)
            {
                return b.blockID == blockID;
            }
            else
            {
                return false;
            }
        }
        public override Item itemCopy(int stackSize)
        {
            BackgroundBlockItem i = new BackgroundBlockItem(blockID);
            i.currentStackSize = stackSize;
            return i;
        }
    }

    /*
     * ========================================
     * 
     * Weapons:
     * 
     *  Items that exist soley to deal damage to other entities
     * 
     *  Bow
     * ========================================
    */
    public class Bow : Item
    {

        public double bowDamage = 10;
        public Bow()
        {
            spriteSheetID = (int)spriteSheetIDs.weapons;
            origin = new Vector2(-6f, -6f);

            sourceRectangle = new Rectangle(16, 0, 16, 16);
            drawRectangle = new Rectangle(0,0,48, 48);

            maxStackSize = 1;

            tooltip =
                "<h2>Wooden Bow</h2>\n\n" +
                "Damage: <gold>" + bowDamage + "</gold>\n" +
                "Increasing as the velocity changes\n" +
                "<grey>The greatest rival of any melee, range.</grey>";

            tooltipRenderer.stringToRender = tooltip;
        }

        public override void onLeftClick()
        {
            if (itemAnimator == null)
            {
                itemAnimator = new Animator(animationController, this, 0.3, (0, 0, 0), (0, 0, 0), 0, new Vector2(0, 0));
                if (Mouse.GetState().X < owner.x + owner.worldContext.screenSpaceOffset.x) { owner.playerDirection = -1; owner.directionalEffect = SpriteEffects.FlipHorizontally; }
                else if
                    (Mouse.GetState().X > owner.x + owner.worldContext.screenSpaceOffset.x) { owner.playerDirection = 1; owner.directionalEffect = SpriteEffects.None; }
                animationController.addAnimator(itemAnimator);
                //Generate an arrow entity
                Arrow firedArrow = new Arrow(owner.worldContext, (owner.x, owner.y), 30, owner);

            }
        }
        public override Item itemCopy(int stackSize)
        {
            Bow i = new Bow();
            i.owner = owner;
            i.animationController = animationController;
            if (stackSize < maxStackSize)
            {
                i.currentStackSize = stackSize;
            }
            else { i.currentStackSize = maxStackSize; }
            return i;
        }

    }

    /*
     * ========================================
     * 
     * Equipment:
     * 
     *  Equipable items that protect the player and occassionally add abilities or set bonuses
     * 
     *  Helmet
     * ========================================
    */
    public class Helmet : Equipment
    {
        public Helmet()
        {
            equipmentType = ArmorType.Head;

            spriteSheetID = (int)spriteSheetIDs.armour;
            sourceRectangle = new Rectangle(0, 0, 16, 16);
            drawRectangle = new Rectangle(0,0,48, 48);
        }


        public override Item itemCopy(int stackSize)
        {
            Helmet h = new Helmet();
            h.owner = owner;
            h.animationController = animationController;

            return h;
        }

    }
    
    /*
     * ========================================
     * 
     *  Accesories:
     *  
     *  Equipable items that equip to the accessory slot. 
     *  
     *  Typically provide some form of stat improvement or special ability
     *  
     *   AmuletOfFallDamage
     *   CloudInAJar
     * ========================================
     */
    public class AmuletOfFallDamage : Accessory
    {
        public AmuletOfFallDamage()
        {
            spriteSheetID = (int)spriteSheetIDs.accessories;
            sourceRectangle = new Rectangle(0, 0, 16, 16);
            drawRectangle = new Rectangle(0,0,32, 32);

            tooltip =
                "<h2>Amulet of fall negation</h2>\n\n" +

                "<grey>A magic amulet that\n" +
                "prevents the wearer from taking\n" +
                "fall damage.</grey>";

            tooltipRenderer.stringToRender = tooltip;
        }

        public override double onDamageTaken(DamageType damageType, double damage, object source)
        {
            if (damageType == DamageType.Falldamage)
            {
                damage = 0;
            }
            return damage;
        }
        public override Item itemCopy(int stackSize)
        {
            AmuletOfFallDamage i = new AmuletOfFallDamage();
            i.owner = owner;
            i.animationController = animationController;
            return i;
        }
    }
    public class CloudInAJar : Accessory
    {

        public double jumpWaitTime;
        public double maxJumpWaitTime = 0.4f;
        public bool hasSetWaitTimeOnce = false;
        public bool hasUsedItem = false;

        public double jumpAcceleration = 9;
        public CloudInAJar()
        {
            spriteSheetID = (int)spriteSheetIDs.accessories;
            sourceRectangle = new Rectangle(0, 0, 16, 16);
            drawRectangle = new Rectangle(0,0,32, 32);


            tooltip =
                "<h2>Cloud In A Jar</h2>\n\n" +

                "<grey>A bottle containing\n" +
                "a fine mist. Whoever opens\n" +
                "the jar gets a little upwards boost.</grey>";

            tooltipRenderer.stringToRender = tooltip;
        }

        public override void onInput(double elapsedTime)
        {
            if (jumpWaitTime > 0)
            {
                jumpWaitTime -= elapsedTime;
            }

            if (!owner.isOnGround)
            {
                DoubleJumpEvolution j = (DoubleJumpEvolution)owner.evolutionTree.getEvolution(typeof(DoubleJumpEvolution));
                bool canJump = false;
                if (j == null)
                {
                    canJump = true;
                }
                else
                {
                    if (j.isEvolutionActive)
                    {
                        if (j.hasDoubleJumped)
                        {
                            canJump = true;
                        }
                        else
                        {
                            canJump = false;
                        }
                    }
                    else
                    {
                        canJump = true;
                    }
                }


                if (hasSetWaitTimeOnce && canJump)
                {
                    if (jumpWaitTime <= 0)
                    {
                        if ((Keyboard.GetState().IsKeyDown(Keys.W) || Keyboard.GetState().IsKeyDown(Keys.Space)) && !hasUsedItem)
                        {
                            hasUsedItem = true;
                            if (owner.velocityY < 0)
                            {
                                owner.velocityY = 0;
                            }
                            owner.accelerationY += jumpAcceleration / elapsedTime;
                        }
                    }
                }
                else
                {
                    jumpWaitTime = maxJumpWaitTime;
                    hasSetWaitTimeOnce = true;
                }


            }
            else if (hasSetWaitTimeOnce || hasUsedItem)
            {
                hasSetWaitTimeOnce = false;
                hasUsedItem = false;
            }
        }

        public override Item itemCopy(int stackSize)
        {
            CloudInAJar i = new CloudInAJar();
            i.animationController = animationController;
            i.owner = owner;
            return i;
        }
    }

}
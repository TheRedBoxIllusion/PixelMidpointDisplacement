using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading;

using Vector2 = Microsoft.Xna.Framework.Vector2;


namespace PixelMidpointDisplacement {
    //This will be converted to an NPC controller system
    public class HouseController {
        //Whenever the player opens the inventory, and there is NPC waiting to move in somewhere,
        //check the screen for things that constitute a room

        //Algorithm:
        //Check _ number of spots on screen
        //If its air, check one direction sideways until it hits the max room size or a wall.
        //If it hits a wall, go back to the start location, go the other way and continue until the cummulative distance is the max room size
        //If the width is between the min and max room size: Proper sideways dimensions! Keep an (int, int) of the x and y of the side walls

        //Check vertically, if its between the max and min room height, continue. Keep an (int, int) of the top and bottom
        //Store a top left, and bottom right location to fully capture the room dimensions

        //Check the walls
        //Go through the top, right, bottom and left walls. Check if they are all "solid" as in collisions are active
        //Make sure that none of the blocks are transparent

        //Check the room to ensure that there is a background on all blocks.
        //Ensure that the background is not an 'unaccepted' background

        //Search the walls for a door


        //Search the room for appropriate furniture

        public WorldContext wc;
        const int maxRoomCheckCount = 20;

        const int minRoomWidth = 5;
        const int maxRoomWidth = 15;
        const int minRoomHeight = 3;
        const int maxRoomHeight = 6;

        List<House> houses = new List<House>();

        Dictionary<blockIDs, bool> acceptableWalls = new Dictionary<blockIDs, bool>() {
            {blockIDs.air, false},
            {blockIDs.dirt, true},
            { blockIDs.stone, true},
            { blockIDs.woodenDoor, true}
        };

        public HouseController(WorldContext worldContext) {
            wc = worldContext;
        }

        public void inventoryWasOpened() {
            System.Diagnostics.Debug.WriteLine(houses.Count);
            for (int i = 0; i < houses.Count; i++) {
                houses[i].showHouse();
            }
            if (wc != null)
            {
               
                checkForValidRoom();
            }
        }

        public void inventoryWasClosed() {
            for (int i = 0; i < houses.Count; i++)
            {
                houses[i].hideHouse();
            }
        }
        public void checkForValidRoom() {
            Random r = new Random();

            //Look at currently acceptable rooms
            for (int count = 0; count < maxRoomCheckCount; count++)
            {
                int checkX = r.Next(-wc.screenSpaceOffset.x / wc.pixelsPerBlock, (-wc.screenSpaceOffset.x + wc.applicationWidth) / wc.pixelsPerBlock);
                int checkY = r.Next(-wc.screenSpaceOffset.y / wc.pixelsPerBlock, (-wc.screenSpaceOffset.y + wc.applicationHeight) / wc.pixelsPerBlock);

                bool isRoomValid;
                Vector2 topRight;
                Vector2 bottomLeft;
                string rejectionReason = "";
                if (wc != null)
                {
                    (isRoomValid, topRight, bottomLeft, rejectionReason) = checkRoom(checkX, checkY);

                    if (isRoomValid)
                    {
                        //Checking system will add duplicates :(
                        if (houses.Find(h => h.topRight == topRight) == null)
                        {
                            House h = new House(checkX, checkY, topRight, bottomLeft);
                            wc.engineController.UIController.addUIElement(House.drawOrder, h.uiHouse);
                            houses.Add(h);

                        }
                    }

                    //If it is, add it to a list maybe? Not sure the best way to deal with destroying a room...
                }
            }
        }

        public (bool isRoomValid, Vector2 topRight, Vector2 bottomLeft, string rejecionReason) checkRoom(int checkX, int checkY) {
            int baseX = checkX;
            int baseY = checkY;
            bool isRoomValid = false;
            string rejectionReason = "";

            Vector2 topRight = Vector2.Zero;
            Vector2 bottomLeft = Vector2.Zero;
            if (wc.worldArray != null)
            {
                if (checkX >= 0 && checkY >= 0 && checkX < wc.worldArray.GetLength(0) && checkY < wc.worldArray.GetLength(1))
                {
                    if (wc.worldArray[checkX, checkY] != null)
                    {
                        if (wc.worldArray[checkX, checkY].ID == (int)blockIDs.air)
                        {
                            //Check sideways for an empty void that could be a room:
                            bool acceptableWidth = false;
                            bool acceptableHeight = false;
                            bool completeWalls = false;
                            bool enoughFurniture = false;


                            bool hitWallRightWithinLimit = false;
                            bool hitWallLeftWithinLimit = false;

                            int sidewaysMotionDirection = 1;
                            for (int currentWidth = 0; currentWidth <= maxRoomWidth; currentWidth++)
                            {
                                bool isBlockAnAcceptedWall = false;
                                try{
                                    if (checkX < 0 || checkY < 0 || checkX >= wc.worldArray.GetLength(0) || checkY >= wc.worldArray.GetLength(1)) {
                                        System.Diagnostics.Debug.WriteLine(checkX + ", " + checkY + " was outside the range!?");
                                    }
                                    acceptableWalls.TryGetValue((blockIDs)wc.worldArray[checkX, checkY].ID, out isBlockAnAcceptedWall);
                                    if (isBlockAnAcceptedWall)
                                    {
                                        if (sidewaysMotionDirection == 1)
                                        {
                                            hitWallLeftWithinLimit = true;
                                            bottomLeft.X = checkX;

                                            sidewaysMotionDirection = -1;
                                            checkX = baseX;
                                            checkY = baseY;

                                        }
                                        else
                                        {
                                            hitWallRightWithinLimit = true;
                                            topRight.X = checkX;
                                        }
                                    }


                                    if (hitWallRightWithinLimit && hitWallLeftWithinLimit)
                                    {
                                        if (currentWidth >= minRoomWidth && currentWidth <= maxRoomWidth)
                                        {
                                            acceptableWidth = true;

                                        }
                                        else
                                        {
                                            rejectionReason = "Not within the accepted width";
                                        }
                                        break;
                                    }
                                }
                                catch (Exception e) {
                                    System.Diagnostics.Debug.WriteLine(e.Message + " despite the if statement");
                                    throw;
                                }
                                checkX += sidewaysMotionDirection;
                            }

                            if (acceptableWidth)
                            {
                                checkX = baseX;
                                checkY = baseY;
                                int verticalMotionDirection = 1;
                                bool hitWallUpWithinLimit = false;
                                bool hitWallDownWithinLimit = false;

                                for (int currentHeight = 0; currentHeight <= maxRoomHeight; currentHeight++)
                                {
                                    bool isBlockAnAcceptedWall = false;
                                    acceptableWalls.TryGetValue((blockIDs)wc.worldArray[checkX, checkY].ID, out isBlockAnAcceptedWall);
                                    if (isBlockAnAcceptedWall)
                                    {
                                        if (verticalMotionDirection == 1)
                                        {
                                            hitWallDownWithinLimit = true;
                                            bottomLeft.Y = checkY;

                                            verticalMotionDirection = -1;
                                            checkX = baseX;
                                            checkY = baseY;
                                        }
                                        else
                                        {
                                            hitWallUpWithinLimit = true;
                                            topRight.Y = checkY;
                                        }
                                    }

                                    if (hitWallUpWithinLimit && hitWallDownWithinLimit)
                                    {
                                        if (currentHeight >= minRoomHeight && currentHeight <= maxRoomHeight)
                                        {
                                            acceptableHeight = true;
                                        }
                                        else
                                        {
                                            rejectionReason = "Not within the accepted height";
                                        }
                                        break;
                                    }

                                    checkY += verticalMotionDirection;
                                }
                            }

                            if (acceptableHeight)
                            {
                                //Check the walls:
                                //Vectors topRight and bottomLeft define the boundaries of the room:
                                //Iterate through all the walls to make sure that each block is accpetable
                                bool solidCeiling = true;
                                bool solidLeftWall = true;
                                bool solidRightWall = true;
                                bool solidFloor = true;

                                //Check Top
                                for (int x = (int)topRight.X; x < bottomLeft.X && solidCeiling; x++)
                                {
                                    acceptableWalls.TryGetValue((blockIDs)wc.worldArray[x, (int)topRight.Y].ID, out solidCeiling);
                                }

                                //Check Left wall
                                if (solidCeiling)
                                {
                                    for (int y = (int)topRight.Y; y < bottomLeft.Y && solidLeftWall; y++)
                                    {
                                        acceptableWalls.TryGetValue((blockIDs)wc.worldArray[(int)bottomLeft.X, y].ID, out solidLeftWall);
                                    }
                                }
                                //Check Right wall
                                if (solidCeiling && solidLeftWall)
                                {
                                    for (int y = (int)topRight.Y; y < bottomLeft.Y && solidLeftWall; y++)
                                    {
                                        acceptableWalls.TryGetValue((blockIDs)wc.worldArray[(int)topRight.X, y].ID, out solidRightWall);
                                    }
                                }

                                //Check Floor
                                if (solidCeiling && solidLeftWall && solidRightWall)
                                {
                                    for (int x = (int)topRight.X; x < bottomLeft.X && solidCeiling; x++)
                                    {
                                        acceptableWalls.TryGetValue((blockIDs)wc.worldArray[x, (int)bottomLeft.Y].ID, out solidFloor);
                                    }
                                }

                                completeWalls = solidCeiling && solidFloor && solidLeftWall && solidRightWall;
                                if (!completeWalls)
                                {
                                    rejectionReason = "Incomplete walls";
                                }
                            }

                            if (completeWalls)
                            {
                                //Run through the air of the room, ensure that it's empty, and check if it hits the furniture required
                                bool hasDoor = false;

                                //Check for a door
                                int yCheck = (int)bottomLeft.Y - 1;
                                try
                                {

                                    //Replace with the generic door parent block
                                    if (wc.worldArray[(int)bottomLeft.X, yCheck] is WoodenDoorBlock || wc.worldArray[(int)topRight.X, yCheck] is WoodenDoorBlock) {
                                        hasDoor = true;
                                    }

                                }
                                catch (Exception e){
                                    System.Diagnostics.Debug.WriteLine("Wall was outside the world");
                                    throw;
                                }

                                enoughFurniture = hasDoor;
                            }

                            isRoomValid = completeWalls && acceptableHeight && acceptableWidth && enoughFurniture;
                        }
                    }
                }
            }
            if (rejectionReason != "")
            {
                System.Diagnostics.Debug.WriteLine(rejectionReason);
            }
            if (isRoomValid) {
                System.Diagnostics.Debug.WriteLine("Room was accepted...");
            }
            return (isRoomValid, topRight, bottomLeft, rejectionReason);
        }

        public void tryToAssignAHouse(NPC npc) {
            for (int i = 0; i < houses.Count; i++) {
                if (houses[i].assignedNPC == null) {
                    npc.setHouse(houses[i]);
                }
            }
        }
    }

    public class House {
        public const int drawOrder = 20;
        public int x;
        public int y;

        public Vector2 topRight;
        public Vector2 bottomLeft;

        public HouseUI uiHouse;

        public NPC assignedNPC;

        public House(int x, int y, Vector2 topRight, Vector2 bottomLeft) {
            this.x = (int)Math.Floor((topRight.X + bottomLeft.X) / 2.0);
            this.y = (int)Math.Floor((topRight.Y + bottomLeft.Y) / 2.0);
            this.topRight = topRight;
            this.bottomLeft = bottomLeft;
            uiHouse = new HouseUI(this.x,this.y);
        }

        public void showHouse()
        {
            uiHouse.isUIElementActive = true;
            System.Diagnostics.Debug.WriteLine(uiHouse.isUIElementActive);
        }

        public void hideHouse() {
            uiHouse.isUIElementActive = false;
        }

        public void setHouseNPC(NPC npc) {
            assignedNPC = npc;
        }
    }

    public class NPC : SpawnableEntity, IGroundTraversalAlgorithm, IActiveCollider {
        public double targetX { get; set; }
        public double targetY { get; set; }

        public double percievedX { get; set; }
        public double percievedY { get; set; }

        public double perceptionDistance { get; set; }

        public double xDifferenceThreshold { get; set; }
        public double notJumpThreshold { get; set; }
        public double jumpWhenWithinXRange { get; set; }


        public Entity owner { get; set; }
        public bool isActive { get; set; }
        public double invincibilityCooldown { get; set; }
        public double maxInvincibilityCooldown { get; set; }

        public House house;

        public double minWait = 4;
        public double maxWait = 9;

        public bool canChangeTarget = true;

        public Timer waitTimer;
        public double maxWanderDistance = 10;

        public Dictionary<string, Dictionary<string, string>> dialogue = new Dictionary<string, Dictionary<string, string>>();

        public DialogueBox dialogueBox;

        public const string dialogueFolder = "Content\\JSON\\NPCDialogue\\";
        public NPC(WorldContext wc) : base(wc) {

            targetX = x;
            targetY = y;
            maxWanderDistance *= wc.pixelsPerBlock;

            owner = this;
            notJumpThreshold = -32;
            perceptionDistance = maxWanderDistance + 1;
            xDifferenceThreshold = 10;
            drawWidth = 1.5f;
            drawHeight = 3;

            maxMovementVelocityX = 7;

            maxInvincibilityCooldown = 0.2;
            kX = 0.02;
            kY = 0.02;

            defaultkX = 0.02;
            defaultkY = 0.02;
            minVelocityX = 0.5;
            minVelocityY = 0.01;

            width = 0.8;
            height = 2.7;

            spriteSheetID = (int)spriteSheetIDs.player;

            baseHorizontalAcceleration = 100;
            baseJumpAcceleration = 12;

            collider = new Rectangle(0, 0, (int)(width * wc.pixelsPerBlock), (int)(height * wc.pixelsPerBlock));

            drawWidth = 1.5f;
            drawHeight = 3;

            rotation = 0;
            rotationOrigin = Vector2.Zero;

            maxHealth = 100;
            baseHealth = 100;
            currentHealth = maxHealth;

            worldContext.engineController.collisionController.addActiveCollider(this);
            isActive = true;

            spriteAnimator = new SpriteAnimator(animationController: worldContext.animationController, constantOffset: new Vector2(12f, 8f), frameOffset: new Vector2(32, 65), sourceDimensions: new Vector2((float)32, (float)64), animationlessSourceRect: new Rectangle(160, 0, (int)32, (int)64), owner: this);

            spriteAnimator.animationDictionary = new Dictionary<string, (int frameCount, int yOffset)> {

                { "walk", (6, 0) }

            };

            spriteAnimator.startAnimation(0.1, "walk");

            wc.engineController.entityController.addEntity(this);


            waitTimer = new Timer(waitEnded, null, 1000, 3000);

        }

        public virtual void generateDialogueDictionary(string npcName) {
            dialogue = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + dialogueFolder + npcName + ".json"), new JsonSerializerOptions(JsonSerializerDefaults.Web));
            
        }

        public virtual void openDialogue() {
            
        }

        public virtual void dialogueButtonPress(string pressedButton) {
            if (pressedButton.ToLower() == "close")
            {
                dialogueBox.closeDialogue();
            }
            
        }

        public override void inputUpdate(double elapsedTime)
        {
            (int horizontal, int vertical) = ((IGroundTraversalAlgorithm)this).traverseTerrain();
            if (horizontal == 1)
            {
                walkRight();
            }
            else if(horizontal == 2){
                walkLeft();
            }

            if (vertical == 1) {
                jump(elapsedTime);
            } else if (vertical == -1)
            {
                drop();
            }

            if (Mouse.GetState().RightButton == ButtonState.Pressed) {
                int mouseX = Mouse.GetState().X - worldContext.screenSpaceOffset.x;
                int mouseY = Mouse.GetState().Y - worldContext.screenSpaceOffset.y;
                if (mouseX > x && mouseX < x + drawWidth * worldContext.pixelsPerBlock && mouseY > y && mouseY < y + drawHeight * worldContext.pixelsPerBlock) {
                    openDialogue();
                }

            }

            base.inputUpdate(elapsedTime);
        }


        public void setHouse(House house)
        {
            this.house = house;
            house.setHouseNPC(this);
        }

        public void waitEnded(Object stateInfo) {
            Random r = new Random();
            if (house != null)
            {
                double displacementToTravel = (r.NextDouble() * maxWanderDistance * 2) - maxWanderDistance;
                targetX = house.x * worldContext.pixelsPerBlock + displacementToTravel;
                targetY = house.y * worldContext.pixelsPerBlock;
            }
            else {
                targetX = x + (r.NextDouble() * maxWanderDistance * 2) - maxWanderDistance;
            }

            int duration = r.Next((int)(minWait * 1000), (int)(maxWait * 1000));
            waitTimer.Change(duration, duration);
        }
    }

    public class HandyNPC : NPC {
        public HandyNPC(WorldContext wc) : base(wc) {
            generateDialogueDictionary("handyman");
            dialogueBox = new GuideDialogueBox(worldContext, this);

        }


        public override void openDialogue() {
            if (dialogueBox != null) {
                if (!dialogueBox.isDialogueOpen) {

                    int index = new Random().Next(dialogue["Greetings"].Count);
                    dialogueBox.openDialogue(dialogue["Greetings"].Values.ElementAt(index));
                }
            }
        }

        public override void dialogueButtonPress(string pressedButton)
        {
            base.dialogueButtonPress(pressedButton);
            if (pressedButton.ToLower() == "help" || pressedButton.ToLower() == "hints") {
                string newDialogue = "";
                while (newDialogue != dialogueBox.dialogueString && dialogue["Hints"].Count > 0) {
                    int index = new Random().Next(dialogue["Hints"].Count);
                    newDialogue = dialogue["Hints"].Values.ElementAt(index);
                    dialogueBox.openDialogue(newDialogue);
                }
            }
        }

        public override void inputUpdate(double elapsedTime)
        {
            (int horizontal, int vertical) = ((IGroundTraversalAlgorithm)this).traverseTerrain();
            if (horizontal == 1)
            {
                walkRight();
            }
            else if (horizontal == 2)
            {
                walkLeft();
            }

            if (vertical == 1)
            {
                jump(elapsedTime);
            }
            else if (vertical == -1)
            {
                drop();
            }

            if (Mouse.GetState().RightButton == ButtonState.Pressed)
            {
                
                int mouseX = Mouse.GetState().X - worldContext.screenSpaceOffset.x;
                int mouseY = Mouse.GetState().Y - worldContext.screenSpaceOffset.y;
                if (mouseX > x && mouseX < x + drawWidth * worldContext.pixelsPerBlock && mouseY > y && mouseY < y + drawHeight * worldContext.pixelsPerBlock)
                {
                    openDialogue();
                }

            }

            base.inputUpdate(elapsedTime);
        }
    }
    public class NPCController {
        List<NPC> unlockedNPCs = new List<NPC>();

        public HouseController houseController;

        WorldContext worldContext;
        public NPCController(WorldContext wc, HouseController houseController) {
            this.worldContext = wc;
            this.houseController = houseController;
        }

        public void unlockedAnNPC(NPC npc) {
            if (!unlockedNPCs.Contains(npc))
            {
                unlockedNPCs.Add(npc);
                houseController.tryToAssignAHouse(npc);
            }
        }
    }
}
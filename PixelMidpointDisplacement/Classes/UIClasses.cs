using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using static System.Formats.Asn1.AsnWriter;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Vector4 = Microsoft.Xna.Framework.Vector4;

namespace PixelMidpointDisplacement {
    //Due to the complex, and often multi-component nature of ui elements, they shall be grouped by purpose, and individually defined instead of grouping into parent classes

    /*
     * ========================================
     *  Display Enums
     *  
     *  THe display enums define the position, scale and anchor point of all UI elements as the screen size changes
     *  
     *  UIAlignOffset defines the anchor point (top left, centre)
     *  Position defines how the position scales with screen dimensions (absolute, relative)
     *  Scale defines how the scale changes with screen dimensions (absolute, relative)
     * ========================================
    */

    public enum UIAlignOffset
    {
        TopLeft,
        Centre
    }
    public enum Position
    {
        Absolute,
        Relative,
        WorldSpace,
        BlockSpace
    }
    public enum Scale
    {
        Absolute,
        Relative
    }

    /*
     * ========================================
     * UI Parent Classes
     * 
     * UI Element is the base UI element, containing ordinary draw variables, display enums, the element's scene (when to display it) and other key things for all elements
     * Interactive UI Element is a child of the UI element with additional functionality, including left and right click functions for buttons and other interactable features.
     * ========================================
    */
    public class UIElement : DrawnClass
    {

        public float scale = 1;
        public SpriteEffects effect = SpriteEffects.None;

        public UIAlignOffset alignment = UIAlignOffset.TopLeft;
        public Position positionType = Position.Absolute;
        public Scale scaleType = Scale.Absolute;
        public Color color = Color.White;

        public const int defaultScreenWidth = 1920;
        public const int defaultScreenHeight = 1080;

        public bool isUIElementActive = true;
        public Scene scene;

        public virtual void updateElement(double elapsedTime, Game1 game) { }
    }
    public class InteractiveUIElement : UIElement
    {

        public float clickCooldown;
        public float maxClickCooldown;

        public string buttonText;
        public Vector2 textLocation;
        public virtual void onLeftClick(Game1 game) {
            clickCooldown = maxClickCooldown;
        }
        public virtual void onRightClick(Game1 game) {
            clickCooldown = maxClickCooldown;
        }
    }

    /*
     * ========================================
     * Generalised components
     * 
     * UILine
     * StringRenderer and subclasses:
     *      - StringCharacter
     *      - StringRendererBackground
     *      - StringRendererBackgroundSegment
     * ========================================
    */
    public class UILine
    {
        public Vector2 point1;
        public Vector2 point2;

        public float rotation;
        public Rectangle drawRectangle;

        public Color drawColor;

        public int lineWidth = 3;

        public Scene scene;

        public UILine(Vector2 point1, Vector2 point2)
        {
            this.point1 = point1;
            this.point2 = point2;

            drawRectangle = new Rectangle((int)point1.X, (int)point1.Y, lineWidth, 10);
        }

        public void updateSecondPoint(Vector2 secondPoint)
        {
            point2 = secondPoint;
        }

        public void updateFirstPoint(Vector2 firstPoint)
        {
            point1 = firstPoint;
            drawRectangle.X = (int)point1.X - (int)(lineWidth / 2.0);
            drawRectangle.Y = (int)point1.Y;
        }

        public virtual void updateLine()
        {


            int distance = (int)Math.Pow(Math.Pow(point1.X - point2.X, 2) + Math.Pow(point1.Y - point2.Y, 2), 0.5);
            drawRectangle.Height = distance;

            //Rotation:
            rotation = (float)Math.Atan((point1.Y - point2.Y) / (point1.X - point2.X));

            if (point2.X > point1.X)
            {
                rotation -= MathHelper.PiOver2;
            }
            else
            {
                rotation += MathHelper.PiOver2;
            }
        }
    }

    public class StringRenderer
    {
        public List<StringCharacter> visualisedString = new List<StringCharacter>();


        public string stringToRender;

        int UILayer = 18;

        int textDrawHeight;

        public bool isVisible = true;

        public StringRendererBackground background;

        WorldContext wc;

        Scene scene;
        UIAlignOffset stringOffset;
        public Position positionType;
        public Scale scaleType;

        bool haveBackground = false;

        int maxLineLength = 0;

        public int x;
        public int y;

        int borderOffsetX;
        int borderOffsetY;
        public StringRenderer(Scene scene, UIAlignOffset offset, int textHeight, bool haveBackground)
        {
            this.scene = scene;
            this.haveBackground = haveBackground;
            this.textDrawHeight = textHeight;

            stringOffset = offset;
        }

        public StringRenderer(Scene scene, UIAlignOffset offset, int textHeight, bool haveBackground, int UILayer)
        {
            this.scene = scene;
            this.haveBackground = haveBackground;
            this.textDrawHeight = textHeight;

            stringOffset = offset;
            this.UILayer = UILayer;
        }

        public void setWorldContext(WorldContext wc)
        {
            this.wc = wc;
        }

        public void updateLocation(int x, int y)
        {
            if (stringOffset == UIAlignOffset.Centre)
            {
                x -= maxLineLength / 2;
            }

            this.x = x;
            this.y = y;


            for (int i = 0; i < visualisedString.Count; i++)
            {
                visualisedString[i].drawRectangle.X = visualisedString[i].x + x;
                visualisedString[i].drawRectangle.Y = visualisedString[i].y + y;
            }
            if (background != null)
            {
                background.setLocation(x, y);
            }
        }

        public void showString()
        {
            //Re-generate the string, reduces the size of the UIElements list, and hence performance: a lot.
            setString(stringToRender);


            isVisible = true;
        }

        public void hideString()
        {
            for (int i = 0; i < visualisedString.Count; i++)
            {
                wc.engineController.UIController.removeUIElement(UILayer, visualisedString[i]);
            }

            visualisedString.Clear();
            if (background != null)
            {
                background.clear();
            }
            isVisible = false;
        }

        public void setString(string stringToSet)
        {
            stringToRender = stringToSet;

            for (int i = 0; i < visualisedString.Count; i++)
            {
                //Clear it from the other lists
                wc.engineController.UIController.removeUIElement(UILayer, visualisedString[i]);
            }
            visualisedString.Clear();

            int trailingX = 0;
            int baseTrailingX = 0;
            int maxLineHeight = 35;
            int currentLineY = 0;

            maxLineLength = 0;
            int textHeight = 0;

            Tag currentTag = Tag.None;

            if (stringToRender != null)
            {
                for (int i = 0; i < stringToRender.Length; i++)
                {
                    if (stringToRender[i] == '\n')
                    {
                        currentLineY += maxLineHeight;
                        trailingX = baseTrailingX;
                        maxLineHeight = 0;
                    }
                    else if (stringToRender[i] == '<')
                    {
                        //Find in the string the next '>' tag and take the string between
                        if (stringToRender[i + 1] != '/')
                        {
                            int endTagIndex = stringToRender.IndexOf('>', i);
                            string tag = stringToRender.Substring(i + 1, endTagIndex - (i + 1)).ToLower();

                            switch (tag)
                            {
                                case "purple":
                                    currentTag |= Tag.Purple;
                                    break;
                                case "green":
                                    currentTag |= Tag.Green;
                                    break;
                                case "grey":
                                    currentTag |= Tag.Grey;
                                    break;
                                case "gold":
                                    currentTag |= Tag.Gold;
                                    break;
                                case "red":
                                    currentTag |= Tag.Red;
                                    break;
                                case "h1":
                                    currentTag |= Tag.H1;
                                    break;
                                case "h2":
                                    currentTag |= Tag.H2;
                                    break;
                                case "h3":
                                    currentTag |= Tag.H3;
                                    break;

                            }

                            i = endTagIndex;
                        }
                        else
                        {
                            //Is an end tag
                            int endTagIndex = stringToRender.IndexOf('>', i);
                            string tag = stringToRender.Substring(i + 2, endTagIndex - (i + 2)).ToLower();

                            switch (tag)
                            {
                                case "purple":
                                    currentTag &= ~Tag.Purple;
                                    break;
                                case "green":
                                    currentTag &= ~Tag.Green;
                                    break;
                                case "grey":
                                    currentTag &= ~Tag.Grey;
                                    break;
                                case "gold":
                                    currentTag &= ~Tag.Gold;
                                    break;
                                case "red":
                                    currentTag &= ~Tag.Red;
                                    break;
                                case "h1":
                                    currentTag &= ~Tag.H1;
                                    break;
                                case "h2":
                                    currentTag &= ~Tag.H2;
                                    break;
                                case "h3":
                                    currentTag &= ~Tag.H3;
                                    break;

                            }
                            i = endTagIndex;
                        }
                    }
                    else
                    {
                        TextStyle currentStyle = TextStyle.None;
                        if ((currentTag & Tag.H1) == Tag.H1)
                        {
                            currentStyle |= TextStyle.h1;
                        }
                        else if ((currentTag & Tag.H2) == Tag.H2)
                        {
                            currentStyle |= TextStyle.h2;
                        }
                        else if ((currentTag & Tag.H3) == Tag.H3)
                        {
                            currentStyle |= TextStyle.h3;
                        }

                        StringCharacter c = new StringCharacter(stringToRender[i], textDrawHeight, trailingX, currentLineY, currentStyle);

                        c.scene = scene;
                        c.alignment = stringOffset;
                        //I could improve this by allowing for nested color tags
                        if ((currentTag & Tag.Purple) == Tag.Purple)
                        {
                            c.color = Color.Purple;
                        }
                        else if ((currentTag & Tag.Green) == Tag.Green)
                        {
                            c.color = Color.Green;
                        }
                        else if ((currentTag & Tag.Gold) == Tag.Gold)
                        {
                            c.color = Color.Gold;
                        }
                        else if ((currentTag & Tag.Grey) == Tag.Grey)
                        {
                            c.color = Color.Gray;
                        }
                        else if ((currentTag & Tag.Red) == Tag.Red)
                        {
                            c.color = Color.Red;
                        }
                        trailingX += c.drawRectangle.Width;
                        if (maxLineLength < trailingX)
                        {
                            maxLineLength = trailingX;
                        }
                        if (c.drawRectangle.Height > maxLineHeight)
                        {
                            maxLineHeight = c.drawRectangle.Height;
                        }

                        c.positionType = positionType;
                        c.scaleType = scaleType;
                        visualisedString.Add(c);
                        wc.engineController.UIController.addUIElement(UILayer, c);
                    }
                }
            }
            if (background != null)
            {
                background.clear();
            }
            if (haveBackground)
            {
                background = new StringRendererBackground(wc, scene, stringOffset, maxLineLength, currentLineY + maxLineHeight, x, y);
            }
        }

        public void replaceAllTags(Tag find, Tag replaceWith)
        {
            string tagToFind = tagToString(find);



            string tagToReplace = tagToString(replaceWith);
            //Replace the start and end tags in two different stages:
            //Add a "<" at the start, then insert a "><" into all of the spaces, then add a ">"
            if (tagToFind != "" && tagToReplace != "")
            {
                string startTagToFind = tagToFind;
                startTagToFind = "<" + startTagToFind;
                while (startTagToFind != startTagToFind.Replace(" ", "><"))
                {
                    startTagToFind = startTagToFind.Replace(" ", "><");
                }
                string startTagToReplace = tagToReplace;
                startTagToReplace = "<" + startTagToReplace;
                while (startTagToReplace != startTagToReplace.Replace(" ", "><"))
                {
                    startTagToReplace = startTagToReplace.Replace(" ", "><");
                }

                while (stringToRender.Replace(startTagToFind, startTagToReplace) != stringToRender)
                {
                    stringToRender = stringToRender = stringToRender.Replace(startTagToFind, startTagToReplace);
                }


                //End tags:

                string endTagToFind = tagToFind;
                endTagToFind = "</" + endTagToFind;
                while (endTagToFind != endTagToFind.Replace(" ", "></"))
                {
                    endTagToFind = endTagToFind.Replace(" ", "></");
                }

                string endTagToReplace = tagToReplace;
                endTagToReplace = "</" + endTagToReplace;
                while (endTagToReplace != endTagToReplace.Replace(" ", "></"))
                {
                    endTagToReplace = endTagToReplace.Replace(" ", "></");
                }

                while (stringToRender.Replace(endTagToFind, endTagToReplace) != stringToRender)
                {
                    stringToRender = stringToRender.Replace(endTagToFind, endTagToReplace);
                }


            }
            setString(stringToRender);
        }

        public string tagToString(Tag tag)
        {
            string stringVersion = "";
            if ((tag & Tag.Purple) == Tag.Purple)
            {
                stringVersion += "purple ";
            }
            if ((tag & Tag.Green) == Tag.Green)
            {
                stringVersion += "green ";
            }
            if ((tag & Tag.Grey) == Tag.Grey)
            {
                stringVersion += "grey ";
            }
            if ((tag & Tag.Gold) == Tag.Gold)
            {
                stringVersion += "gold ";
            }
            if ((tag & Tag.Red) == Tag.Red)
            {
                stringVersion += "red ";
            }
            if ((tag & Tag.H1) == Tag.H1)
            {
                stringVersion += "h1 ";
            }
            if ((tag & Tag.H2) == Tag.H2)
            {
                stringVersion += "h2 ";
            }
            if ((tag & Tag.H3) == Tag.H3)
            {
                stringVersion += "h3 ";
            }

            stringVersion = stringVersion.TrimEnd();

            return stringVersion;

        }
    }
    public class StringCharacter : UIElement
    {
        double drawScale = 3;


        const int characterWidth = 5;
        const int characterHeight = 7;

        public int x;
        public int y;

        bool isAlphabet = false;

        public StringCharacter(char character, int drawHeight, int x, int y, TextStyle style)
        {
            isUIElementActive = true;
            spriteSheetID = (int)spriteSheetIDs.stringRendering;

            alignment = UIAlignOffset.TopLeft;
            scaleType = Scale.Absolute;
            positionType = Position.Absolute;

            drawScale = drawHeight / (double)characterHeight;

            this.x = x;
            this.y = y;
            int relativeValue = -4;
            if (65 <= character && character <= 90)
            {
                //Is a capital letter
                relativeValue = character - 'A';
                isAlphabet = true;
            }
            else if (97 <= character && character <= 122)
            {
                relativeValue = character - 'a';
                isAlphabet = true;
            }

            if (style == TextStyle.h1)
            {
                drawScale *= 2;
            }
            else if (style == TextStyle.h2)
            {
                drawScale = drawScale * 1.8;
            }
            else if (style == TextStyle.h3)
            {
                drawScale = drawScale * 1.6;
            }
            else if (style == TextStyle.h4)
            {
                drawScale = drawScale * 1.4;
            }
            else if (style == TextStyle.h5)
            {
                drawScale = drawScale * 1.2;
            }

            drawRectangle = new Rectangle(x, y, (int)(characterWidth * drawScale), (int)(characterHeight * drawScale));

            if (isAlphabet)
            {
                sourceRectangle = new Rectangle(4 * relativeValue, 0, 5, 7);

                if (relativeValue > 12)
                {
                    sourceRectangle.X += 2;
                }
                else if (relativeValue == 12)
                {
                    sourceRectangle.Width += 2;
                    drawRectangle.Width += (int)(drawScale * 2);
                }
                if (relativeValue > 13)
                {
                    sourceRectangle.X += 1;
                }
                else if (relativeValue == 13)
                {
                    sourceRectangle.Width += 1;

                    drawRectangle.Width += (int)drawScale;
                }

                if (relativeValue > 22)
                {
                    sourceRectangle.X += 2;
                }
                else if (relativeValue == 22)
                {
                    sourceRectangle.Width += 2;

                    drawRectangle.Width += (int)(drawScale * 2);
                }
            }

            //If it's not an alphabetical character, then check if it's a number:
            else if (48 <= character && character <= 57)
            {
                relativeValue = character - 48;
                sourceRectangle = new Rectangle(relativeValue * (characterWidth - 1), 6, characterWidth, characterHeight);
            }
            //Special cases
            //Does use magic numbers, so sorry, however they can be found from the alphabet.png file
            if (character == ' ')
            {
                drawRectangle.Width = (int)(characterWidth * drawScale) / 2;
            }
            else if (character == ':')
            {
                sourceRectangle = new Rectangle(0, 14, characterWidth, characterHeight);
            }
            else if (character == '!')
            {
                sourceRectangle = new Rectangle(5, 14, characterWidth, characterHeight);
            }
            else if (character == '%')
            {
                sourceRectangle = new Rectangle(9, 14, characterWidth, characterHeight);
            }
        }

    }
    public class StringRendererBackground
    {
        public int x;
        public int y;
        StringRendererBackgroundSegment top = new StringRendererBackgroundSegment();
        StringRendererBackgroundSegment body = new StringRendererBackgroundSegment();
        StringRendererBackgroundSegment bottom = new StringRendererBackgroundSegment();

        const int UILayer = 17;

        WorldContext wc;


        const int imageWidth = 64;
        const int pixelsHighToBorder = 12;

        double drawScale = 1;
        //3 parts:

        int height;
        int width;

        const int pixelOffset = 8;

        public StringRendererBackground(WorldContext wc, Scene scene, UIAlignOffset offset, int width, int height, int x, int y)
        {
            //Scale the image according to the width, then variably increase the height by cutting it into 3 sections
            this.x = x;
            this.y = y;
            this.wc = wc;


            drawScale = (double)width / (imageWidth - 2 * pixelOffset);

            height += (int)(drawScale);
            width += (int)(2 * pixelOffset * drawScale);
            this.height = height;
            this.width = width;

            top.scene = scene;
            top.alignment = offset;
            body.scene = scene;
            body.alignment = offset;
            bottom.scene = scene;
            bottom.alignment = offset;
            top.drawRectangle = new Rectangle(x, y, width, (int)(pixelsHighToBorder * drawScale));
            body.drawRectangle = new Rectangle(x, y + (int)(pixelsHighToBorder * drawScale), width, height - (int)(pixelsHighToBorder * drawScale));
            bottom.drawRectangle = new Rectangle(x, y + height, width, (int)(pixelsHighToBorder * drawScale));

            top.sourceRectangle = new Rectangle(0, 0, imageWidth, pixelsHighToBorder);
            body.sourceRectangle = new Rectangle(0, pixelsHighToBorder, imageWidth, imageWidth - 2 * pixelsHighToBorder);
            bottom.sourceRectangle = new Rectangle(0, imageWidth - pixelsHighToBorder, imageWidth, pixelsHighToBorder);

            wc.engineController.UIController.addUIElement(UILayer, top);
            wc.engineController.UIController.addUIElement(UILayer, body);
            wc.engineController.UIController.addUIElement(UILayer, bottom);

        }

        public void setLocation(int x, int y)
        {
            top.drawRectangle.X = x - (int)(pixelOffset * drawScale);
            top.drawRectangle.Y = y - (int)(pixelOffset * drawScale);

            body.drawRectangle.X = x - (int)(pixelOffset * drawScale);
            body.drawRectangle.Y = y + (int)(pixelsHighToBorder * drawScale) - (int)(pixelOffset * drawScale);


            bottom.drawRectangle.X = x - (int)(pixelOffset * drawScale);
            bottom.drawRectangle.Y = y + height - (int)(pixelOffset * drawScale);


        }

        public void hideBackground()
        {
            top.isUIElementActive = false;
            body.isUIElementActive = false;
            bottom.isUIElementActive = false;
        }

        public void showBackground()
        {
            top.isUIElementActive = true;
            body.isUIElementActive = true;
            bottom.isUIElementActive = true;
        }

        public void clear()
        {
            wc.engineController.UIController.removeUIElement(UILayer, top);
            wc.engineController.UIController.removeUIElement(UILayer, body);
            wc.engineController.UIController.removeUIElement(UILayer, bottom);
        }
    }
    public class StringRendererBackgroundSegment : UIElement
    {
        public StringRendererBackgroundSegment()
        {
            positionType = Position.Absolute;
            scaleType = Scale.Absolute;
            alignment = UIAlignOffset.TopLeft;
            spriteSheetID = (int)spriteSheetIDs.tooltipBackground;
            isUIElementActive = true;
        }
    }

    /*
     * ========================================
     *  Main Menu
     * ========================================
    */
    public class MainMenuTitle : UIElement
    {
        public MainMenuTitle()
        {
            spriteSheetID = (int)spriteSheetIDs.mainMenuUI;
            drawRectangle = new Rectangle(0, 50, 1160, 152);
            sourceRectangle = new Rectangle(0, 0, 145, 19);
            alignment = UIAlignOffset.Centre;
            scaleType = Scale.Relative;
            positionType = Position.Relative;

            scene = Scene.MainMenu;
        }
    }
    public class MainMenuStartButton : InteractiveUIElement
    {
        UIElement generateWorldText;
        int tickCount = 0;

        Timer t;
        bool timerFinished = true;

        SpriteAnimator animator;
        public MainMenuStartButton(AnimationController ac, UIElement generateWorldText)
        {
            spriteSheetID = (int)spriteSheetIDs.mainMenuUI;
            drawRectangle = new Rectangle(0, 400, 192, 66);
            sourceRectangle = new Rectangle(0, 25, 33, 12);
            alignment = UIAlignOffset.Centre;
            positionType = Position.Relative;
            scaleType = Scale.Relative;
            tickCount = 0;
            scene = Scene.MainMenu;

            animator = new SpriteAnimator(animationController : ac, constantOffset : new Vector2(0,25), frameOffset : new Vector2(33, 25), sourceDimensions : new Vector2(33,12), new Rectangle(0, 25, 33, 12), owner : this);


            //this.generateWorldText = generateWorldText;

            animator.animationDictionary = new Dictionary<string, (int frameCount, int yOffset)>()
            {
                { "shine", (4,1)}
            };


            t = new Timer(new TimerCallback(shineAnimation));
            t.Change(100, 0);
        }

        public void shineAnimation(object state) {
            animator.startAnimation(1, "shine");
            timerFinished = true;
        }
        public override void onLeftClick(Game1 game)
        {
            //generateWorldText.isUIElementActive = true;
            if (tickCount == 0)
            {
                StringRenderer sr = new StringRenderer(Scene.MainMenu, UIAlignOffset.Centre, 42, false);
                sr.setWorldContext(game.worldContext);
                sr.setString("Generating the world");
                sr.updateLocation(0, 350);

            }
            tickCount += 1;

        }
        public override void updateElement(double elapsedTime, Game1 game)
        {
            if (timerFinished)
            {
                Random r = new Random();
                t.Change(r.Next(1600, 3600), 0);
                timerFinished = false;
            }

            //If the button was pressed for 2 ticks, then generate the world. This allows the UI to update

            if (tickCount > 10)
            {
                (int width, int height) worldDimensions = (800, 800);
                game.worldContext.generateWorld(worldDimensions);
                game.changeScene(Scene.Game);
            }
        }
    }
    public class MainMenuWorldGenText : UIElement
    {
        public MainMenuWorldGenText()
        {
            isUIElementActive = false;
            spriteSheetID = (int)spriteSheetIDs.mainMenuUI;
            drawRectangle = new Rectangle(0, 350, 576, 30);
            sourceRectangle = new Rectangle(0, 38, 96, 5);
            alignment = UIAlignOffset.Centre;
            positionType = Position.Relative;
            scaleType = Scale.Relative;
            scene = Scene.MainMenu;
        }
    }

    /*
     * ========================================
     * Functional inventory UI
     * ========================================
    */
    public class FloatingUIItem : InteractiveUIElement
    {

        public Item item;
        public Player owner;

        public bool clickedOnAUIElement = false;

        public FloatingUIItem(Player owner)
        {
            isUIElementActive = false;
            scene = Scene.Game;
            setItem(null);
            this.owner = owner;
            maxClickCooldown = 0.1f;
        }
        public void setItem(Item item)
        {
            if (item != null)
            {
                this.item = item;
                item.onPickup(owner);
                if (item.currentStackSize <= 1) { buttonText = null; }
                isUIElementActive = true;
                spriteSheetID = item.spriteSheetID;
                sourceRectangle = item.sourceRectangle;
                int offsetWidth = item.drawRectangle.Width;
                int offsetHeight = item.drawRectangle.Height;

                //If the sprite is the exact same size, don't offset it by anything
                //If the sprite is smaller, offset it by half - half the width
                drawRectangle = new Rectangle(Mouse.GetState().X + ((64 - offsetWidth) / 2), Mouse.GetState().Y + ((64 - offsetHeight) / 2), item.drawRectangle.Width, item.drawRectangle.Height);
            }
            else
            {
                this.item = null;
                isUIElementActive = false;
                sourceRectangle = new Rectangle(0, 0, 0, 0);
                drawRectangle = new Rectangle(Mouse.GetState().X, Mouse.GetState().Y, 64, 64);
            }
        }

        public override void updateElement(double elasedTime, Game1 game)
        {
            if (isUIElementActive)
            {
                drawRectangle.X = Mouse.GetState().X;
                drawRectangle.Y = Mouse.GetState().Y;
                if (item != null)
                {
                    if (item.currentStackSize > 1)
                    {
                        buttonText = item.currentStackSize.ToString();
                    }
                    textLocation = new Vector2(drawRectangle.X + item.drawRectangle.Width, drawRectangle.Y + item.drawRectangle.Height);
                }
            }
        }

        public void dropItem()
        {
            if (item != null)
            {
                float pickupDelay = 1f;
                DroppedItem dropItem = new DroppedItem(owner.worldContext, item.itemCopy(item.currentStackSize), (owner.x, owner.y), Vector2.Zero);
                dropItem.x = owner.x;
                dropItem.y = owner.y;
                dropItem.pickupDelay = pickupDelay;
                owner.worldContext.engineController.entityController.addEntity(dropItem);
                setItem(null);
            }
        }

        public override void onLeftClick(Game1 game)
        {
            if (item != null && isUIElementActive)
            {
                clickCooldown = maxClickCooldown;
                if (!clickedOnAUIElement)
                {
                    bool isOutsideAllActiveInventoryBackgrounds = true;
                    for (int i = 0; i < game.worldContext.engineController.UIController.inventoryBackgrounds.Count; i++)
                    {
                        UIElement inventoryBackground = game.worldContext.engineController.UIController.inventoryBackgrounds[i];
                        if (inventoryBackground.isUIElementActive)
                        {
                            if (drawRectangle.X > inventoryBackground.drawRectangle.X && drawRectangle.Y > inventoryBackground.drawRectangle.Y && drawRectangle.X < inventoryBackground.drawRectangle.X + inventoryBackground.drawRectangle.Width && drawRectangle.Y < inventoryBackground.drawRectangle.Y + inventoryBackground.drawRectangle.Height)
                            {
                                isOutsideAllActiveInventoryBackgrounds = false;
                                //No need to continue checking, just break

                                break;
                            }
                        }
                    }
                    if (isOutsideAllActiveInventoryBackgrounds)
                    {
                        dropItem();
                    }
                }


                clickedOnAUIElement = false;
            }


        }
    }
    public class UIItem : InteractiveUIElement
    {
        public Item item;
        public Player owner;
        public IInventory inventory;
        //A class that represents an item. Each ui element contains it's own corrosponding item. 
        public int drawX;
        public int drawY;


        public const int inventorySlotSize = 66;

        public (int x, int y) inventoryIndex;

        bool lockedToInventory = true;
        //When a droppedItem entity is picked up, it either adjusts the item of the UIItem element, or it creates both a new UiElement and item class
        public UIItem(int x, int y, int inventoryDrawOffsetX, int inventoryDrawOffsetY, Player owner)
        {
            isUIElementActive = false;
            scene = Scene.Game;
            this.owner = owner;
            inventory = owner;

            inventoryIndex = (x, y);
            setDrawLocation(inventoryDrawOffsetX, inventoryDrawOffsetY);
            maxClickCooldown = 0.1f;
            //Just set the ID to be a random sprite sheet that has opacity
            spriteSheetID = (int)spriteSheetIDs.weapons;
            textLocation = Vector2.Zero;
            setItem(null);
        }
        public UIItem(int x, int y, int inventoryDrawOffsetX, int inventoryDrawOffsetY, WorldContext worldContext, IInventory inventoryClass)
        {
            isUIElementActive = false;
            scene = Scene.Game;
            this.owner = worldContext.player;
            inventory = inventoryClass;
            inventoryIndex = (x, y);
            setDrawLocation(inventoryDrawOffsetX, inventoryDrawOffsetY);
            maxClickCooldown = 0.1f;
            //Just set the ID to be a random sprite sheet that has opacity
            spriteSheetID = (int)spriteSheetIDs.weapons;
            textLocation = Vector2.Zero;
            setItem(null);
        }

        public void setDrawLocation(int inventoryDrawOffsetX, int inventoryDrawOffsetY)
        {
            drawX = inventorySlotSize * inventoryIndex.x + inventoryDrawOffsetX;
            drawY = inventorySlotSize * inventoryIndex.y + inventoryDrawOffsetY;
        }

        public void setItem(Item item)
        {
            if (item != null)
            {
                this.item = item;
                item.onPickup(owner);
                if (item.currentStackSize <= 1) { buttonText = null; }
                spriteSheetID = item.spriteSheetID;
                sourceRectangle = item.sourceRectangle;
                int offsetWidth = item.drawRectangle.Width;
                int offsetHeight = item.drawRectangle.Height;

                //If the sprite is the exact same size, don't offset it by anything
                //If the sprite is smaller, offset it by half - half the width
                drawRectangle = new Rectangle(drawX + ((64 - offsetWidth) / 2), drawY + ((64 - offsetHeight) / 2), item.drawRectangle.Width, item.drawRectangle.Height);
                textLocation = new Vector2(drawX + offsetWidth + ((64 - offsetWidth) / 2), drawY + offsetWidth + ((64 - offsetHeight) / 2));
            }
            else
            {
                this.item = null;
                sourceRectangle = new Rectangle(0, 0, 0, 0);
                drawRectangle = new Rectangle(drawX, drawY, 64, 64);
            }
        }

        public override void updateElement(double elasedTime, Game1 game)
        {
            if (item != null)
            {
                if (item.currentStackSize > 1)
                {
                    buttonText = item.currentStackSize.ToString();
                }
                if (isUIElementActive)
                {
                    if (item.tooltipRenderer != null)
                    {

                        if (new Rectangle(Mouse.GetState().X, Mouse.GetState().Y, 5, 5).Intersects(drawRectangle))
                        {
                            if (item.tooltipRenderer.isVisible == false)
                            {
                                item.tooltipRenderer.showString();
                            }
                            else
                            {
                                item.tooltipRenderer.updateLocation(Mouse.GetState().X + 25, Mouse.GetState().Y + 25);
                            }
                        }
                        else if (item.tooltipRenderer.isVisible == true)
                        {
                            item.tooltipRenderer.hideString();
                        }
                    }
                }
            }
            else
            {
                buttonText = "";
            }
        }
        public override void onLeftClick(Game1 game)
        {
            clickCooldown = maxClickCooldown;
            Item floatingItem = owner.selectedItem.item;
            bool couldCombineItems = false;
            if (floatingItem != null && item != null)
            {
                if (floatingItem.isItemIdentical(item))
                {

                    couldCombineItems = inventory.combineItemStacks(floatingItem, inventoryIndex.x, inventoryIndex.y);
                    if (couldCombineItems)
                    {
                        if (item != null)
                        {
                            if (item.tooltipRenderer != null)
                            {
                                item.tooltipRenderer.hideString();
                            }
                        }
                        owner.selectedItem.setItem(null);
                        inventory.inventory[inventoryIndex.x, inventoryIndex.y].setItem(inventory.inventory[inventoryIndex.x, inventoryIndex.y].item);
                    }
                }
            }
            if (!couldCombineItems)
            {
                if (item != null)
                {
                    if (item.tooltipRenderer != null)
                    {
                        item.tooltipRenderer.hideString();
                    }
                }

                owner.selectedItem.setItem(item);
                setItem(floatingItem);
                if (inventoryIndex.x == owner.mainHandIndex && owner == inventory)
                {
                    owner.mainHand = null;
                }
            }
        }
    }
    public class EquipmentUIItem : UIItem
    {
        public ArmorType slotEquipmentType;

        public EquipmentUIItem(ArmorType equipmentSlotType, int x, int y, int inventoryDrawOffsetX, int inventoryDrawOffsetY, Player owner) : base(x, y, inventoryDrawOffsetX, inventoryDrawOffsetY, owner)
        {
            slotEquipmentType = equipmentSlotType;
            this.setDrawLocation(inventoryDrawOffsetX, inventoryDrawOffsetY);

        }

        public override void onLeftClick(Game1 game)
        {
            clickCooldown = maxClickCooldown;

            if (owner.selectedItem.item is Equipment floatingEquipmentItem)
            {

                if (floatingEquipmentItem.equipmentType == slotEquipmentType)
                {
                    owner.selectedItem.setItem(item);
                    if (item is Equipment previouslyEquipment)
                    {
                        previouslyEquipment.onUnequipFromSlot();
                    }
                    setItem(floatingEquipmentItem);
                    if (floatingEquipmentItem != null)
                    {
                        //Because the armor and all that never reach the mainhand, equip them 
                        floatingEquipmentItem.onEquipToSlot();
                    }

                }
            }
            else if (owner.selectedItem.item == null)
            {
                owner.selectedItem.setItem(item);
                setItem(null);
            }
        }
    }
    public class AccessoryUIItem : UIItem
    {

        public AccessoryUIItem(int x, int y, int inventoryDrawOffsetX, int inventoryDrawOffsetY, Player owner) : base(x, y, inventoryDrawOffsetX, inventoryDrawOffsetY, owner)
        {

        }
        public override void onLeftClick(Game1 game)
        {
            clickCooldown = maxClickCooldown;
            if (owner.selectedItem.item is Accessory floatingAccessoryItem)
            {

                owner.selectedItem.setItem(item);
                if (item is Accessory previouslyEquipment)
                {
                    previouslyEquipment.onUnequipFromSlot();
                }
                setItem(floatingAccessoryItem);
                if (floatingAccessoryItem != null)
                {
                    floatingAccessoryItem.onEquipToSlot();
                }


            }
            else if (owner.selectedItem.item == null)
            {
                owner.selectedItem.setItem(item);
                setItem(null);
            }
        }
    }

    /*
     * ========================================
     *  Inventory background / UX
     * ========================================
    */
    public class InventoryBackground : UIElement
    {
        public InventoryBackground()
        {
            spriteSheetID = (int)spriteSheetIDs.inventoryUI;
            drawRectangle = new Rectangle(0, 66, 594, 266);
            sourceRectangle = new Rectangle(0, 32, 297, 132);
            alignment = UIAlignOffset.TopLeft;
            positionType = Position.Absolute;
            scaleType = Scale.Absolute;
            scene = Scene.Game;
            isUIElementActive = false;
        }
    }

    public class EquipmentBackground : UIElement
    {
        public EquipmentBackground()
        {
            spriteSheetID = (int)spriteSheetIDs.inventoryUI;
            isUIElementActive = false;
            sourceRectangle = new Rectangle(297, 0, 65, 132);
            drawRectangle = new Rectangle(700, 66, 130, 264);
            alignment = UIAlignOffset.TopLeft;
            positionType = Position.Absolute;
            scaleType = Scale.Absolute;
            scene = Scene.Game;
        }
    }
    public class Hotbar : UIElement
    {
        public Hotbar()
        {
            spriteSheetID = (int)spriteSheetIDs.inventoryUI;
            drawRectangle = new Rectangle(0, 0, 594, 64);
            sourceRectangle = new Rectangle(0, 0, 297, 32);
            alignment = UIAlignOffset.TopLeft;
            positionType = Position.Absolute;
            scaleType = Scale.Absolute;
            scene = Scene.Game;
        }
    }
    public class HotbarSelected : UIElement
    {
        public HotbarSelected()
        {
            spriteSheetID = (int)spriteSheetIDs.inventoryUI;
            drawRectangle = new Rectangle(0, 0, 64, 64);
            sourceRectangle = new Rectangle(1, 165, 32, 32);
            alignment = UIAlignOffset.TopLeft;
            positionType = Position.Absolute;
            scaleType = Scale.Absolute;
            scene = Scene.Game;
        }

        public void swapItem(int x)
        {
            int pixelsPerHotbarSlot = 66;
            drawRectangle = new Rectangle(pixelsPerHotbarSlot * x, 0, 64, 64);
        }
    }

    /*
     * ========================================
     * Health UI
     * ========================================
    */
    public class HealthBarOutline : UIElement
    {
        public HealthBarOutline()
        {
            spriteSheetID = (int)spriteSheetIDs.healthUI;
            sourceRectangle = new Rectangle(0, 0, 145, 23);
            drawRectangle = new Rectangle(0, 900, 290, 46);
            alignment = UIAlignOffset.Centre;
            positionType = Position.Relative;
            scaleType = Scale.Relative;
            scene = Scene.Game;
        }
    }
    public class HealthBar : UIElement
    {
        public int maxHealthDrawWidth = 290;
        public HealthBar()
        {
            spriteSheetID = (int)spriteSheetIDs.healthUI;
            sourceRectangle = new Rectangle(0, 25, 145, 21);
            drawRectangle = new Rectangle(0, 902, 290, 46);
            alignment = UIAlignOffset.Centre;
            positionType = Position.Relative;
            scaleType = Scale.Relative;
            scene = Scene.Game;
        }
    }

    /*
     * ========================================
     * Respawn UI
     * ========================================
    */

    public class RespawnUI {
        RespawnScreen rs = new RespawnScreen();
        RespawnButton rb;
        StringRenderer sr;
        Player player;

        //Put a _ where the source should be
        List<string> deathMessages = new List<string>() {
            "_ tried and succeeded to kill you!"
        };
        public RespawnUI(WorldContext wc, Player player) {
            this.player = player;
            rb = new RespawnButton(this);
            wc.engineController.UIController.addUIElement(150, rb);
            wc.engineController.UIController.addUIElement(150, rs);

            sr = new StringRenderer(Scene.Game, UIAlignOffset.Centre, 26, false, 151);

            sr.setWorldContext(wc);
        }

        public void onDeath(object source) {
            //Generate a string to render
            Random r = new Random();
            int index = r.Next(deathMessages.Count);
            string baseString = deathMessages[index];
            if (source != null)
            {
                baseString = baseString.Replace("_", "\"" + source.ToString().Substring(source.ToString().LastIndexOf(".")));
            }
            else {
                baseString = baseString.Replace("_", "The world");
            }

            sr.setString(baseString);
            sr.updateLocation(sr.x, 200);
            rs.isUIElementActive = true;
            rb.isUIElementActive = true;
        }

        public void respawn(Game1 game){
            player.respawn();
            game.changeScene(Scene.Evolution);
            rs.isUIElementActive = false;
            rb.isUIElementActive = false;
            sr.hideString();
        }
    }
    public class RespawnScreen : UIElement
    {
        public RespawnScreen()
        {
            spriteSheetID = (int)spriteSheetIDs.deathScreen;
            sourceRectangle = new Rectangle(0, 0, 384, 216);
            drawRectangle = new Rectangle(0, 0, 1920, 1080);
            alignment = UIAlignOffset.TopLeft;
            positionType = Position.Absolute;
            scaleType = Scale.Relative;
            scene = Scene.Game;

            isUIElementActive = false;
        }
    }

    public class RespawnButton : InteractiveUIElement
    {
        RespawnUI rUI;
        public RespawnButton(RespawnUI respawnUI)
        {
            spriteSheetID = (int)spriteSheetIDs.deathScreen;
            sourceRectangle = new Rectangle(177, 74, 40, 8);
            drawRectangle = new Rectangle(885, 370, 200, 40);
            alignment = UIAlignOffset.TopLeft;
            positionType = Position.Relative;
            scaleType = Scale.Relative;
            scene = Scene.Game;
            isUIElementActive = false;
            this.rUI = respawnUI;
        }

        public override void onLeftClick(Game1 game)
        {
            rUI.respawn(game);
            
        }
    }

    public class EndEvolutionButton : InteractiveUIElement
    {

        double mouseMovementCoefficient = 0.027;
        public EndEvolutionButton()
        {
            spriteSheetID = (int)spriteSheetIDs.deathScreen;
            sourceRectangle = new Rectangle(177, 74, 40, 12);
            drawRectangle = new Rectangle(885, 900, 200, 60);
            alignment = UIAlignOffset.TopLeft;
            positionType = Position.Relative;
            scaleType = Scale.Relative;
            scene = Scene.Evolution;
            isUIElementActive = true;
        }

        public override void updateElement(double elapsedTime, Game1 game)
        {
            if (Mouse.GetState().X >= 0 && Mouse.GetState().X < game.GraphicsDevice.Viewport.Width)
            {
                drawRectangle.X = 885 + (int)(mouseMovementCoefficient * Mouse.GetState().X);
            }

            if (Mouse.GetState().Y >= 0 && Mouse.GetState().Y < game.GraphicsDevice.Viewport.Height)
            {
                drawRectangle.Y = 900 + (int)(mouseMovementCoefficient * Mouse.GetState().Y);
            }
        }

        public override void onLeftClick(Game1 game)
        {
            game.changeScene(Scene.Game);
        }
    }

    public class Damage : UIElement
    {
        WorldContext worldContext;

        double x;
        double y;

        int drawOrder;

        double yIncrease = 30;
        double maxExistingDuration = 0.5;
        double existingDuration;

        public Damage(WorldContext wc, int damageAmount, double x, double y, int drawOrder)
        {

            this.drawOrder = drawOrder;
            drawRectangle = new Rectangle((int)x, (int)y, 10, 14);
            this.x = x;
            this.y = y;
            existingDuration = maxExistingDuration;

            sourceRectangle = new Rectangle(damageAmount * 6, 0, 5, 7);
            worldContext = wc;
            spriteSheetID = (int)spriteSheetIDs.pixelNumbers;

            alignment = UIAlignOffset.TopLeft;
            positionType = Position.WorldSpace;
            scaleType = Scale.Absolute;
            scene = Scene.Game;
            isUIElementActive = true;
        }

        public override void updateElement(double elapsedTime, Game1 game)
        {
            existingDuration -= elapsedTime;
            y -= ((yIncrease / maxExistingDuration) * elapsedTime);

            if (existingDuration <= 0)
            {
                worldContext.engineController.UIController.removeUIElement(drawOrder, this);
            }
        }
    }

    /*
     * ========================================
     * Evolution scene UI
     * ========================================
    */
    public class EvolutionStarBackground : UIElement
    {

        public double mouseMovementCoefficient;


        int startXOffset = 1920;
        int startYOffset = 1080;

        int sourceY = 0;
        int sourceX = 0;

        public EvolutionStarBackground()
        {
            spriteSheetID = (int)spriteSheetIDs.evolutionBackground;
            sourceRectangle = new Rectangle(0, 0, 960, 540);
            drawRectangle = new Rectangle(0, 0, 1920, 1080);
            alignment = UIAlignOffset.TopLeft;
            positionType = Position.Absolute;
            scaleType = Scale.Relative;
            scene = Scene.Evolution;
            isUIElementActive = true;


            mouseMovementCoefficient = 1;
        }

        public void setSourceLocation(int x, int y)
        {

            sourceY = y;
            sourceX = x;
        }

        public void zeroStartingOffset()
        {
            startXOffset = 0;
            startYOffset = 0;
        }

        public override void updateElement(double elapsedTime, Game1 game)
        {
            if (Mouse.GetState().X >= 0 && Mouse.GetState().X < game.GraphicsDevice.Viewport.Width)
            {
                sourceRectangle.X = sourceX + (int)(startXOffset * mouseMovementCoefficient) - (int)(mouseMovementCoefficient * Mouse.GetState().X);
            }

            if (Mouse.GetState().Y >= 0 && Mouse.GetState().Y < game.GraphicsDevice.Viewport.Height)
            {
                sourceRectangle.Y = sourceY + (int)(startYOffset * mouseMovementCoefficient) - (int)(mouseMovementCoefficient * Mouse.GetState().Y);
            }
        }
    }

    public class EvolutionButton : InteractiveUIElement
    {
        double mouseMovementCoefficient = 0.027;

        const int iconWidth = 15;

        const int heightBetweenLayers = 75;

        int defaultX;
        int defaultY;

        const int heightOffset = 200;

        public Evolution ownerEvolution;

        bool isClicked = false;
        public EvolutionButton(Evolution owner)
        {
            spriteSheetID = (int)spriteSheetIDs.evolutionIcons;
            sourceRectangle = new Rectangle(0, owner.iconSourceY, 15, 15);
            //Adjust the draw location based on the location within the tree
            drawRectangle = new Rectangle(50, 50, 45, 45);
            alignment = UIAlignOffset.TopLeft;
            positionType = Position.Relative;
            scaleType = Scale.Relative;
            scene = Scene.Evolution;
            isUIElementActive = true;

            ownerEvolution = owner;
        }

        public void setSourceLocation(int x, int y)
        {
            sourceRectangle.X = x;
            sourceRectangle.Y = y;
        }

        public void setLocationFromTree()
        {
            //Set the x and y location on screen based on the tree layer
            int evolutionsInLayer = ownerEvolution.tree.evolutionTree[ownerEvolution.treeLayer].evolutionLayer.Count;
            defaultX = (ownerEvolution.indexWithinLayer + 1) * (defaultScreenWidth / (evolutionsInLayer + 1));
            defaultY = defaultScreenHeight - ((ownerEvolution.treeLayer + 1) * heightBetweenLayers + heightOffset);
        }

        public override void updateElement(double elapsedTime, Game1 game)
        {
            if (Mouse.GetState().X >= 0 && Mouse.GetState().X < game.GraphicsDevice.Viewport.Width)
            {
                drawRectangle.X = defaultX + (int)(mouseMovementCoefficient * Mouse.GetState().X);
            }

            if (Mouse.GetState().Y >= 0 && Mouse.GetState().Y < game.GraphicsDevice.Viewport.Height)
            {
                drawRectangle.Y = defaultY + (int)(mouseMovementCoefficient * Mouse.GetState().Y);
            }

            if (ownerEvolution.isEvolutionActive)
            {
                sourceRectangle.X = 3 * iconWidth;
            }
            else if (!ownerEvolution.canBeActivated)
            {
                if (isClicked)
                {
                    sourceRectangle.X = iconWidth;
                }
                else
                {
                    sourceRectangle.X = 0;
                }
            }
            else
            {
                sourceRectangle.X = 2 * iconWidth;
            }

            isClicked = false;
        }

        public override void onLeftClick(Game1 game)
        {
            isClicked = true;
            ownerEvolution.requestActivation();
        }
    }

    public class EvolutionDependencyLine : UILine
    {
        EvolutionButton evolution;
        EvolutionButton dependencyEvolution;
        public EvolutionDependencyLine(EvolutionButton evolution, EvolutionButton dependency, Vector2 point1, Vector2 point2) : base(point1, point2)
        {
            this.evolution = evolution;
            dependencyEvolution = dependency;

            scene = Scene.Evolution;

            point1 = new Vector2(evolution.drawRectangle.X + evolution.drawRectangle.Width / 2, evolution.drawRectangle.Y + evolution.drawRectangle.Height);
            point2 = new Vector2(dependencyEvolution.drawRectangle.X + dependencyEvolution.drawRectangle.Width / 2, dependencyEvolution.drawRectangle.Y + dependencyEvolution.drawRectangle.Height);

        }

        public override void updateLine()
        {

            point1 = new Vector2(evolution.drawRectangle.X + evolution.drawRectangle.Width / 2, evolution.drawRectangle.Y + evolution.drawRectangle.Height);

            point2 = new Vector2(dependencyEvolution.drawRectangle.X + dependencyEvolution.drawRectangle.Width / 2, dependencyEvolution.drawRectangle.Y);

            drawRectangle.X = (int)point1.X;
            drawRectangle.Y = (int)point1.Y;


            if (dependencyEvolution.ownerEvolution.isEvolutionActive)
            {
                drawColor = Color.White;
            }
            else
            {
                drawColor = new Color(50, 50, 50);
            }
            base.updateLine();
        }
    }

    public class ExperienceStringCharacter : UIElement
    {
        const int characterSizeInPixels = 6;

        double mouseMovementCoefficient = 0.027;

        int defaultX;
        int defaultY;
        public ExperienceStringCharacter(int character, int x, int y)
        {
            spriteSheetID = (int)spriteSheetIDs.evolutionCounterCharacters;
            sourceRectangle = new Rectangle(character * characterSizeInPixels, 0, 5, 7);
            drawRectangle = new Rectangle(x, y, 10, 14);
            defaultX = x;
            defaultY = y;
            scene = Scene.Evolution;
            alignment = UIAlignOffset.TopLeft;
            scaleType = Scale.Relative;
            positionType = Position.Relative;

            isUIElementActive = true;


        }

        public override void updateElement(double elapsedTime, Game1 game)
        {
            if (Mouse.GetState().X >= 0 && Mouse.GetState().X < game.GraphicsDevice.Viewport.Width)
            {
                drawRectangle.X = defaultX + (int)(mouseMovementCoefficient * Mouse.GetState().X);
            }

            if (Mouse.GetState().Y >= 0 && Mouse.GetState().Y < game.GraphicsDevice.Viewport.Height)
            {
                drawRectangle.Y = defaultY + (int)(mouseMovementCoefficient * Mouse.GetState().Y);
            }
        }
    }

    public class ExperienceCounter
    {
        public List<ExperienceStringCharacter> stringCharacters = new List<ExperienceStringCharacter>();
        const int stringDrawLayer = 14;
        public WorldContext worldContext;

        public string stringNumber;

        int x;
        int y;

        const int characterSizeInPixels = 10;
        public ExperienceCounter(WorldContext wc, int x, int y)
        {
            worldContext = wc;
            this.x = x;
            this.y = y;
        }
        public void updateString(string newString)
        {
            while (stringCharacters.Count > 0)
            {
                worldContext.engineController.UIController.removeUIElement(stringDrawLayer, stringCharacters[0]);
                stringCharacters.RemoveAt(0);
            }


            stringNumber = newString;

            //for each character of the string:
            for (int i = 0; i < newString.Length; i++)
            {
                string character = newString[i].ToString();
                ExperienceStringCharacter esc = new ExperienceStringCharacter(Convert.ToInt32(character), x + i * characterSizeInPixels, y);
                stringCharacters.Add(esc);
                worldContext.engineController.UIController.addUIElement(stringDrawLayer, esc);
            }
        }
    }

    /*
     * ========================================
     * Crafting UI
     * ========================================
    */
    public class CraftItemButton : InteractiveUIElement
    {
        Player owner;
        Item craftedItem;
        CraftingRecipe recipe;

        public CraftItemBackground background;

        const int drawLayer = 15;

        public int x;
        public int y;

        public CraftItemButton(CraftingRecipe recipe)
        {
            this.recipe = recipe;
            setItem(recipe.recipeOutput);
            owner = recipe.manager.worldContext.player;

            recipe.manager.worldContext.engineController.UIController.addUIElement(drawLayer, this);

            scene = Scene.Game;
            positionType = Position.Absolute;
            scaleType = Scale.Absolute;

            maxClickCooldown = 0.25f;


            background = new CraftItemBackground();
            recipe.manager.worldContext.engineController.UIController.addUIElement(drawLayer - 1, background);

        }
        public void setItem(Item item)
        {
            if (item != null)
            {
                this.craftedItem = item;
                if (item.currentStackSize <= 1) { buttonText = null; }
                spriteSheetID = item.spriteSheetID;
                sourceRectangle = item.sourceRectangle;
                int offsetWidth = item.drawRectangle.Width;
                int offsetHeight = item.drawRectangle.Height;

                //If the sprite is the exact same size, don't offset it by anything
                //If the sprite is smaller, offset it by half - half the width
                drawRectangle.Width = item.drawRectangle.Width;
                drawRectangle.Height = item.drawRectangle.Height;
                x = ((64 - offsetWidth) / 2);
                y = ((64 - offsetHeight) / 2);
                textLocation = new Vector2(offsetWidth + ((64 - offsetWidth) / 2), offsetWidth + ((64 - offsetHeight) / 2));
            }
            else
            {
                this.craftedItem = null;
                sourceRectangle = new Rectangle(0, 0, 0, 0);
                drawRectangle = new Rectangle(0, 0, 64, 64);
            }
        }

        public override void updateElement(double elapsedTime, Game1 game)
        {
            isUIElementActive = recipe.canBeCrafted && recipe.manager.showCraftingSystem;
            background.drawRectangle.X = drawRectangle.X - x;
            background.drawRectangle.Y = drawRectangle.Y - y;
            background.isUIElementActive = isUIElementActive;

            base.updateElement(elapsedTime, game);
        }
        public override void onLeftClick(Game1 game)
        {
            clickCooldown = maxClickCooldown;

            if (recipe.canBeCrafted)
            {
                Item floatingItem = owner.selectedItem.item;
                bool couldCombineItems = false;
                owner.selectedItem.clickedOnAUIElement = true;
                if (floatingItem != null && craftedItem != null)
                {
                    //Add some logic in here for combining items onto a stack when crafting. Shouldn't be super hard to do
                }
                else
                {
                    owner.selectedItem.setItem(craftedItem.itemCopy(craftedItem.currentStackSize));
                    recipe.itemWasCrafted();
                }
            }
        }
    }

    public class CraftItemBackground : UIElement
    {
        public CraftItemBackground()
        {
            scene = Scene.Game;
            positionType = Position.Absolute;
            scaleType = Scale.Absolute;
            spriteSheetID = (int)spriteSheetIDs.inventoryUI;
            sourceRectangle = new Rectangle(0, 32, 32, 33);
            drawRectangle = new Rectangle(0, 0, 64, 66);
        }
    }

    /*
     * ========================================
     * Housing UI
     * ========================================
    */

    public class HouseUI : UIElement {
        double baseX;
        double baseY;
        public HouseUI(double x, double y) {
            scene = Scene.Game;
            positionType = Position.BlockSpace;
            scaleType = Scale.Absolute;
            alignment = UIAlignOffset.TopLeft;

            spriteSheetID = (int)spriteSheetIDs.houseUI;
            drawRectangle = new Rectangle((int)x,(int)y,64,64);
            sourceRectangle = new Rectangle(0,0,32,32);
            baseX = x;
            baseY = y;
            
        }
    }

    /*
     * ========================================
     * NPC Parent Dialogue UI
     * ========================================
    */

    public class DialogueBox {

        protected DialogueBackground background;
        protected CloseDialogueButton closeDialogueButton;
        protected StringRenderer dialogue;
        protected WorldContext wc;
        public NPC npc;

        public string dialogueString;

        public bool isDialogueOpen = false;

        public const int drawLayer = 20;
        int dialogueOffsetX = 25;
        int dialogueOffsetY = 15;

        public DialogueBox(WorldContext wc, NPC npc) {
            background = new DialogueBackground();
            closeDialogueButton = new CloseDialogueButton(this, wc);
            dialogue = new StringRenderer(Scene.Game, UIAlignOffset.TopLeft, 15, false, drawLayer);
            dialogue.positionType = Position.WorldSpace;

            this.wc = wc;
            this.npc = npc;
            dialogue.setWorldContext(wc);
            wc.engineController.UIController.addUIElement(drawLayer - 1, background);
            wc.engineController.UIController.addUIElement(drawLayer, closeDialogueButton);
        }

        public virtual void closeDialogue() {
            dialogue.hideString();
            background.onDialogueClose();
            closeDialogueButton.onDialogueClose();

            isDialogueOpen = false;
        }

        public virtual void openDialogue(string dialogue) {
            dialogueString = dialogue;
            this.dialogue.setString(dialogue);
            this.dialogue.updateLocation((int)npc.x + dialogueOffsetX, (int)npc.y + dialogueOffsetY);

            background.onDialogueOpen((int)npc.x, (int)npc.y);
            closeDialogueButton.onDialogueOpen((int)npc.x + background.drawRectangle.Width, (int)npc.y + background.drawRectangle.Height);

            isDialogueOpen = true;
        }
    }

    public class DialogueBackground : UIElement
    {
        
        public DialogueBackground() {
            spriteSheetID = (int)spriteSheetIDs.dialogue;
            sourceRectangle = new Rectangle(0,0,256,88);
            drawRectangle = new Rectangle(0,0,512,196);
            positionType = Position.WorldSpace;
            scene = Scene.Game;
            isUIElementActive = false;
        }

        public void onDialogueOpen(int x, int y) {
            drawRectangle.X = x;
            drawRectangle.Y = y;
            isUIElementActive = true;
        }

        public void onDialogueClose() {
            isUIElementActive = false;
        }
    }

    public class CloseDialogueButton : InteractiveUIElement {
        public StringRenderer closeText;
        public DialogueBox owner;
        public int xOffset = 50;
        public int yOffset = 35;
        public CloseDialogueButton(DialogueBox owner, WorldContext wc) {

            scene = Scene.Game;
            spriteSheetID = (int)spriteSheetIDs.weapons;
            sourceRectangle = new Rectangle(0,0,0,0);
            drawRectangle = new Rectangle(0,0,50,25);
            positionType = Position.WorldSpace;

            closeText = new StringRenderer(Scene.Game, UIAlignOffset.TopLeft, 11, false, DialogueBox.drawLayer);
            
            closeText.positionType = Position.WorldSpace;
            
            closeText.setWorldContext(wc);

            closeText.setString("Close");
            closeText.hideString();


            this.owner = owner;
        }

        public void onDialogueOpen(int x, int y) {
            drawRectangle.X = x - xOffset;
            drawRectangle.Y = y - yOffset;
            isUIElementActive = true;
            closeText.showString();
            closeText.updateLocation((int)drawRectangle.X, (int)drawRectangle.Y);
        }

        public void onDialogueClose() {
            closeText.hideString();
            isUIElementActive = false;
        }

        public override void onLeftClick(Game1 game)
        {
            this.owner.npc.dialogueButtonPress("close");
            base.onLeftClick(game);
            
        }

    }


    /*========================================
     * NPC Dialogue UI
     * ========================================
    */

    public class GuideDialogueBox : DialogueBox {
        HelpDialogueButton helpButton;
        public GuideDialogueBox(WorldContext wc, NPC npc) : base(wc, npc) {
            helpButton = new HelpDialogueButton(this, wc);
            wc.engineController.UIController.addUIElement(drawLayer, helpButton);
        }

        public override void openDialogue(string dialogue)
        {
            helpButton.onDialogueOpen((int)npc.x + background.drawRectangle.Width, (int)npc.y + background.drawRectangle.Height);
            base.openDialogue(dialogue);
        }

        public override void closeDialogue()
        {
            helpButton.onDialogueClose();
            base.closeDialogue();
        }
    }

    public class HelpDialogueButton : InteractiveUIElement {
        public StringRenderer helpText;
        public DialogueBox owner;
        public int xOffset = 100;
        public int yOffset = 35;
        public HelpDialogueButton(DialogueBox owner, WorldContext wc)
        {

            scene = Scene.Game;
            spriteSheetID = (int)spriteSheetIDs.weapons;
            sourceRectangle = new Rectangle(0, 0, 0, 0);
            drawRectangle = new Rectangle(0, 0, 50, 25);
            positionType = Position.WorldSpace;

            helpText = new StringRenderer(Scene.Game, UIAlignOffset.TopLeft, 11, false, DialogueBox.drawLayer);

            helpText.positionType = Position.WorldSpace;

            helpText.setWorldContext(wc);

            helpText.setString("Help");
            helpText.hideString();

            maxClickCooldown = 0.4f;

            this.owner = owner;
        }

        public void onDialogueOpen(int x, int y)
        {
            drawRectangle.X = x - xOffset;
            drawRectangle.Y = y - yOffset;
            isUIElementActive = true;
            helpText.showString();
            helpText.updateLocation((int)drawRectangle.X, (int)drawRectangle.Y);
        }

        public void onDialogueClose() {
            isUIElementActive = false;
            helpText.hideString();
        }

        public override void onLeftClick(Game1 game)
        {
            this.owner.npc.dialogueButtonPress("help");
            base.onLeftClick(game);

        }
    }
}

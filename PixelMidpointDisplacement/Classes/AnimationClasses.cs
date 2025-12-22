using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;


using Vector2 = Microsoft.Xna.Framework.Vector2;


namespace PixelMidpointDisplacement {
    public class Animator
    {
        public double duration;
        public double elapsedTime;
        public double maxDuration;
        public (double xPos, double yPos, double rotation) initialPosition;
        public (double xPos, double yPos, double rotation) currentPosition;
        public (double xPos, double yPos, double rotation) finalPosition;

        public Item owner;

        public (double xPos, double yPos, double rotation) currentChange;
        double constantRotationOffset;



        public AnimationController animationController;

        public Animator(AnimationController ac, Item owner, double duration, (double xPos, double yPos, double rotation) initialPosition, (double xPos, double yPos, double rotation) finalPosition, double constantRotationOffset, Vector2 constantPositionOffset)
        {
            animationController = ac;
            this.owner = owner;

            this.duration = 0;
            maxDuration = duration;
            initialPosition.rotation += constantRotationOffset;
            finalPosition.rotation += constantRotationOffset;

            initialPosition.xPos += constantPositionOffset.X;
            finalPosition.xPos += constantPositionOffset.X;

            initialPosition.yPos += constantPositionOffset.Y;
            finalPosition.yPos += constantPositionOffset.Y;

            this.constantRotationOffset = constantRotationOffset;


            this.initialPosition = initialPosition;
            currentPosition = initialPosition;
            this.finalPosition = finalPosition;

        }

        public void tick(double elapsedTime)
        {

            duration += elapsedTime;
            this.elapsedTime = elapsedTime;
            if (duration >= maxDuration)
            {
                animationController.removeAnimator(this);
                owner.animationFinished();
            }

            currentChange.xPos = linearInterpolation(initialPosition.xPos, finalPosition.xPos);
            currentChange.yPos = linearInterpolation(initialPosition.yPos, finalPosition.yPos);
            currentChange.rotation = linearInterpolation(initialPosition.rotation, finalPosition.rotation);

            currentPosition = (currentPosition.xPos + currentChange.xPos, currentPosition.yPos + currentChange.yPos, currentPosition.rotation + currentChange.rotation);
        }

        public double linearInterpolation(double initialValue, double finalValue)
        {
            double difference = finalValue - initialValue;
            double differencePerSecond = difference / maxDuration;
            double linearlyInterpolatedValue = differencePerSecond * elapsedTime;// + initialValue; //Altered to calculate the change, then do the addition of the initial value when defining the current position. This allows for the change in a frame to be calculated for later efficiency purposes

            return linearlyInterpolatedValue;
        }

    }

    public class SpriteAnimator
    {
        public Texture2D spriteSheet; //The sprite sheet to take pictures from
        public Vector2 sourceOffset; //The initial offset to shift over all the draws
        double maxDuration; //The entire duration of the animation
        double duration; //The current duration
        Vector2 frameOffset; //The offset each frame. The y value will be the offset for different animations (from the yOffset from the dictionary)
        Vector2 sourceDimensions; //The dimensions of the source
        int frame;
        public Rectangle sourceRect; //The rectangle to draw from as the source rect. This takes the sourceDimensions, and the frameOffset * frame to get the location to draw from.
        public Dictionary<String, (int frameCount, int yOffset)> animationDictionary; //The string indicates the animation, and the int value indicates the y offset (an index value) to get to said animation
        int currentAnimationFrameCount;
        int currentAnimationYOffset;
        double currentDurationPerFrame;

        Rectangle animationlessSourceRectangle; //The source rectangle when no animation is being played.
        public bool isAnimationActive;

        public AnimationController animationController;
        public Entity owner; //Substitute with something else later on.

        public SpriteAnimator(AnimationController animationController, Vector2 constantOffset, Vector2 frameOffset, Vector2 sourceDimensions, Rectangle animationlessSourceRect, Entity owner)
        {
            this.owner = owner;
            this.spriteSheet = owner.spriteSheet;
            this.animationController = animationController;
            this.sourceOffset = constantOffset;
            this.frameOffset = frameOffset;

            this.sourceDimensions = sourceDimensions;
            animationlessSourceRectangle = animationlessSourceRect;
            sourceRect = animationlessSourceRectangle;
        }

        public void startAnimation(double duration, string animation)
        {
            isAnimationActive = true;
            currentAnimationFrameCount = animationDictionary[animation].frameCount;
            currentAnimationYOffset = animationDictionary[animation].yOffset;
            currentDurationPerFrame = maxDuration / currentAnimationFrameCount;
            this.maxDuration = duration;
            this.duration = 0;
            frame = 0;
            animationController.addSpriteAnimator(this);
        }

        public void tickAnimation(double elapsedTime)
        {
            duration += elapsedTime;
            if (duration >= maxDuration || !isAnimationActive)
            {
                isAnimationActive = false;
                currentAnimationFrameCount = 0;
                currentAnimationYOffset = 0;
                currentDurationPerFrame = 0;
                frame = 0;
                sourceRect = animationlessSourceRectangle;
                animationController.removeSpriteAnimator(this);

                return;
            }

            frame = (int)Math.Floor(duration / currentDurationPerFrame);

            sourceRect = new Rectangle((int)(frame * frameOffset.X), (int)(currentAnimationYOffset * frameOffset.Y), (int)sourceDimensions.X, (int)sourceDimensions.Y);

        }
    }
}
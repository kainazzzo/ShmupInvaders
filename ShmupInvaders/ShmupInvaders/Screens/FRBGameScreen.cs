
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Debugging;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Gum.Animation;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Math.Splines;

using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;
using FlatRedBall.Localization;
using Microsoft.Xna.Framework;
using ShmupInvaders.Entities;
using ShmupInvaders.Factories;
using ShmupInvaders.GumRuntimes;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;

namespace ShmupInvaders.Screens
{
	public partial class FRBGameScreen
	{
	    private I1DInput playerShipInput;
	    private IPressableInput playerFireInput;

	    private AxisAlignedRectangle cursor;


		void CustomInitialize()
		{
		    cursor = new AxisAlignedRectangle(10, 10);
		    SpriteManager.AddPositionedObject(cursor);
		    cursor.Visible = false;


		    float width = ShipsPerRow*ColumnSpacing;
            float height = Rows*RowSpacing;
		    
		    var currentY = 0;

		    for (int row = 0; row < Rows; row++)
		    {
                var currentX = 0;
                for (int shipCount = 0; shipCount < ShipsPerRow; shipCount++)
		        {
		            var ship = ShipEntityFactory.CreateNew();
                    
		            ship.AttachTo(ShipContainerInstance, false);

		            ship.RelativeX = currentX - width/2.0f + ColumnSpacing/2.0f;
		            ship.RelativeY = currentY - height/2.0f + RowSpacing/2.0f;
		            currentX += ColumnSpacing;
		        }

		        currentY += RowSpacing;
		    }

            this.ShipContainerInstance.AxisAlignedRectangleInstance.Width = width;
            this.ShipContainerInstance.AxisAlignedRectangleInstance.Height = height;

            this.ShipContainerInstance.XVelocity = StartingXVelocity;

		    
            InitializeInput();
		}

	    private void InitializeInput()
	    {
	        playerShipInput = InputManager.Keyboard.Get1DInput(MoveLeftKey, MoveRightKey);
	        playerFireInput = InputManager.Keyboard.GetKey(FireBulletKey);
	    }

	    void CustomActivity(bool firstTimeCalled)
		{
            cursor.X = GuiManager.Cursor.WorldXAt(0);
            cursor.Y = GuiManager.Cursor.WorldYAt(0);

		    cursor.Color = Color.White;

		    if (this.ShipContainerInstance.CollideAgainstBounce(this.LeftBoundary, 0, 1, 1) ||
		        this.ShipContainerInstance.CollideAgainstBounce(this.RightBoundary, 0, 1, 1))
		    {
		        this.ShipContainerInstance.Y -= StepDownPixels;
		        this.ShipContainerInstance.XVelocity *= StepDownSpeedMultiplier;
		    }

		    HandleInput();
	        HandleCollisions();
	        DestroyBullets();
		}

	    private void DestroyBullets()
	    {
	        foreach (var playerBullet in PlayerBulletList)
	        {
	            if (playerBullet.Y > RightBoundary.Top)
	            {
	                playerBullet.Destroy();
	            }
	        }
	    }

	    private void HandleCollisions()
	    {
	        PlayerShipInstance.CollideAgainstMove(LeftBoundary, 0, 1);
	        PlayerShipInstance.CollideAgainstMove(RightBoundary, 0, 1);

	        foreach (var playerBullet in PlayerBulletList)
	        {
	            if (playerBullet.CollideAgainst(ShipContainerInstance))
	            {
	                foreach (var shipEntity in ShipEntityList)
	                {
	                    if (playerBullet.CollideAgainst(shipEntity))
	                    {
	                        shipEntity.Destroy();
                            playerBullet.Destroy();
	                        recalculateContainerWidth();
	                    }
	                }
	            }
	        }
	    }

	    private void recalculateContainerWidth()
	    {
	        if (ShipEntityList.Count > 0)
	        {
	            var minX = ShipEntityList.Min(s => s.RelativeX);
	            var maxX = ShipEntityList.Max(s => s.RelativeX);

	            var width = maxX - minX;
	            width += ColumnSpacing;

	            ShipContainerInstance.AxisAlignedRectangleInstance.Width = width;
	            ShipContainerInstance.AxisAlignedRectangleInstance.RelativeX = minX + width/2f - ColumnSpacing/2.0f;
	        }
	    }

	    private void HandleInput()
	    {
	        PlayerShipInstance.XVelocity = playerShipInput.Value*PlayerShipSpeed;

	        if (PlayerShipInstance.XVelocity < 0)
	        {
	            PlayerShipInstance.CurrentFlyState = PlayerShip.Fly.Left;
	        }
            else if (PlayerShipInstance.XVelocity > 0)
            {
                PlayerShipInstance.CurrentFlyState = PlayerShip.Fly.Right;
            }
            else
	        {
	          PlayerShipInstance.CurrentFlyState = PlayerShip.Fly.Straight;
	        }

	        if (playerFireInput.WasJustPressed && PlayerBulletList.Count < MaxBullets)
	        {
	            var bullet = PlayerBulletFactory.CreateNew();
	            bullet.Position = PlayerShipInstance.Position;
	            bullet.Y += 22;
	            bullet.YVelocity = PlayerBulletSpeed;
            }
	    }

	    void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}

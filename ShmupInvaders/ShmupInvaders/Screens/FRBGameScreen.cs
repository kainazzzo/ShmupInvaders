
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
using ShmupInvaders.Factories;
using ShmupInvaders.GumRuntimes;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;

namespace ShmupInvaders.Screens
{
	public partial class FRBGameScreen
	{
	    private AxisAlignedRectangle cursor;
		void CustomInitialize()
		{
		    cursor = new AxisAlignedRectangle(10, 10);
		    SpriteManager.AddPositionedObject(cursor);
		    cursor.Visible = false;


		    var width = ShipsPerRow*ColumnSpacing;
		    var height = Rows*RowSpacing;
		    
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
        }

		void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}

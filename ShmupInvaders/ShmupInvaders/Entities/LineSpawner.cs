#region Usings

using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Debugging;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;

using FlatRedBall.Math.Geometry;
using FlatRedBall.Math.Splines;
using FlatRedBall.Math.Statistics;
using ShmupInvaders.Factories;
using BitmapFont = FlatRedBall.Graphics.BitmapFont;
using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;

#endif
#endregion

namespace ShmupInvaders.Entities
{
	public partial class LineSpawner
	{

	    public float SpeedExponent { get; set; }
	    private float _maxLines;

	    /// <summary>
        /// Initialization logic which is execute only one time for this Entity (unless the Entity is pooled).
        /// This method is called when the Entity is added to managers. Entities which are instantiated but not
        /// added to managers will not have this method called.
        /// </summary>
		private void CustomInitialize()
		{
            ColorLineFactory.Initialize(this.ColorLineList, ContentManagerName);
		}

	    public void FastSpeed(bool enable)
	    {
	        if ((Math.Abs(SpeedExponent - FastExponent) < .0001f) != enable)
	        {
	            for (int i = ColorLineList.Count - 1; i >= 0 ; i--)
	            {
	                ColorLineList[i].Destroy();
	            }
	        }
            if (enable)
            {
                SpeedExponent = FastExponent;
                _maxLines = FastMaxLines;
            }
            else
            {
                SpeedExponent = NormalExponent;
                _maxLines = NormalMaxLines;
            }
        }
		private void CustomActivity()
		{
            CreateLines();

		    for (int l = 0; l < ColorLineList.Count; l++)
		    {
		        var line = ColorLineList[l];

		        if (line.Y < -300)
		        {
		            line.Destroy();
		        }
		    }

		}

	    private void CreateLines()
	    {
	        while (ColorLineList.Count < _maxLines)
	        {
	            var line = ColorLineFactory.CreateNew();

	            line.X = FlatRedBallServices.Random.Next(-400, 400);
	            line.Y = 300;


	            SetRandomVelocityAndHeight(line);
	        }
	    }

	    private void SetRandomVelocityAndHeight(ColorLine line)
	    {
	        var zeroToOne = FlatRedBallServices.Random.NextFloat(1.0f);
	        line.YVelocity = -(float) Math.Pow(zeroToOne, SpeedExponent)*Speed;

	        line.SpriteInstance.Height = Math.Abs(line.YVelocity*VelocityToHeight);
	    }

	    private void CustomDestroy()
		{


		}

        private static void CustomLoadStaticContent(string contentManagerName)
        {


        }
	}
}

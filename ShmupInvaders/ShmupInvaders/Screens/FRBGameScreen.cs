
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Debugging;
using FlatRedBall.Glue.StateInterpolation;
using FlatRedBall.Graphics;
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
using StateInterpolationPlugin;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;

namespace ShmupInvaders.Screens
{
	public partial class FRBGameScreen
	{
	    private I1DInput _playerShipInput;
	    private IPressableInput _playerFireInput;
	    private Vector3 _initialShipContainerPosition;
	    private int _wave = 1;
	    private bool _gameOver = false;

		void CustomInitialize()
		{
		    _initialShipContainerPosition = ShipContainerInstance.Position;

		    InitializeShips();
		    InitializeInput();
		}

	    private void InitializeShips()
	    {
	        
	        float width = ShipsPerRow*ColumnSpacing;
	        float height = Rows*RowSpacing;

            ShipContainerInstance.Position = _initialShipContainerPosition;
	        ShipContainerInstance.Y -= _wave*this.StepDownPixels;
	        

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

	        ShipContainerInstance.XVelocity = StartingXVelocity;
	        
	        ShipContainerInstance.AxisAlignedRectangleInstance.Height = height;

            RecalculateContainerWidth();
        }

	    private void InitializeInput()
	    {
	        _playerShipInput = InputManager.Keyboard.Get1DInput(MoveLeftKey, MoveRightKey);
	        _playerFireInput = InputManager.Keyboard.GetKey(FireBulletKey);
	    }

	    void CustomActivity(bool firstTimeCalled)
		{
	        if (_gameOver == false)
	        {
	            if (this.ShipContainerInstance.CollideAgainstBounce(this.LeftBoundary, 0, 1, 1) ||
	                this.ShipContainerInstance.CollideAgainstBounce(this.RightBoundary, 0, 1, 1))
	            {

	                var currentXVelocity = ShipContainerInstance.XVelocity;
	                ShipContainerInstance.XVelocity = 0;

                    
                    

	                this.ShipContainerInstance.Tween("Y")
	                    .To(this.ShipContainerInstance.Y - StepDownPixels)
	                    .During(.5)
	                    .Using(InterpolationType.Bounce, Easing.Out).Ended += () =>
	                    {
	                        ShipContainerInstance.XVelocity = currentXVelocity*StepDownSpeedMultiplier;
	                    };

	                this.Call(ShakeScreen).After(.2);


	            }

	            HandleInput();
	            HandleCollisions();
	            DestroyBullets();
	        }
		}

        private void ShakeScreen()
        {
            var shakerX = new ShakeTweener
            {
                Amplitude = 20f,
                Duration = .275f
            };

            var shakerY = new ShakeTweener
            {
                Amplitude = 10f,
                MaxAmplitude = 10f,
                Duration = .275f
            };



            TweenerManager.Self.Add(shakerY);


            
            shakerY.PositionChanged += position => Camera.Main.Position.Y = position;
        }

        private void GameOver()
	    {
	        _gameOver = true;
	        PlayerShipInstance.XVelocity = 0;
	        ShipContainerInstance.XVelocity = 0;
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
	        HandlePlayerCollision();

	        HandleBulletCollisions();
	    }

	    private void HandlePlayerCollision()
	    {
            // Stay in the screen
	        PlayerShipInstance.CollideAgainstMove(LeftBoundary, 0, 1);
	        PlayerShipInstance.CollideAgainstMove(RightBoundary, 0, 1);

	        foreach (var enemy in ShipEntityList)
	        {
	            if (enemy.CollideAgainst(PlayerShipInstance))
	            {
	                GameOver();
	            }
	        }
	    }

	    private void HandleBulletCollisions()
	    {
	        foreach (var playerBullet in PlayerBulletList)
	        {
	            if (!playerBullet.CollideAgainst(ShipContainerInstance)) continue;

	            foreach (var shipEntity in ShipEntityList)
	            {
	                if (playerBullet.CollideAgainst(shipEntity) && shipEntity.SpriteInstance.CurrentChainName != "Explosion")
	                {
                        playerBullet.Destroy();

                        shipEntity.SpriteInstance.ColorOperation = ColorOperation.Add;

	                    shipEntity.SpriteInstance.Red = 255f;
	                    shipEntity.SpriteInstance.Blue = 255f;
	                    shipEntity.SpriteInstance.Green = 255f;





	                    if (shipEntity.TotalHits++ < shipEntity.HitsToKill - 1)
	                    {
	                        this.Call(() => shipEntity.SpriteInstance.ColorOperation = ColorOperation.None)
	                            .After(TimeSpan.FromMilliseconds(10).TotalSeconds);
	                        continue;
	                    }
	                    else
	                    {
	                        shipEntity.SpriteInstance.ColorOperation = ColorOperation.None;
	                    }

	                    shipEntity.SpriteInstance.CurrentChainName = "Explosion";
	                    shipEntity.SpriteInstance.TextureScale = .6f;

	                    this.Call(() =>
	                    {
	                        shipEntity.Destroy();
	                        RecalculateContainerWidth();
	                    }).After(.55);
                        
	                    Score += shipEntity.PointValue;
	                    break;
	                }
	            }
	        }

	        if (ShipEntityList.Count == 0)
	        {
	            // All ships destroyed. Start new wave:
	            ++_wave;
                InitializeShips();
	        }
	    }

	    private void RecalculateContainerWidth()
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
	        PlayerShipInstance.XVelocity = _playerShipInput.Value*PlayerShipSpeed;

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

	        if (_playerFireInput.WasJustPressed && PlayerBulletList.Count < MaxBullets)
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

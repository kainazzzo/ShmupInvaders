
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
using FlatRedBall.Math.Statistics;
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
	    private bool _gameOver;

	    private static readonly string[] WaveColors = { "Purple", "Orange", "Green", "Blue" };
        private bool _newWave = false;

	    void CustomInitialize()
		{
		    _initialShipContainerPosition = ShipContainerInstance.Position;

		    InitializeInput();

	        LineSpawnerInstance.FastSpeed(true);

            MainGumScreenGlueInstance.FlyInAnimation.Play(this);
	        _newWave = true;
            
            this.Call(() =>
            {
                CurrentWave = GlobalContent.Waves[_wave.ToString()];

                LineSpawnerInstance.FastSpeed(false);
                _newWave = false;
                WaveIndicatorInstance.Visible = false;
            }).After(4);
        }

	    

	    private void InitializeInput()
	    {
	        _playerShipInput = InputManager.Keyboard.Get1DInput(MoveLeftKey, MoveRightKey);
	        _playerFireInput = InputManager.Keyboard.GetKey(FireBulletKey);
	    }

	    void CustomActivity(bool firstTimeCalled)
		{
	        if (_gameOver == false && _newWave == false)
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

	                //this.Call(ShakeScreen).After(.2);


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

                        FlashEnemyShip(shipEntity);


	                    if (shipEntity.TotalHits++ < shipEntity.HitsToKill - 1)
	                    {
	                        this.Call(() => shipEntity.SpriteInstance.ColorOperation = ColorOperation.None)
	                            .After(TimeSpan.FromMilliseconds(10).TotalSeconds);
                            
                            shipEntity.DamageInstance.Play();
	                        continue;
	                    }
	                    else
	                    {
	                        shipEntity.SpriteInstance.ColorOperation = ColorOperation.None;
	                    }

	                    shipEntity.SpriteInstance.CurrentChainName = "Explosion";
	                    shipEntity.SpriteInstance.TextureScale = .6f;
                        shipEntity.Detach();
                        shipEntity.ExplosionInstance.Play();

	                    this.Call(() =>
	                    {
	                        shipEntity.Destroy();
	                        RecalculateContainerWidth();
	                    }).After(.55);
                        
	                    //Score += shipEntity.PointValue;
	                    break;
	                }
	            }
	        }

	        if (ShipEntityList.Count == 0 && !_newWave)
	        {
	            _newWave = true;
	            // All ships destroyed. Start new wave:
	            LineSpawnerInstance.FastSpeed(true);

	            WaveDisplay = _wave + 1;
                PlayerShipInstance.Velocity = Vector3.Zero;
                PlayerShipInstance.CurrentFlyState = PlayerShip.Fly.Straight;

                PlayerShipInstance.Tween(nameof(PlayerShipInstance.X), 0f, 4.0f, InterpolationType.Linear, Easing.In);

                if (GlobalContent.Waves.ContainsKey((_wave + 1).ToString()))
                {
                    this.Call(() =>
                    {
                        ++_wave;
                        CurrentWave = GlobalContent.Waves[_wave.ToString()];
                        LineSpawnerInstance.FastSpeed(false);
                        WaveIndicatorInstance.Visible = false;
                        _newWave = false;
                        WaveIndicatorInstance.ApplyState("WaveState", "FlyInStart");
                    }).After(4.0);
                }
                else
                {
                    // YOU WIN!
                    WaveIndicatorInstance.TextInstanceText = "YOU ";
                    WaveIndicatorInstance.WaveTextInstanceText = "WIN!!!";
                }

                WaveIndicatorInstance.Visible = true;
                MainGumScreenGlueInstance.FlyInAnimation.Play(this);
            }
	    }

	    private static void FlashEnemyShip(ShipEntity shipEntity)
	    {
	        shipEntity.SpriteInstance.ColorOperation = ColorOperation.Add;

	        shipEntity.SpriteInstance.Red = 255f;
	        shipEntity.SpriteInstance.Blue = 255f;
	        shipEntity.SpriteInstance.Green = 255f;
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
                PlayerShipInstance.LaserInstance.Play();
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

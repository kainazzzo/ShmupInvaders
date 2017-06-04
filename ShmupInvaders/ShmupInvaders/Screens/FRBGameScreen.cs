
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
            GameOverText.Visible = false;
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

                    // Using TweenerHolder instead of PositionedObjectTweenerExtensionMethods.Tween, 
                    // because the latter function returns a TweenerHolder instead of a tweener, and I need the "Ended" event
                    new TweenerHolder()
                    {
                        Caller = ShipContainerInstance
                    }.Tween("Y", this.ShipContainerInstance.Y - StepDownPixels, .5f, InterpolationType.Bounce, Easing.Out).Ended += () =>
                    {
                        ShipContainerInstance.XVelocity = currentXVelocity * StepDownSpeedMultiplier;
                    };

                    //this.Call(ShakeScreen).After(.2);


                }

	            HandleInput();
	            HandleCollisions();
                FireEnemyBullets();
	            DestroyBullets();
	        }
		}

        private void FireEnemyBullets()
        {
            foreach (var ship in ShipEntityList)
            {
                if (ship.BulletCharged(PauseAdjustedCurrentTime))
                {
                    ship.FireBullet(PauseAdjustedCurrentTime);
                }
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

            var text = GameOverText.Text;

            GameOverText.Visible = true;
            GameOverText.Text = "";

            var step = TextTimeStep;

            for(var it = 1; it <= text.Length; ++it)
            {
                var newText = text.Substring(0, it);

                this.Call(() =>
                {
                    GameOverText.Text = newText;
                }).After(step * it);
            }
        }

        private void YouWin()
        {
            _gameOver = true;
            PlayerShipInstance.XVelocity = 0;
            ShipContainerInstance.XVelocity = 0;

            var text = WinScreenText.Text;

            WinScreenText.Visible = true;
            WinScreenText.Text = "";

            var step = TextTimeStep;

            for (var it = 1; it <= text.Length; ++it)
            {
                var newText = text.Substring(0, it);

                this.Call(() =>
                {
                    WinScreenText.Text = newText;
                }).After(step * it);
            }
        }

        private void DestroyBullets()
	    {
            for(var pbi = PlayerBulletList.Count - 1; pbi >= 0; pbi--)
	        {
                var playerBullet = PlayerBulletList[pbi];
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
            HandleRicochetCollisions();
        }

	    private void HandlePlayerCollision()
	    {
            // Stay in the screen
	        PlayerShipInstance.CollideAgainstMove(LeftBoundary, 0, 1);
	        PlayerShipInstance.CollideAgainstMove(RightBoundary, 0, 1);

            if (ShipContainerInstance.CollideAgainst(PlayerShipInstance))
            {
                GameOver();
            }

            if (ShipContainerInstance.AxisAlignedRectangleInstance.Bottom < LeftBoundary.Bottom)
            {
                GameOver();
            }
	    }

	    private void HandleBulletCollisions()
        {
            HandlePlayerBulletCollision();
            HandleEnemyBulletCollision();
        }

        private void HandleEnemyBulletCollision()
        {
            for (var ebi = EnemyBulletList.Count - 1; ebi >= 0; ebi--)
            {
                var enemyBullet = EnemyBulletList[ebi];
                if (enemyBullet.CollideAgainst(PlayerShipInstance))
                {
                    // You Lose!
                    GameOver();

                    enemyBullet.Destroy();
                }

                else if (enemyBullet.Y < RightBoundary.Bottom)
                {
                    enemyBullet.Destroy();
                }
            }
        }

        private void HandlePlayerBulletCollision()
        {
            for (var pbi = PlayerBulletList.Count - 1; pbi >= 0; pbi--)
            {
                var playerBullet = PlayerBulletList[pbi];

                for (var sei = ShipEntityList.Count - 1; sei >= 0; sei--)
                {
                    var shipEntity = ShipEntityList[sei];
                    if (HandleShipHit(shipEntity, playerBullet))
                    {
                        playerBullet.Destroy();
                        break;
                    }
                }

                for (var ebi = EnemyBulletList.Count - 1; ebi >= 0; ebi--)
                {
                    var enemyBullet = EnemyBulletList[ebi];
                    if (playerBullet.CollideAgainst(enemyBullet))
                    {
                        // Spawn ricochet
                        SpawnRicochet(enemyBullet);
                        enemyBullet.Destroy();
                        playerBullet.Destroy();
                    }
                }
            }

            if (ShipEntityList.Count == 0 && !_newWave)
            {
                for (var ebi = EnemyBulletList.Count - 1; ebi >= 0; ebi--)
                {
                    EnemyBulletList[ebi].Destroy();
                }

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

                    WaveIndicatorInstance.Visible = true;
                    MainGumScreenGlueInstance.FlyInAnimation.Play(this);
                }
                else
                {
                    // YOU WIN!
                    YouWin();
                }


            }
        }

        private void HandleRicochetCollisions()
        {
            for (var rbi = RicochetList.Count - 1; rbi >= 0; rbi--)
            {
                var ricochetBullet = RicochetList[rbi];
                for (var esi = ShipEntityList.Count - 1; esi >= 0; esi--)
                {
                    var enemyShip = ShipEntityList[esi];

                    if (HandleShipHit(enemyShip, ricochetBullet))
                    {
                        ricochetBullet.Destroy();
                        break;
                    }
                }

                for (var ebi = EnemyBulletList.Count - 1; ebi >= 0; ebi--)
                {
                    var enemyBullet = EnemyBulletList[ebi];
                    if (enemyBullet.CollideAgainst(ricochetBullet))
                    {
                        SpawnRicochet(enemyBullet);
                        ricochetBullet.Destroy();
                        enemyBullet.Destroy();
                        break;
                    }
                }
            }
        }

        private bool HandleShipHit(ShipEntity shipEntity, ICollidable bullet)
        {
            if (bullet.CollideAgainst(shipEntity) && shipEntity.SpriteInstance.CurrentChainName != "Explosion")
            {
                FlashEnemyShip(shipEntity);


                if (shipEntity.TotalHits++ < shipEntity.HitsToKill - 1)
                {
                    this.Call(() => shipEntity.SpriteInstance.ColorOperation = ColorOperation.None)
                        .After(TimeSpan.FromMilliseconds(10).TotalSeconds);

                    shipEntity.DamageInstance.Play();
                    return true;
                }
                else
                {
                    shipEntity.SpriteInstance.ColorOperation = ColorOperation.None;
                }

                shipEntity.SpriteInstance.CurrentChainName = "Explosion";
                shipEntity.SpriteInstance.TextureScale = .6f;
                shipEntity.NextBullet = PauseAdjustedCurrentTime + 1.0;
                shipEntity.Detach();
                shipEntity.ExplosionInstance.Play();

                this.Call(() =>
                {
                    shipEntity.Destroy();
                    RecalculateContainerWidth();
                }).After(.55);

                //Score += shipEntity.PointValue;
                return true;
            }
            return false;
        }

        private void SpawnRicochet(PositionedObject enemyBullet)
        {
            var up = RicochetFactory.CreateNew();
            var down = RicochetFactory.CreateNew();
            var left = RicochetFactory.CreateNew();
            var right = RicochetFactory.CreateNew();
            var upleft = RicochetFactory.CreateNew();
            var upright = RicochetFactory.CreateNew();
            var downright = RicochetFactory.CreateNew();
            var downleft = RicochetFactory.CreateNew();

            up.CurrentDirectionState = Ricochet.Direction.Up;
            down.CurrentDirectionState = Ricochet.Direction.Down;
            left.CurrentDirectionState = Ricochet.Direction.Left;
            right.CurrentDirectionState = Ricochet.Direction.Right;
            upleft.CurrentDirectionState = Ricochet.Direction.UpLeft;
            upright.CurrentDirectionState = Ricochet.Direction.UpRight;
            downright.CurrentDirectionState = Ricochet.Direction.DownRight;
            downleft.CurrentDirectionState = Ricochet.Direction.DownLeft;

            up.Position = down.Position = left.Position = right.Position 
                = upleft.Position = upright.Position = downright.Position = downleft.Position = enemyBullet.Position;

            up.YVelocity = 1 * RicochetSpeed;
            down.YVelocity = -1 * RicochetSpeed;
            left.XVelocity = -1 * RicochetSpeed;
            right.XVelocity = 1 * RicochetSpeed;

            upleft.XVelocity = left.XVelocity;
            upleft.YVelocity = up.YVelocity;
            upright.XVelocity = right.XVelocity;
            upright.YVelocity = up.YVelocity;
            downright.XVelocity = right.XVelocity;
            downright.YVelocity = down.YVelocity;
            downleft.XVelocity = left.XVelocity;
            downleft.YVelocity = down.YVelocity;

            this.Call(() => {
                up.Destroy();
                down.Destroy();
                left.Destroy();
                right.Destroy();
                upleft.Destroy();
                upright.Destroy();
                downright.Destroy();
                downleft.Destroy();
            }).After(RicochetTTL);
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

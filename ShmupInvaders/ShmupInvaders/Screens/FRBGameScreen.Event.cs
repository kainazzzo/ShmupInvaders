using System;
using ShmupInvaders.Entities;
using ShmupInvaders.Factories;
using FlatRedBall;

namespace ShmupInvaders.Screens
{
    public partial class FRBGameScreen
	{
     
        void OnAfterCurrentWaveSet (object sender, EventArgs e)
        {
            this.ShipEntityList.Clear();

            ShipContainerInstance.Position = _initialShipContainerPosition;

            float width = ShipsPerRow * ColumnSpacing;
            float height = Rows * RowSpacing;

            var currentY = 0;
            var currentX = 0;

            waveShots = 0;
            waveHits = 0;
            foreach (var shipName in CurrentWave.Ships)
            {
                var ship = ShipEntityFactory.CreateNew();

                SetShipState(shipName, ship);

                ship.AttachTo(ShipContainerInstance, false);

                ship.RelativeX = currentX - width / 2.0f + ColumnSpacing / 2.0f;
                ship.RelativeY = currentY - height / 2.0f + RowSpacing / 2.0f;
                currentX += ColumnSpacing;

                
                if (currentX >= ShipsPerRow * ColumnSpacing)
                {
                    currentX = 0;
                    currentY += RowSpacing;
                }

                if (currentY >= Rows * RowSpacing)
                {
                    break;
                }
            }


            ShipContainerInstance.XVelocity = StartingXVelocity;

            ShipContainerInstance.AxisAlignedRectangleInstance.Height = height;

            RecalculateContainerSize();
        }

        private void SetShipState(string shipName, ShipEntity ship)
        {
            switch (shipName)
            {
                case "OrangeShip":
                    ship.CurrentState = ShipEntity.VariableState.OrangeShip;
                    break;
                case "OrangeEye":
                    ship.CurrentState = ShipEntity.VariableState.OrangeEye;
                    break;
                case "OrangeTea":
                    ship.CurrentState = ShipEntity.VariableState.OrangeTea;
                    break;
                case "OrangeHorseshoe":
                    ship.CurrentState = ShipEntity.VariableState.OrangeHorseshoe;
                    break;
            }

            var shipType = GlobalContent.ShipType[shipName];

            ship.ShipType = shipType;
            ship.HitsToKill = shipType.Hits;
            ship.NextBullet = PauseAdjustedCurrentTime + FlatRedBallServices.Random.NextDouble() *
                (shipType.BulletFrequencyMax - shipType.BulletFrequencyMin) + shipType.BulletFrequencyMin;
        }
    }
}

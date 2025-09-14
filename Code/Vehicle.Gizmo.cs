namespace MSC;

public partial class Vehicle
{
	private SceneModel _frontLeftWheelGizmo;
	private SceneModel _frontRightWheelGizmo;
	private SceneModel _rearLeftWheelGizmo;
	private SceneModel _rearRightWheelGizmo;

	protected override void OnDestroy()
	{
		base.OnDestroy();

		DestroyWheelGizmos();
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if ( Game.IsPlaying )
		{
			DestroyWheelGizmos();
			return;
		}

		Gizmo.Transform = global::Transform.Zero;

		DrawAxleGizmo( FrontAxle, Color.Cyan );
		DrawWheelGizmo( FrontAxle, FrontAxle.WheelDataLeft, false, Color.Cyan );
		DrawWheelGizmo( FrontAxle, FrontAxle.WheelDataRight, true, Color.Cyan );

		DrawAxleGizmo( RearAxle, Color.Red );
		DrawWheelGizmo( RearAxle, RearAxle.WheelDataLeft, false, Color.Red );
		DrawWheelGizmo( RearAxle, RearAxle.WheelDataRight, true, Color.Red );

		var wsDownDirection = WorldTransform.NormalToWorld( Vector3.Down );

		if ( FrontAxle.WheelModel.IsValid() )
		{
			var (wsL, wsR) = GetWorldWheelPositions( FrontAxle );

			FrontAxle.WheelDataLeft.Compression = 0.0f;
			_frontLeftWheelGizmo ??= new SceneModel( Scene.SceneWorld, FrontAxle.WheelModel, global::Transform.Zero );
			_frontLeftWheelGizmo.Transform = CalculateWheelVisualTransform( wsL, wsDownDirection, FrontAxle, FrontAxle.WheelDataLeft, true );

			FrontAxle.WheelDataRight.Compression = 0.0f;
			_frontRightWheelGizmo ??= new SceneModel( Scene.SceneWorld, FrontAxle.WheelModel, global::Transform.Zero );
			_frontRightWheelGizmo.Transform = CalculateWheelVisualTransform( wsR, wsDownDirection, FrontAxle, FrontAxle.WheelDataRight, false );
		}

		if ( _frontLeftWheelGizmo.IsValid() && _frontRightWheelGizmo.IsValid() )
		{
			if ( FrontAxle.WheelModel != _frontLeftWheelGizmo.Model && FrontAxle.WheelModel != _frontRightWheelGizmo.Model )
			{
				_frontLeftWheelGizmo.Delete();
				_frontLeftWheelGizmo = null;

				_frontRightWheelGizmo.Delete();
				_frontRightWheelGizmo = null;
			}
		}

		if ( RearAxle.WheelModel.IsValid() )
		{
			var (wsL, wsR) = GetWorldWheelPositions( RearAxle );

			RearAxle.WheelDataLeft.Compression = 0.0f;
			_rearLeftWheelGizmo ??= new SceneModel( Scene.SceneWorld, RearAxle.WheelModel, global::Transform.Zero );
			_rearLeftWheelGizmo.Transform = CalculateWheelVisualTransform( wsL, wsDownDirection, RearAxle, RearAxle.WheelDataLeft, true );

			RearAxle.WheelDataRight.Compression = 0.0f;
			_rearRightWheelGizmo ??= new SceneModel( Scene.SceneWorld, RearAxle.WheelModel, global::Transform.Zero );
			_rearRightWheelGizmo.Transform = CalculateWheelVisualTransform( wsR, wsDownDirection, RearAxle, RearAxle.WheelDataRight, false );
		}

		if ( _rearLeftWheelGizmo.IsValid() && _rearRightWheelGizmo.IsValid() )
		{
			if ( RearAxle.WheelModel != _rearLeftWheelGizmo.Model && RearAxle.WheelModel != _rearRightWheelGizmo.Model )
			{
				_rearLeftWheelGizmo.Delete();
				_rearLeftWheelGizmo = null;

				_rearRightWheelGizmo.Delete();
				_rearRightWheelGizmo = null;
			}
		}
	}

	private void DrawAxleGizmo( Axle axle, Color color )
	{
		var (wsL, wsR) = GetWorldWheelPositions( axle );

		Gizmo.Draw.IgnoreDepth = true;
		Gizmo.Draw.Color = color;
		Gizmo.Draw.Line( wsL, wsR );
	}

	private void DrawWheelGizmo( Axle axle, Wheel wheel, bool isLeftWheel, Color color )
	{
		var localPos = new Vector3( axle.Offset.x, !isLeftWheel ? axle.Width * -0.5f : axle.Width * 0.5f, axle.Offset.y );
		var localPosRelaxed = localPos + Vector3.Down * axle.LengthRelaxed;

		var wsPos = WorldTransform.PointToWorld( localPos );
		var wsPosRelaxed = WorldTransform.PointToWorld( localPosRelaxed );

		Gizmo.Draw.IgnoreDepth = true;
		Gizmo.Draw.Color = wheel.IsGrounded ? color : color.WithAlpha( 0.3f );

		Gizmo.Draw.Line( wsPos, wsPosRelaxed );

		var localWheelRot = Rotation.FromAxis( Vector3.Up, wheel.YawInRadians );
		var wsWheelRot = WorldTransform.Rotation * localWheelRot;

		var wheelWidth = 0.085f;
		var wsAxleLeft = wsWheelRot * Vector3.Left;

		var wheelStart = wsPosRelaxed + wsAxleLeft * (wheelWidth * 0.5f);
		var wheelEnd = wsPosRelaxed - wsAxleLeft * (wheelWidth * 0.5f);

		Gizmo.Draw.LineCylinder( wheelStart, wheelEnd, axle.Radius, axle.Radius, 16 );
	}

	private void DestroyWheelGizmos()
	{
		if ( _frontLeftWheelGizmo.IsValid() )
		{
			_frontLeftWheelGizmo.Delete();
			_frontLeftWheelGizmo = null;
		}

		if ( _frontRightWheelGizmo.IsValid() )
		{
			_frontRightWheelGizmo.Delete();
			_frontRightWheelGizmo = null;
		}

		if ( _rearLeftWheelGizmo.IsValid() )
		{
			_rearLeftWheelGizmo.Delete();
			_rearLeftWheelGizmo = null;
		}

		if ( _rearRightWheelGizmo.IsValid() )
		{
			_rearRightWheelGizmo.Delete();
			_rearRightWheelGizmo = null;
		}
	}
}

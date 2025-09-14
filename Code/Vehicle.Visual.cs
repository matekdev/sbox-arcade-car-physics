namespace MSC;

// https://github.com/SergeyMakeev/ArcadeCarPhysics
public partial class Vehicle
{
	[Sync] private Transform FrontLeftWheelTransform { get; set; }
	private SceneObject FrontLeftWheelRenderer { get; set; }

	[Sync] private Transform FrontRightWheelTransform { get; set; }
	private SceneObject FrontRightWheelRenderer { get; set; }

	[Sync] private Transform RearLeftWheelTransform { get; set; }
	private SceneObject RearLeftWheelRenderer { get; set; }

	[Sync] private Transform RearRightWheelTransform { get; set; }
	private SceneObject RearRightWheelRenderer { get; set; }

	private void UpdateWheelVisuals()
	{
		if ( FrontLeftWheelRenderer.IsValid() )
			FrontLeftWheelRenderer.Transform = FrontLeftWheelTransform;

		if ( FrontRightWheelRenderer.IsValid() )
			FrontRightWheelRenderer.Transform = FrontRightWheelTransform;

		if ( RearLeftWheelRenderer.IsValid() )
			RearLeftWheelRenderer.Transform = RearLeftWheelTransform;

		if ( RearRightWheelRenderer.IsValid() )
			RearRightWheelRenderer.Transform = RearRightWheelTransform;
	}

	private void CalculateWheelVisuals()
	{
		if ( FrontAxle.WheelModel.IsValid() )
			(FrontLeftWheelTransform, FrontRightWheelTransform) = CalculateAxleVisual( FrontAxle );

		if ( RearAxle.WheelModel.IsValid() )
			(RearLeftWheelTransform, RearRightWheelTransform) = CalculateAxleVisual( RearAxle );
	}

	private (Transform left, Transform right) CalculateAxleVisual( Axle axle )
	{
		var wsDownDirection = WorldTransform.NormalToWorld( Vector3.Down );
		var (wsL, wsR) = GetWorldWheelPositions( axle );

		var leftTransform = CalculateWheelVisualTransform( wsL, wsDownDirection, axle, axle.WheelDataLeft, true );

		if ( !_isBraking )
			CalculateWheelRotationFromSpeed( axle, axle.WheelDataLeft, leftTransform.Position );

		var rightTransform = CalculateWheelVisualTransform( wsR, wsDownDirection, axle, axle.WheelDataRight, false );

		if ( !_isBraking )
			CalculateWheelRotationFromSpeed( axle, axle.WheelDataRight, rightTransform.Position );

		return (leftTransform, rightTransform);
	}

	private Transform CalculateWheelVisualTransform( Vector3 wsAttachPoint, Vector3 wsDownDirection, Axle axle, Wheel wheel, bool isLeftWheel )
	{
		var compressionFactor = MathX.Clamp( wheel.Compression, 0.0f, 1.0f );
		var extensionDistance = axle.LengthRelaxed * (1.0f - compressionFactor);
		var pos = wsAttachPoint + wsDownDirection * extensionDistance;

		var spinDirection = !isLeftWheel ? -1.0f : 1.0f;
		var spinAngle = wheel.VisualRotationInRadians.RadianToDegree() * spinDirection;

		var yawAngle = wheel.YawInRadians.RadianToDegree();

		if ( !isLeftWheel )
			yawAngle += 180f;

		var wheelRotation = Rotation.FromYaw( yawAngle ) * Rotation.FromPitch( spinAngle );
		var rot = WorldRotation * wheelRotation;

		return new Transform( pos, rot, axle.VisualScale );
	}

	private void CalculateWheelRotationFromSpeed( Axle axle, Wheel wheel, Vector3 wsWheelPos )
	{
		var wheelForward = (WorldRotation * Rotation.FromYaw( wheel.YawInRadians.RadianToDegree() )).Forward;

		var samplePos = wheel.IsGrounded ? wheel.TouchTrace.HitPosition : wsWheelPos;
		var forwardSpeed = Vector3.Dot( Rigidbody.GetVelocityAtPoint( samplePos ), wheelForward );

		var rotationRate = forwardSpeed / axle.Radius; // radians per second
		wheel.VisualRotationInRadians += rotationRate * Time.Delta;
	}
}

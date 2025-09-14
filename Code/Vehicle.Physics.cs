namespace MSC;

// https://github.com/SergeyMakeev/ArcadeCarPhysics
public partial class Vehicle
{
	private const int NUMBER_OF_WHEELS = 4;

	protected Vector3 DefaultGravity => Scene?.PhysicsWorld?.Gravity ?? default;

	protected bool AllWheelsInAir =>
		!FrontAxle.WheelDataLeft.IsGrounded &&
		!FrontAxle.WheelDataRight.IsGrounded &&
		!RearAxle.WheelDataLeft.IsGrounded &&
		!RearAxle.WheelDataRight.IsGrounded;

	protected void UpdatePhysics()
	{
		if ( !_isAccelerating && !_isReverseAccelerating )
		{
			_accelerationForceMagnitude = 0.0f;
		}
		else
		{
			var speed = GetSpeedInMetresPerSecond();
			var dt = Time.Delta;

			if ( _isAccelerating )
				_accelerationForceMagnitude = GetAccelerationForceMagnitude( AccelerationCurve, speed, dt );
			else
				_accelerationForceMagnitude = -GetAccelerationForceMagnitude( AccelerationCurveReverse, -speed, dt );
		}

		// 0.8 - pressed
		// 1.0 - not pressed
		var accelerationCoefficient = MathX.Clamp( 0.8f + (1.0f - GetHandBrakeCoefficient()) * 0.2f, 0.0f, 1.0f );
		_accelerationForceMagnitude *= accelerationCoefficient;

		CalculateAckermannSteering();

		var numberOfPoweredWheels = 0;
		if ( FrontAxle.IsPowered )
			numberOfPoweredWheels += 2;
		if ( RearAxle.IsPowered )
			numberOfPoweredWheels += 2;

		CalculateAxleForces( FrontAxle, numberOfPoweredWheels );
		CalculateAxleForces( RearAxle, numberOfPoweredWheels );

		var carUp = WorldTransform.NormalToWorld( Vector3.Up );
		var carDown = -carUp;

		if ( AllWheelsInAir )
		{
			_afterFlightSlipperyTiresTimer = 1.0f;

			// Try to keep vehicle parallel to ground (maybe we let people control this i.e GTA?)
			var worldUp = Vector3.Up;

			var cross = Vector3.Cross( worldUp, carUp );
			var mass = Rigidbody.Mass;

			var angularVelocity = Rigidbody.AngularVelocity;

			var angularVelocityDamping = angularVelocity;
			angularVelocityDamping.y = 0.0f;
			angularVelocityDamping *= MathX.Clamp( FlightStabilizationDamping * Time.Delta, 0.0f, 1.0f );

			Rigidbody.AngularVelocity = angularVelocity - angularVelocityDamping;
			Rigidbody.ApplyTorque( cross * FlightStabilizationForce * mass );
		}
		else
		{
			var speed = GetSpeedInMetresPerSecond();
			var speedKmH = MathF.Abs( speed ) * 3.6f;

			var mass = Rigidbody.Mass;
			var downForceAmount = DownForceCurve.Evaluate( speedKmH ) / 100.0f;

			Rigidbody.ApplyForce( carDown * mass * downForceAmount * DownForce );
		}
	}

	private void CalculateAckermannSteering()
	{
		FrontAxle.WheelDataLeft.YawInRadians = MathX.DegreeToRadian( FrontAxle.SteerAngle );
		FrontAxle.WheelDataRight.YawInRadians = MathX.DegreeToRadian( FrontAxle.SteerAngle );

		RearAxle.WheelDataLeft.YawInRadians = MathX.DegreeToRadian( RearAxle.SteerAngle );
		RearAxle.WheelDataRight.YawInRadians = MathX.DegreeToRadian( RearAxle.SteerAngle );

		var axleDiff = WorldTransform.PointToWorld( new Vector3( 0.0f, FrontAxle.Offset.y, FrontAxle.Offset.x ) ) - WorldTransform.PointToWorld( new Vector3( 0.0f, RearAxle.Offset.y, RearAxle.Offset.x ) );
		var axleSeparation = axleDiff.Length;

		var wheelDiff = WorldTransform.PointToWorld( new Vector3( FrontAxle.Width * -0.5f, FrontAxle.Offset.y, FrontAxle.Offset.x ) ) - WorldTransform.PointToWorld( new Vector3( FrontAxle.Width * 0.5f, FrontAxle.Offset.y, FrontAxle.Offset.x ) );
		var wheelsSeparation = wheelDiff.Length;

		var turningCircleRadius = axleSeparation / MathF.Tan( MathX.DegreeToRadian( FrontAxle.SteerAngle ) );

		var steerAngleLeft = MathF.Atan( axleSeparation / (turningCircleRadius + (wheelsSeparation / 2)) );
		var steerAngleRight = MathF.Atan( axleSeparation / (turningCircleRadius - (wheelsSeparation / 2)) );

		FrontAxle.WheelDataLeft.YawInRadians = steerAngleLeft;
		FrontAxle.WheelDataRight.YawInRadians = steerAngleRight;
	}

	private void CalculateAxleForces( Axle axle, int numberOfPoweredWheels )
	{
		var wsDown = WorldTransform.NormalToWorld( Vector3.Down );
		var (wsL, wsR) = GetWorldWheelPositions( axle );

		CalculateWheelForces( axle, wsDown, axle.WheelDataLeft, true, wsL, numberOfPoweredWheels );
		CalculateWheelForces( axle, wsDown, axle.WheelDataRight, false, wsR, numberOfPoweredWheels );

		// http://projects.edy.es/trac/edy_vehicle-physics/wiki/TheStabilizerBars
		// Apply "stablizier bar" forces
		var travelL = 1.0f - MathX.Clamp( axle.WheelDataLeft.Compression, 0.0f, 1.0f );
		var travelR = 1.0f - MathX.Clamp( axle.WheelDataRight.Compression, 0.0f, 1.0f );

		var antiRollForce = (travelL - travelR) * axle.AntiRollForce;

		if ( axle.WheelDataLeft.IsGrounded )
			Rigidbody.ApplyForceAt( axle.WheelDataLeft.TouchTrace.HitPosition, wsDown * antiRollForce );

		if ( axle.WheelDataRight.IsGrounded )
			Rigidbody.ApplyForceAt( axle.WheelDataRight.TouchTrace.HitPosition, wsDown * -antiRollForce );
	}

	private void CalculateWheelForces( Axle axle, Vector3 wsDown, Wheel wheel, bool isLeftWheel, Vector3 wsAttachPoint, int numberOfPoweredWheels )
	{
		var dt = Time.Delta;

		wheel.IsGrounded = false;

		// Get wheel world space rotation and axes.
		var localWheelRot = Rotation.FromYaw( wheel.YawInRadians.RadianToDegree() );
		var wsWheelRot = WorldTransform.Rotation * localWheelRot;

		// Wheel axle left direction
		var wsAxleLeft = wsWheelRot * Vector3.Left;

		var traceLength = axle.LengthRelaxed + axle.Radius;
		var wheelWidth = 0.085f; // You should probably make this a property.

		var leftTrace = Scene.Trace
			.Ray( wsAttachPoint + wsAxleLeft * wheelWidth, wsAttachPoint + wsAxleLeft * wheelWidth + wsDown * traceLength )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "player" )
			.Run();

		var rightTrace = Scene.Trace
			.Ray( wsAttachPoint - wsAxleLeft * wheelWidth, wsAttachPoint - wsAxleLeft * wheelWidth + wsDown * traceLength )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "player" )
			.Run();

		var centerTrace = Scene.Trace
			.Ray( wsAttachPoint, wsAttachPoint + wsDown * traceLength )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "player" )
			.Run();

		// Check if all traces hit valid surfaces.
		const float groundDot = 0.6f;

		var leftHit = leftTrace.Hit && Vector3.Dot( leftTrace.Normal, Vector3.Up ) >= groundDot;
		var rightHit = rightTrace.Hit && Vector3.Dot( rightTrace.Normal, Vector3.Up ) >= groundDot;
		var centerHit = centerTrace.Hit && Vector3.Dot( centerTrace.Normal, Vector3.Up ) >= groundDot;

		// No wheel contact found.
		if ( !centerHit || !leftHit || !rightHit )
		{
			// Wheel not touching ground(relaxing spring).
			var relaxSpeed = 1.0f;
			wheel.CompressionPrevious = wheel.Compression;
			wheel.Compression = MathX.Clamp( wheel.Compression - dt * relaxSpeed, 0.0f, 1.0f );
			return;
		}

		// Consider wheel radius.
		var suspensionLength = centerTrace.Distance - axle.Radius;

		wheel.IsGrounded = true;
		wheel.TouchTrace = centerTrace;

		//
		// Calculate Suspension Force
		//

		// Positive value means that the spring is compressed.
		// Negative value means that the spring is elongated.
		wheel.Compression = 1.0f - MathX.Clamp( suspensionLength / axle.LengthRelaxed, 0.0f, 1.0f );

		// Hooke's law (springs) F = -k x 
		var suspensionForceMagnitude = 0.0f;
		var springForce = wheel.Compression * -axle.Stiffness;
		suspensionForceMagnitude += springForce;

		// Damping force (try to reset velocity to 0)
		var suspensionCompressionVelocity = (wheel.Compression - wheel.CompressionPrevious) / dt;
		wheel.CompressionPrevious = wheel.Compression;

		var damperForce = -suspensionCompressionVelocity * axle.Damping;
		suspensionForceMagnitude += damperForce;

		// Only consider component of force that is along the contact normal.
		var denom = Vector3.Dot( wheel.TouchTrace.Normal, -wsDown );
		suspensionForceMagnitude *= denom;

		// Apply suspension force
		var suspensionForce = wsDown * suspensionForceMagnitude;
		Rigidbody.ApplyForceAt( wheel.TouchTrace.HitPosition, suspensionForce );

		//
		// Calculate Friction Forces
		//

		var wheelVelocity = Rigidbody.GetVelocityAtPoint( wheel.TouchTrace.HitPosition );

		// Contact basis(can be different from wheel basis).
		var contactUp = wheel.TouchTrace.Normal;
		var contactLeft = (leftTrace.HitPosition - rightTrace.HitPosition).Normal;
		var contactForward = Vector3.Cross( contactLeft, contactUp );

		// Calculate sliding velocity(velocity without normal force).
		var lvel = Vector3.Dot( wheelVelocity, contactLeft ) * contactLeft;
		var fvel = Vector3.Dot( wheelVelocity, contactForward ) * contactForward;
		var slideVelocity = (lvel + fvel) * 0.5f;

		// Calculate current sliding force.
		var slidingForce = (slideVelocity * Rigidbody.Mass / dt) / NUMBER_OF_WHEELS;

		var lateralFriction = MathX.Clamp( axle.LateralFriction, 0.0f, 1.0f );

		var slipperyK = 1.0f;

		// Simulate slippery tires.
		if ( _afterFlightSlipperyTiresTimer > 0.0f )
		{
			var slippery = MathX.Lerp( 1.0f, axle.AfterFlightSlipperyCoefficient, MathX.Clamp( _afterFlightSlipperyTiresTimer, 0.0f, 1.0f ) );
			slipperyK = MathF.Min( slipperyK, slippery );
		}

		if ( _brakeSlipperyTiresTimer > 0.0f )
		{
			var slippery = MathX.Lerp( 1.0f, axle.BrakeSlipperyCoefficient, MathX.Clamp( _brakeSlipperyTiresTimer, 0.0f, 1.0f ) );
			slipperyK = MathF.Min( slipperyK, slippery );
		}

		var handBrakeK = GetHandBrakeCoefficient();
		if ( handBrakeK > 0.0f )
		{
			var slippery = MathX.Lerp( 1.0f, axle.HandBrakeSlipperyCoefficient, handBrakeK );
			slipperyK = MathF.Min( slipperyK, slippery );
		}

		lateralFriction *= slipperyK;

		// Simulate perfect static friction
		var frictionForce = -slidingForce * lateralFriction;

		// Remove friction along roll-direction of wheel 
		var longitudinalForce = Vector3.Dot( frictionForce, contactForward ) * contactForward;

		// Apply braking force or rolling resistance force or nothing
		var isBrakeEnabled = isLeftWheel ? axle.BrakeLeft : axle.BrakeRight;
		var isHandBrakeEnabled = isLeftWheel ? axle.HandBrakeLeft : axle.HandBrakeRight;

		if ( isBrakeEnabled || isHandBrakeEnabled )
		{
			var clampedMagnitude = MathX.Clamp( axle.BrakeForceMagnitude * Rigidbody.Mass, 0.0f, longitudinalForce.Length );
			var brakeForce = longitudinalForce.Normal * clampedMagnitude;

			if ( isHandBrakeEnabled )
				brakeForce *= 0.8f;

			longitudinalForce -= brakeForce;
		}
		else
		{
			// Apply rolling-friction (automatic slow-down) only if player don't press to the accelerator
			if ( !_isAccelerating && !_isReverseAccelerating )
			{
				var rollingK = 1.0f - MathX.Clamp( axle.RollingFriction, 0.0f, 1.0f );
				longitudinalForce *= rollingK;
			}
		}

		frictionForce -= longitudinalForce;

		Rigidbody.ApplyForceAt( wheel.TouchTrace.HitPosition, frictionForce );

		// Engine force
		if ( !_isBraking && axle.IsPowered && MathF.Abs( _accelerationForceMagnitude ) > 0.01f )
		{
			var accForcePoint = wheel.TouchTrace.HitPosition - (wsDown * 0.2f);
			var engineForce = contactForward * _accelerationForceMagnitude / numberOfPoweredWheels / dt;

			Rigidbody.ApplyForceAt( accForcePoint, engineForce );
		}
	}

	private float GetAccelerationForceMagnitude( Curve curve, float speedMetersPerSecond, float deltaTime )
	{
		var speedKmH = speedMetersPerSecond * 3.6f;

		if ( curve.Length == 0 )
			return 0.0f;

		float desiredSpeedKmH;

		if ( curve.Length == 1 )
		{
			desiredSpeedKmH = curve.Evaluate( curve[0].Time );
		}
		else
		{
			var minTime = curve[0].Time;
			var maxTime = curve[^1].Time;
			var step = maxTime - minTime;
			var timeNow = minTime;
			bool resultFound = false;

			if ( speedKmH < curve.Evaluate( curve[^1].Time ) )
			{
				for ( int i = 0; i < ReverseEvaluationAccuracy; ++i )
				{
					var currentSpeed = curve.Evaluate( timeNow );
					var currentSpeedDifference = Math.Abs( speedKmH - currentSpeed );

					var stepTime = timeNow + step;
					var stepSpeed = curve.Evaluate( stepTime );
					var stepSpeedDifference = Math.Abs( speedKmH - stepSpeed );

					if ( stepSpeedDifference < currentSpeedDifference )
					{
						timeNow = stepTime;
						currentSpeed = stepSpeed;
					}

					step = MathF.Abs( step / 2 ) * MathF.Sign( speedKmH - currentSpeed );
				}
				resultFound = true;
			}

			if ( resultFound )
				desiredSpeedKmH = curve.Evaluate( timeNow + deltaTime );
			else
				desiredSpeedKmH = curve.Evaluate( maxTime ); // Max speed
		}

		var accelerationKmH = desiredSpeedKmH - speedKmH;
		var accelerationMs = accelerationKmH / 3.6f; // Convert to m/s
		var forceMagnitude = accelerationMs * Rigidbody.Mass;

		return MathF.Max( forceMagnitude, 0.0f );
	}

	private float GetSteerAngleLimitInDegrees( float speedKmPerHour )
	{
		speedKmPerHour *= GetSteeringHandBrakeCoefficient();
		var limitDegrees = SteerAngleLimit.Evaluate( speedKmPerHour );
		return limitDegrees;
	}

	private float GetSteeringHandBrakeCoefficient()
	{
		// 0.4 - pressed
		// 1.0 - not pressed
		var steeringK = MathX.Clamp( 0.4f + (1.0f - GetHandBrakeCoefficient()) * 0.6f, 0.0f, 1.0f );
		return steeringK;
	}

	private float GetHandBrakeCoefficient()
	{
		var x = _handBrakeSlipperyTiresTimer / Math.Max( 0.1f, HandBrakeSlipperyTime );
		// Convert to smooth step
		x = x * x * x * (x * (x * 6 - 15) + 10);
		return x;
	}

	private float GetSpeedInMetresPerSecond()
	{
		var velocity = Rigidbody.Velocity;
		var wsForward = Rigidbody.WorldRotation * Vector3.Forward;
		var vProj = Vector3.Dot( velocity, wsForward );
		var projVelocity = vProj * wsForward;
		var speed = projVelocity.Length * MathF.Sign( vProj );
		return speed * 0.0254f;
	}

	public (Vector3 wsLeft, Vector3 wsRight) GetWorldWheelPositions( Axle axle )
	{
		var localL = new Vector3( axle.Offset.x, axle.Width * 0.5f, axle.Offset.y );
		var localR = new Vector3( axle.Offset.x, axle.Width * -0.5f, axle.Offset.y );

		return (WorldTransform.PointToWorld( localL ), WorldTransform.PointToWorld( localR ));
	}
}

namespace MSC;

// https://github.com/SergeyMakeev/ArcadeCarPhysics
public partial class Vehicle
{
	protected void UpdateInput()
	{
		var vertical = Input.AnalogMove.x;
		var horizontal = Input.AnalogMove.y;
		var requestHandBrake = Input.Down( InputAction.JUMP );
		var speed = GetSpeedInMetresPerSecond();

		var requestBrake = UpdateAcceleration( vertical, speed );
		UpdateBraking( requestBrake, requestHandBrake );
		UpdateSteering( horizontal, speed );
	}

	private bool UpdateAcceleration( float vertical, float speed )
	{
		var requestBrake = false;

		_isAccelerating = false;
		_isReverseAccelerating = false;

		if ( vertical > 0.4f )
		{
			if ( speed < -0.5f )
				requestBrake = true;
			else
				_isAccelerating = true;
		}
		else if ( vertical <= -0.4f )
		{
			if ( speed > 0.5f )
				requestBrake = true;
			else
				_isReverseAccelerating = true;
		}

		return requestBrake;
	}

	private void UpdateBraking( bool requestBrake, bool requestHandBrake )
	{
		if ( requestBrake && !_isBraking )
			_brakeSlipperyTiresTimer = 1.0f;

		if ( requestHandBrake )
			_handBrakeSlipperyTiresTimer = Math.Max( 0.1f, HandBrakeSlipperyTime );

		_isBraking = requestBrake;
		_isHandBraking = requestHandBrake && !_isAccelerating && !_isReverseAccelerating;

		FrontAxle.BrakeLeft = _isBraking;
		FrontAxle.BrakeRight = _isBraking;
		RearAxle.BrakeLeft = _isBraking;
		RearAxle.BrakeRight = _isBraking;

		FrontAxle.HandBrakeLeft = _isHandBraking;
		FrontAxle.HandBrakeRight = _isHandBraking;
		RearAxle.HandBrakeLeft = _isHandBraking;
		RearAxle.HandBrakeRight = _isHandBraking;
	}

	private void UpdateSteering( float horizontal, float speed )
	{
		if ( MathF.Abs( horizontal ) > 0.001f )
		{
			var speedKmH = MathF.Abs( speed ) * 3.6f;
			speedKmH *= GetSteeringHandBrakeCoefficient();

			var steerSpeed = SteeringSpeed.Evaluate( speedKmH );

			var newSteerAngle = FrontAxle.SteerAngle + (horizontal * steerSpeed);
			var sign = MathF.Sign( newSteerAngle );

			var steerLimit = GetSteerAngleLimitInDegrees( speedKmH );
			newSteerAngle = MathF.Min( MathF.Abs( newSteerAngle ), steerLimit ) * sign;

			FrontAxle.SteerAngle = newSteerAngle;
		}
		else
		{
			var speedKmH = MathF.Abs( speed ) * 3.6f;
			var steerResetSpeed = SteeringResetSpeed.Evaluate( speedKmH );
			var angleReturnPerSecond = MathX.Lerp( 0.0f, steerResetSpeed, MathX.Clamp( speedKmH / 2.0f, 0.0f, 1.0f ) );

			var angle = FrontAxle.SteerAngle;
			var sign = MathF.Sign( angle );

			angle = MathF.Abs( angle );
			angle -= angleReturnPerSecond * Time.Delta;
			angle = MathF.Max( angle, 0.0f ) * sign;

			FrontAxle.SteerAngle = angle;
		}
	}
}

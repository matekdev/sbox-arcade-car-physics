namespace MSC;

// https://github.com/SergeyMakeev/ArcadeCarPhysics
public partial class Vehicle : Component
{
	public const string AXLES = "Axles";
	public const string ENGINE = "Engine";
	public const string STEERING = "Steering";

	public const string MAGNETISM = "Magnetism";
	public const int MAGNETISM_ORDER = 2;

	public const string OTHER = "Other";
	public const int OTHER_ORDER = 3;

	/// <summary>
	/// Acceleration curve for forward driving.
	/// Maps input to engine force output over speed.
	/// </summary>
	[Group( ENGINE ), Property] public Curve AccelerationCurve { get; set; }

	/// <summary>
	/// Acceleration curve for reverse driving.
	/// Defines engine force behavior when moving backward.
	/// </summary>
	[Group( ENGINE ), Property] public Curve AccelerationCurveReverse { get; set; }

	/// <summary>
	/// Accuracy used when evaluating the reverse acceleration curve.
	/// Higher values give smoother reverse behavior at the cost of performance.
	/// </summary>
	[Group( ENGINE ), Property] public float ReverseEvaluationAccuracy { get; set; }

	/// <summary>
	/// Curve controlling maximum steering angle relative to speed.
	/// Prevents sharp turns at high speed.
	/// </summary>
	[Group( STEERING ), Property] public Curve SteerAngleLimit { get; set; }

	/// <summary>
	/// Curve controlling how quickly the steering auto-centers when released.
	/// </summary>
	[Group( STEERING ), Property] public Curve SteeringResetSpeed { get; set; }

	/// <summary>
	/// Curve controlling how fast steering input can change over time.
	/// </summary>
	[Group( STEERING ), Property] public Curve SteeringSpeed { get; set; }

	[Group( AXLES ), Property] public Axle FrontAxle { get; set; } = new();
	[Group( AXLES ), Property] public Axle RearAxle { get; set; } = new();

	/// <summary>
	/// Stabilization force applied when airborne to reduce tumbling.
	/// </summary>
	[Property, Group( OTHER ), Order( OTHER_ORDER )]
	public float FlightStabilizationForce { get; set; }

	/// <summary>
	/// Damping factor for flight stabilization.
	/// Controls how quickly the stabilization force fades out.
	/// </summary>
	[Property, Group( OTHER ), Order( OTHER_ORDER )]
	public float FlightStabilizationDamping { get; set; }

	/// <summary>
	/// Duration (in seconds) that reduced grip is applied after using the handbrake.
	/// </summary>
	[Property, Group( OTHER ), Order( OTHER_ORDER )]
	public float HandBrakeSlipperyTime { get; set; }

	/// <summary>
	/// Curve defining aerodynamic downforce relative to speed.
	/// </summary>
	[Property, Group( OTHER ), Order( OTHER_ORDER )]
	public Curve DownForceCurve { get; set; }

	/// <summary>
	/// Base downforce applied at maximum curve evaluation.
	/// </summary>
	[Property, Group( OTHER ), Order( OTHER_ORDER )]
	public float DownForce { get; set; }

	protected Rigidbody Rigidbody => _rigidbody ??= Components.Get<Rigidbody>( FindMode.EverythingInSelfAndChildren );
	private Rigidbody _rigidbody;

	protected ModelRenderer Renderer => _renderer ??= Components.Get<ModelRenderer>( FindMode.EverythingInSelfAndChildren );
	private ModelRenderer _renderer;

	private bool _isBraking;
	private bool _isHandBraking;
	private bool _isAccelerating;
	private bool _isReverseAccelerating;

	private float _accelerationForceMagnitude;

	private TimeUntil _afterFlightSlipperyTiresTimer;
	private TimeUntil _brakeSlipperyTiresTimer;
	private TimeUntil _handBrakeSlipperyTiresTimer;

	protected override void OnStart()
	{
		base.OnStart();

		if ( !Scene.SceneWorld.IsValid() )
			return;

		if ( FrontAxle.WheelModel.IsValid() )
		{
			FrontLeftWheelRenderer = new SceneObject( Scene.SceneWorld, FrontAxle.WheelModel );
			FrontRightWheelRenderer = new SceneObject( Scene.SceneWorld, FrontAxle.WheelModel );
		}

		if ( RearAxle.WheelModel.IsValid() )
		{
			RearLeftWheelRenderer = new SceneObject( Scene.SceneWorld, RearAxle.WheelModel );
			RearRightWheelRenderer = new SceneObject( Scene.SceneWorld, RearAxle.WheelModel );
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		UpdateInput();
		UpdatePhysics();
	}

	protected override void OnUpdate()
	{
		if ( !IsProxy )
			CalculateWheelVisuals();

		UpdateWheelVisuals();
	}
}

namespace MSC;

// https://github.com/SergeyMakeev/ArcadeCarPhysics
public class Axle
{
	public const string AXLE = "Axle";
	public const string WHEEL = "Wheel";
	public const string VISUAL = "Visual";
	public const string SUSPENSION = "Suspension";

	/// <summary>
	/// Distance between the left and right wheels on this axle.
	/// </summary>
	[Group( AXLE )] public float Width { get; set; }

	/// <summary>
	/// Position of the axle relative to the car body (forward/backward, left/right).
	/// </summary>
	[Group( AXLE )] public Vector2 Offset { get; set; }

	/// <summary>
	/// Whether this axle receives engine power (driven wheels).
	/// </summary>
	[Group( AXLE )] public bool IsPowered { get; set; }

	/// <summary>
	/// Physical size of the wheels, affects ride height and speed calculation.
	/// </summary>
	[Group( WHEEL )] public float Radius { get; set; }

	/// <summary>
	/// Sideways grip (resists sliding sideways during turns).
	/// </summary>
	[Group( WHEEL ), Range( 0.0f, 1.0f )] public float LateralFriction { get; set; }

	/// <summary>
	/// Forward rolling resistance (tire drag, reduces coasting).
	/// </summary>
	[Group( WHEEL ), Range( 0.0f, 1.0f )] public float RollingFriction { get; set; }

	/// <summary>
	/// Maximum braking force the wheels on this axle can apply.
	/// </summary>
	[Group( WHEEL )] public float BrakeForceMagnitude { get; set; }

	/// <summary>
	/// Reduces grip briefly after landing from a jump, making wheels slide before regaining traction.
	/// </summary>
	[Group( WHEEL )] public float AfterFlightSlipperyCoefficient { get; set; }

	/// <summary>
	/// Reduces grip while braking (simulates tires locking/slipping under heavy brake).
	/// </summary>
	[Group( WHEEL )] public float BrakeSlipperyCoefficient { get; set; }

	/// <summary>
	/// Reduces grip more aggressively when handbrake is applied (useful for drifting).
	/// </summary>
	[Group( WHEEL )] public float HandBrakeSlipperyCoefficient { get; set; }

	/// <summary>
	/// The current steering angle applied to this axle’s wheels (set at runtime).
	/// </summary>
	[Group( WHEEL ), Hide] public float SteerAngle { get; set; }

	/// <summary>
	/// Whether the left wheel on this axle is currently applying brake force.
	/// </summary>
	[Group( WHEEL ), Hide] public bool BrakeLeft { get; set; }

	/// <summary>
	/// Whether the right wheel on this axle is currently applying brake force.
	/// </summary>
	[Group( WHEEL ), Hide] public bool BrakeRight { get; set; }

	/// <summary>
	/// Whether the left wheel on this axle is currently applying handbrake force (for drifting/skidding).
	/// </summary>
	[Group( WHEEL ), Hide] public bool HandBrakeLeft { get; set; }

	/// <summary>
	/// Whether the right wheel on this axle is currently applying handbrake force (for drifting/skidding).
	/// </summary>
	[Group( WHEEL ), Hide] public bool HandBrakeRight { get; set; }

	/// <summary>
	/// The simulation and state data for the left wheel on this axle.
	/// </summary>
	[Group( WHEEL ), Hide] public Wheel WheelDataLeft { get; set; } = new();

	/// <summary>
	/// The simulation and state data for the right wheel on this axle.
	/// </summary>
	[Group( WHEEL ), Hide] public Wheel WheelDataRight { get; set; } = new();

	/// <summary>
	/// How much force the suspension resists compression with (spring strength).
	/// </summary>
	[Group( SUSPENSION )] public float Stiffness { get; set; }

	/// <summary>
	/// How quickly suspension oscillations are absorbed (shock absorber strength).
	/// </summary>
	[Group( SUSPENSION )] public float Damping { get; set; }

	/// <summary>
	/// Suspension length at rest (determines ride height).
	/// </summary>
	[Group( SUSPENSION )] public float LengthRelaxed { get; set; }

	/// <summary>
	/// How strongly the suspension resists body roll when cornering.
	/// </summary>
	[Group( SUSPENSION )] public float AntiRollForce { get; set; }

	/// <summary>
	/// Scale factor for wheel models (for adjusting visuals without changing physics).
	/// </summary>
	[Group( VISUAL ), Order( 50 )] public float VisualScale { get; set; } = 1;

	/// <summary>
	/// The model that will be used for the wheels.
	/// </summary>
	[Group( VISUAL ), Order( 50 )] public Model WheelModel { get; set; }
}

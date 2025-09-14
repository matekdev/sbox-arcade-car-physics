namespace MSC;

// https://github.com/SergeyMakeev/ArcadeCarPhysics
public class Wheel
{
	/// <summary>
	/// True if the wheel is currently in contact with the ground.
	/// </summary>
	public bool IsGrounded;

	/// <summary>
	/// The result of the ground trace for this wheel (hit info, normal, etc.).
	/// </summary>
	public SceneTraceResult TouchTrace;

	/// <summary>
	/// Current yaw (rotation around the vertical axis) of the wheel in radians.
	/// </summary>
	public float YawInRadians;

	/// <summary>
	/// Rotation of the wheel model in radians for visual spinning (around axle).
	/// </summary>
	public float VisualRotationInRadians;

	/// <summary>
	/// Current suspension compression (0 = fully extended, 1 = fully compressed).
	/// </summary>
	public float Compression;

	/// <summary>
	/// Suspension compression from the previous frame (used to calculate damping).
	/// </summary>
	public float CompressionPrevious;
}

using Unity.Entities;

public struct PredictionSwitchingSettings : IComponentData
{
    public Entity Player;
    public float PlayerSpeed;

    public float TransitionDurationSeconds;
    public float PredictionSwitchingRadius;
    /// <summary>The margin must be large enough that moving from predicted time to interpolated time does not move the ghost back into the prediction sphere.</summary>
    public float PredictionSwitchingMargin;

    public byte BallColorChangingEnabled;
}

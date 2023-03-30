using Unity.Entities;

[UnityEngine.DisallowMultipleComponent]
public class PredictionSwitchingSettingsAuthoring : UnityEngine.MonoBehaviour
{
    public UnityEngine.GameObject Player;
    [RegisterBinding(typeof(PredictionSwitchingSettings), "PlayerSpeed")]
    public float PlayerSpeed;
    [RegisterBinding(typeof(PredictionSwitchingSettings), "TransitionDurationSeconds")]
    public float TransitionDurationSeconds;
    [RegisterBinding(typeof(PredictionSwitchingSettings), "PredictionSwitchingRadius")]
    public float PredictionSwitchingRadius;
    [RegisterBinding(typeof(PredictionSwitchingSettings), "PredictionSwitchingMargin")]
    public float PredictionSwitchingMargin;
    [RegisterBinding(typeof(PredictionSwitchingSettings), "BallColorChangingEnabled")]
    public byte BallColorChangingEnabled;

    class PredictionSwitchingSettingsBaker : Baker<PredictionSwitchingSettingsAuthoring>
    {
        public override void Bake(PredictionSwitchingSettingsAuthoring authoring)
        {
            PredictionSwitchingSettings component = default(PredictionSwitchingSettings);
            component.Player = GetEntity(authoring.Player, TransformUsageFlags.Dynamic);
            component.PlayerSpeed = authoring.PlayerSpeed;
            component.TransitionDurationSeconds = authoring.TransitionDurationSeconds;
            component.PredictionSwitchingRadius = authoring.PredictionSwitchingRadius;
            component.PredictionSwitchingMargin = authoring.PredictionSwitchingMargin;
            component.BallColorChangingEnabled = authoring.BallColorChangingEnabled;
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}

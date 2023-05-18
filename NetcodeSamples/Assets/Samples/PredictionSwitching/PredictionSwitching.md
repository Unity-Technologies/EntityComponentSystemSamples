# Prediction Switching Sample
[Prediction Switching](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/prediction.html#prediction-switching) allows you to switch the `GhostMode` of a ghost on the fly (i.e. during playmode), allowing clients to opt-into prediction, ultimately saving CPU cycles.

This sample demonstrates that idea with a simplified "football" sandbox.

* `Sphere.prefab` is a physics ball, which is relatively expensive to predict (in high quantities). It is the target of this optimization.
* `Player.prefab` is our player (i.e. character controller), which:
    * Interacts with these balls (by colliding with them).
    * Defines the center of the "Prediction Switching Radius" (see `PredictionSwitchingSystem.cs`).

#### Color Key
* **Cyan** - Interpolated Ghosts.
* **Green** - Predicted Ghosts.
* _Player colors are excluded._

> [!NOTE]
> This color key is also used by the "Bounding Box Drawer" tool, which is toggleable via the `Multiplayer PlayMode Tools Window > Bounding Box Drawer > Disabled` button.

By entering playmode, you are able to observe that balls within a radius of the player transition to Predicted (and vice-versa).
You should also be able to observe the interpolation being applied (during the transition), especially when balls bounce off each other.

See the `PredictionSwitchingSettingsAuthoring` `MonoBehaviour` to modify the settings, and observe how they affect gameplay.

## Takeaways
* Generally speaking: As with all things in NetCode, faster moving (and more unpredictable) entities are significantly harder to compensate for.
* "Prediction Switching" allows you to selectively opt-into Predicting a ghost, giving you a decent tradeoff between client performance (via Interpolation) and gameplay responsiveness (via Client Prediction), while maintaining server authority.

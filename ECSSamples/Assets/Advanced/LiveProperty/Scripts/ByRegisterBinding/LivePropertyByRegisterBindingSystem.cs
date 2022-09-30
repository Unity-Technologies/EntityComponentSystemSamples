using Unity.Entities;

public partial class LivePropertyByRegisterBindingSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref LivePropertyBindingRegisteryComponent move) =>
        {
            // float, int, bool
            move.BindFloat += 1.0f;
            move.BindInt += 1;
            move.BindBool = !move.BindBool;
        }).WithoutBurst().Run();
    }
}

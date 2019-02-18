using Unity.Entities;

public struct MoveForward : ISharedComponentData { }

public class MoveForwardProxy : SharedComponentDataProxy<MoveForward> { }

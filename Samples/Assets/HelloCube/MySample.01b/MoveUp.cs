using Unity.Entities;

namespace HelloCube.MySample._01b
{
    public struct MoveUp : IComponentData
    {
        // この構造体は、状態繊維の判定にのみ用いられるため
        // 処理はもちろんのこと、データの宣言すら不要
    }
}
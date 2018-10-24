using UnityEngine;

namespace TwoStickClassicExample
{
    public class Faction : MonoBehaviour
    {

        public enum Type
        {
            Enemy = 0,
            Player = 1
        }

        public Type Value;
    }

}
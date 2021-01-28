using UnityEngine;

public class GameObjectAnimationSystem : MonoBehaviour
{
    public int spawnIndex
    {
        get { return m_spawnIndex; }
        set { m_spawnIndex = value; }
    }
    private int m_spawnIndex = 0;
    
    public int height
    {
        get { return m_height; }
        set { m_height = value; }
    }
    private int m_height = 0;
       
    void Start()
    {
        var color = gameObject.GetComponent<AnimateGameObjectColor>();
        if(color)
            color.spawnIndex = spawnIndex;
        
        var pos = gameObject.GetComponent<AnimateGameObjectPosition>();
        if (pos)
        {
            pos.spawnIndex = spawnIndex;
            pos.height = height;
        }
        
    }
}

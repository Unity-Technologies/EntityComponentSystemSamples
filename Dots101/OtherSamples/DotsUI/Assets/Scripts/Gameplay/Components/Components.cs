using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.DotsUISample
{
    public struct Event : IComponentData
    {
    }
    
    public struct PickupEvent : IComponentData
    {
    }
    
    public struct CollectableProximityEvent : IComponentData
    {
        public float3 Position;
    }
    
    public struct CauldronProximityEvent : IComponentData
    {
        public float3 Position;
    }
    
    public struct UIScreens : IComponentData
    {
        public UnityObjectRef<SplashScreen> SplashScreen;
        public UnityObjectRef<HelpScreen> HelpScreen;
        public UnityObjectRef<QuestScreen> QuestScreen;
        public UnityObjectRef<HUDScreen> HUDScreen;
        public UnityObjectRef<InventoryScreen> InventoryScreen;
        public UnityObjectRef<DialogueScreen> DialogueScreen;
        public UnityObjectRef<HintScreen> HintScreen;
        public UnityObjectRef<Camera> Camera;
    }
    
    public struct CameraRef : IComponentData
    {
        public UnityObjectRef<Camera> Camera;
        public float3 Offset;
    }

}
using Unity.Entities;
using UnityEngine;

namespace Streaming.AssetManagement
{
#if !UNITY_DISABLE_MANAGED_COMPONENTS
    public partial struct UIInteropSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<References>();
            state.EntityManager.AddComponent<UIInterop>(state.SystemHandle);
            state.EntityManager.SetComponentData(state.SystemHandle, new UIInterop());
        }

        public void OnUpdate(ref SystemState state)
        {
            var ui = state.EntityManager.GetComponentData<UIInterop>(state.SystemHandle);

            if (ui.LoadButton == null)
            {
                ui.LoadButton = GameObject.FindFirstObjectByType<LoadButton>();
                if (ui.LoadButton == null)
                {
                    return;
                }
            }

            if (!ui.LoadButton.Toggle)
            {
                return;
            }

            ui.LoadButton.Toggle = false;

            var unloadQuery = SystemAPI.QueryBuilder().WithAll<References, Loading>().WithNone<RequestUnload>().Build();
            var loadQuery = SystemAPI.QueryBuilder().WithAll<References, RequestUnload>().WithNone<Loading>().Build();

            if (ui.LoadButton.Loaded)
            {
                state.EntityManager.AddComponent<RequestUnload>(unloadQuery);
                ui.LoadButton.Loaded = false;
            }
            else
            {
                state.EntityManager.RemoveComponent<RequestUnload>(loadQuery);
                ui.LoadButton.Loaded = true;
            }
        }
    }

    public class UIInterop : IComponentData
    {
        public LoadButton LoadButton;
    }
#endif
}

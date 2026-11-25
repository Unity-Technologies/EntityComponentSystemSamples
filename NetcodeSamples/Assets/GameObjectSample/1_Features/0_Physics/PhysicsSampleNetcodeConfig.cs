using Unity.Entities;
using Unity.Netcode;
using UnityEngine;

public class PhysicsSampleNetcodeConfig : MonoBehaviour
{
    void Awake()
    {
        foreach (var world in World.All)
        {
            if (world.IsServer())
            {
                var clientServerTickRateQuery = world.EntityManager.CreateEntityQuery(typeof(ClientServerTickRate));
                if (!clientServerTickRateQuery.IsEmptyIgnoreFilter)
                {
                    var tr = clientServerTickRateQuery.GetSingleton<ClientServerTickRate>();
                    tr.MaxSimulationStepsPerFrame = 3;
                    tr.MaxSimulationStepBatchSize = 1;
                    clientServerTickRateQuery.SetSingleton(tr);
                }

                var ghostSendSystemDataQuery = world.EntityManager.CreateEntityQuery(typeof(GhostSendSystemData));
                if (!ghostSendSystemDataQuery.IsEmptyIgnoreFilter)
                {
                    var s = ghostSendSystemDataQuery.GetSingleton<GhostSendSystemData>();
                    s.DefaultSnapshotPacketSize = 15000;
                    ghostSendSystemDataQuery.SetSingleton(s);
                }
            }
        }
    }
}

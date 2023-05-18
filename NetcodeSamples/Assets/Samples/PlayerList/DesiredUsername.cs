using System;
using Unity.Collections;
using Unity.Entities;

namespace Unity.NetCode.Samples.PlayerList
{
    /// <summary>
    ///     The PlayerList sample will handle notifying other users of this `DesiredUsername`.
    ///     <inheritdoc cref="Value"/>
    /// </summary>
    public struct DesiredUsername : IComponentData
    {
        /// <remarks>Changing this value will trigger an RPC broadcast, so you should only set this once the user is finished inputting their new name.</remarks>
        public FixedString64Bytes Value;
    }
}

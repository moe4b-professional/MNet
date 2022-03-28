using System;

namespace MNet
{
    [Flags]
    public enum RemoteAuthority : byte
    {
        /// <summary>
        /// As the name implies, any client will be able to the remote action
        /// </summary>
        Any = 1 << 0,

        /// <summary>
        /// Only the owner of this entity may invoke this this remote action
        /// </summary>
        Owner = 1 << 1,

        /// <summary>
        /// Only the master client may invoke this remote action
        /// </summary>
        Master = 1 << 2,
    }
}
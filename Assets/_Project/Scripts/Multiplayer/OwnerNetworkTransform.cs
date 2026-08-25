using Unity.Netcode.Components;

namespace Robot.Multiplayer
{
    public sealed class OwnerNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}

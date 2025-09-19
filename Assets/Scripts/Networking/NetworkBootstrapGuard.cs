using Unity.Netcode;
using UnityEngine;

namespace MOBA.Networking
{
    internal static class NetworkBootstrapGuard
    {
        private static readonly object gate = new object();
        private static int lastBootstrapFrame = -1;
        private static bool reservationActive;

        internal static bool TryReserve(out string reason)
        {
            lock (gate)
            {
                if (NetworkManager.Singleton != null)
                {
                    reason = "NetworkManager.Singleton already exists.";
                    return false;
                }

                int frame = Time.frameCount;
                if (reservationActive && frame == lastBootstrapFrame)
                {
                    reason = "Network manager instantiation already requested this frame.";
                    return false;
                }

                reservationActive = true;
                lastBootstrapFrame = frame;
                reason = string.Empty;
                return true;
            }
        }

        internal static void Release()
        {
            lock (gate)
            {
                reservationActive = false;
            }
        }
    }
}

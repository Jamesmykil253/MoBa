using UnityEngine;

namespace MOBA
{
    internal static class PhysicsUtility
    {
        public static Vector3 GetVelocity(Rigidbody body)
        {
#if UNITY_6000_0_OR_NEWER
            return body.linearVelocity;
#else
#pragma warning disable 618
            return body.velocity;
#pragma warning restore 618
#endif
        }

        public static void SetVelocity(Rigidbody body, Vector3 velocity)
        {
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = velocity;
#else
#pragma warning disable 618
            body.velocity = velocity;
#pragma warning restore 618
#endif
        }
    }
}

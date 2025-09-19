namespace MOBA
{
    /// <summary>
    /// Receives authoritative health snapshots from the server so clients can update their local presentation.
    /// </summary>
    public interface IDamageSnapshotReceiver
    {
        void ApplyServerHealthSnapshot(float health, bool isDead);
    }
}

namespace FBMMultiMessenger.Database.Models
{
    public class SyncMeta
    {
        public int Id { get; set; }
        public string Key { get; set; }           // PK, e.g. "global"
        public DateTimeOffset LastSyncedAt { get; set; }
    }
}

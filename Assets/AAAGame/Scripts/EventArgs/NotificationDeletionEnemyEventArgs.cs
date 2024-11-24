using GameFramework.Event;
using GameFramework;
/// <summary>
/// 玩家杀死敌人时变通知事件
/// </summary>
public enum GFNotificationDeletionEnemy
{
    killEnemy //杀死敌人
}
public class NotificationDeletionEnemyEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(NotificationDeletionEnemyEventArgs).GetHashCode();
    public override int Id => EventId;
    public GFNotificationDeletionEnemy EventType { get; private set; }
    public int EnemyID { get; private set; }
    public static NotificationDeletionEnemyEventArgs Create(GFNotificationDeletionEnemy eventType, int enemyID)
    {
        var instance = ReferencePool.Acquire<NotificationDeletionEnemyEventArgs>();
        instance.EnemyID = enemyID;
        return instance;
    }
    public override void Clear()
    {
        EventType = default;
        EnemyID = 0;
    }
}

using GameFramework;
using GameFramework.Event;
using System;
using System.Collections.Generic;
using AAAGame.Scripts.Entity;
using Solo.MOST_IN_ONE;
using UnityEngine;
using UnityGameFramework.Runtime;

public class LevelEntity : EntityBase
{
    public const string P_LevelData = "LevelData";
    public const string P_LevelReadyCallback = "OnLevelReady";
    public bool IsAllReady { get; private set; }
    private Transform playerSpawnPoint;
    PlayerEntity m_PlayerEntity;
    
    static PlayerCarEntity m_PlayerCarEntity;
    public Most_Spawn SpawnManager;
    
    List<Spawnner> m_Spawnners;

    HashSet<int> m_EntityLoadingList;
    Dictionary<int, CombatUnitEntity> m_Enemies;
    bool m_IsGameOver;
    //-----TODO：新增加
    static CombatUnitTable m_CombatUnitRow;  
    Dictionary<int, AIEnemyCarEntity> m_AIEnemyCarEntityDic;
    public static LevelEntity Instance { get; private set; } = null;
    LevelTable m_LevelTb;
    public LevelTable LevelTb => m_LevelTb;
    //正计时该局游戏持续时间
    private float m_GameTimer = 0;
    //关卡倒计时
    public float m_ShowGameTimer  { get; private set; } = 0;
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        Instance = this;
        playerSpawnPoint = transform.Find("PlayerSpawnPoint");
        SpawnManager = transform.Find("SpawnGame").GetComponent<Most_Spawn>();
        m_Spawnners = new List<Spawnner>();
        m_EntityLoadingList = new HashSet<int>();
        m_Enemies = new Dictionary<int, CombatUnitEntity>();
        //-----TODO：新增加
        m_AIEnemyCarEntityDic = new Dictionary<int,AIEnemyCarEntity>();
        var combatUnitTb = GF.DataTable.GetDataTable<CombatUnitTable>();
        m_CombatUnitRow = combatUnitTb.GetDataRow(5);
    }

    protected override async void OnShow(object userData)
    {
        base.OnShow(userData);
        Instance = this;
        SpawnManager.OnSpawnNewInstantiatedEvent.AddListener(SpawnEnemiesUpdateNew);
        GF.Event.Subscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
        GF.Event.Subscribe(HideEntityCompleteEventArgs.EventId, OnHideEntityComplete); 
        GF.Event.Subscribe(NotificationDeletionEnemyEventArgs.EventId, OnRemoveEnemy);
        
        m_PlayerEntity = null;
        m_PlayerCarEntity = null;
        m_IsGameOver = false;
        IsAllReady = false;
        m_Spawnners.Clear();
        m_EntityLoadingList.Clear();
        m_Enemies.Clear();
        m_LevelTb = Params.Get<VarObject>(P_LevelData).Value as LevelTable;
        m_GameTimer = 0;
        m_ShowGameTimer = m_LevelTb.LvTime;
        // CachedTransform.Find("EnemySpawnPoints").GetComponentsInChildren<Spawnner>(m_Spawnners);
        // var combatUnitTb = GF.DataTable.GetDataTable<CombatUnitTable>();
        // var playerRow = combatUnitTb.GetDataRow(0);
        // var playerParams = EntityParams.Create(playerSpawnPoint.position, playerSpawnPoint.eulerAngles);
        // playerParams.Set(PlayerEntity.P_DataTableRow, playerRow);
        // playerParams.Set<VarInt32>(PlayerEntity.P_CombatFlag, (int)CombatUnitEntity.CombatFlag.Player);
        // playerParams.Set<VarAction>(PlayerEntity.P_OnBeKilled, (Action)OnPlayerBeKilled);
        // // 关卡创建成功之后显示玩家预制体
        // m_PlayerEntity =
        //     await GF.Entity.ShowEntityAwait<PlayerEntity>(playerRow.PrefabName, Const.EntityGroup.Player, playerParams)
        //         as PlayerEntity; //同步创建玩家实体立即返回一个实体
        // // var playerid = GF.Entity.ShowEntity<PlayerEntity>(playerRow.PrefabName, Const.EntityGroup.Player, playerParams);//异步创建玩家实体会返回一个ID
        // //GF.Entity.HideEntity(playerid);//如果是在未加载完成的时候调用会取消任务，如果加载完成会隐藏
        // CameraController.Instance.SetFollowTarget(m_PlayerEntity.CachedTransform); //设置相机的跟随目标
        
        m_AIEnemyCarEntityDic.Clear();
        var combatUnitTb = GF.DataTable.GetDataTable<CombatUnitTable>();
        var playerRow = combatUnitTb.GetDataRow(4);
        var playerParams = EntityParams.Create(playerSpawnPoint.position, playerSpawnPoint.eulerAngles);
        playerParams.Set(PlayerCarEntity.P_DataTableRow, playerRow);
        playerParams.Set<VarInt32>(PlayerCarEntity.P_CombatFlag, (int)CombatUnitEntity.CombatFlag.Player);
        playerParams.Set<VarAction>(PlayerCarEntity.P_OnBeKilled, (Action)OnPlayerBeKilled);
        //关卡创建成功之后显示玩家预制体
        m_PlayerCarEntity =
            await GF.Entity.ShowEntityAwait<PlayerCarEntity>(playerRow.PrefabName, Const.EntityGroup.Player, playerParams)
                as PlayerCarEntity; //同步创建玩家实体立即返回一个实体
        CameraController.Instance.SetFollowTarget(m_PlayerCarEntity.CachedTransform); //设置相机的跟随目标
        IsAllReady = true;
    }


    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        if (m_IsGameOver || !IsAllReady) return;
        //TODO：注释后边需要解开
        //SpawnEnemiesUpdate();
    }

    protected override void OnHide(bool isShutdown, object userData)
    {
        SpawnManager.OnSpawnNewInstantiatedEvent.RemoveListener(SpawnEnemiesUpdateNew);
        GF.Event.Unsubscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
        GF.Event.Unsubscribe(HideEntityCompleteEventArgs.EventId, OnHideEntityComplete);
        GF.Event.Unsubscribe(NotificationDeletionEnemyEventArgs.EventId, OnRemoveEnemy);
        
        base.OnHide(isShutdown, userData);
    }

    public void StartGame()
    {
        //TODO：注释后边需要解开
        //m_PlayerEntity.Ctrlable = true;
        m_PlayerCarEntity.Ctrlable = true;
        if (SpawnManager != null)
        {
            SpawnManager.EnableState(true);
            SpawnManager.SpawnRatePerCycle = 100; 
        }
    }

    private void SpawnEnemiesUpdate()
    {
        if (m_Spawnners.Count == 0) return;
        Spawnner item = null;
        var playerPos = m_PlayerEntity.CachedTransform.position;
        for (int i = m_Spawnners.Count - 1; i >= 0; i--)
        {
            item = m_Spawnners[i];
            if (item.CheckInBounds(playerPos))
            {
                var ids = item.SpawnAllCombatUnits(m_PlayerEntity);
                m_Spawnners.RemoveAt(i);
                foreach (var entityId in ids)
                {
                    m_EntityLoadingList.Add(entityId);
                }
            }
        }
    }
    #region 新添加测试逻辑
    /// <summary>
    /// 用来刷新关卡进行时间
    /// </summary>
    /// <param name="elapseSeconds"></param>
    /// <param name="realElapseSeconds"></param>
    public void LogicUpdate(float elapseSeconds, float realElapseSeconds)
    {
        m_GameTimer += elapseSeconds;
        //倒计时  
        m_ShowGameTimer -= elapseSeconds;
        if (m_GameTimer >= m_LevelTb.LvTime)
        {
            if (m_IsGameOver) return;
            m_IsGameOver = true;
            GF.LogInfo("倒计时结束了设置游戏结束");
            var eParms = RefParams.Create();
            eParms.Set<VarBoolean>("IsWin", true);
            GF.Event.Fire(GameplayEventArgs.EventId, GameplayEventArgs.Create(GameplayEventType.GameOver, eParms));
        }
    }
    //检查敌人对象列表里的数量是否达到最大生成数量
    private bool CheckEnemyCount()
    {
        if (SpawnManager == null) return false;
        return m_AIEnemyCarEntityDic.Count >= SpawnManager.MaxNumberInScene;
    }
    
    public void SpawnEnemiesUpdateNew(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        AIEnemyCarEntity entity = obj.GetComponent<AIEnemyCarEntity>();
        entity.SetTarget(m_PlayerCarEntity);
        //给添加到列表里的对象生成一个HASH值
        entity.ID = entity.GetHashCode();
        //将敌人对象加入列表
        m_AIEnemyCarEntityDic.Add(entity.ID,entity);
    }
    #endregion
    private void OnPlayerBeKilled()
    {
        if (m_IsGameOver) return;
        m_IsGameOver = true;
        var eParms = RefParams.Create();
        eParms.Set<VarBoolean>("IsWin", false);
        GF.Event.Fire(GameplayEventArgs.EventId, GameplayEventArgs.Create(GameplayEventType.GameOver, eParms));
    }

    private void CheckGameOver()
    {
        if (m_IsGameOver) return;
        if (m_Spawnners.Count < 1 && m_EntityLoadingList.Count < 1 && m_Enemies.Count < 1)
        {
            m_IsGameOver = true;
            var eParms = RefParams.Create();
            eParms.Set<VarBoolean>("IsWin", true);
            GF.Event.Fire(GameplayEventArgs.EventId, GameplayEventArgs.Create(GameplayEventType.GameOver, eParms));
        }
    }

    private void OnShowEntitySuccess(object sender, GameEventArgs e)
    {
        var eArgs = e as ShowEntitySuccessEventArgs;
        int entityId = eArgs.Entity.Id;
        if (m_EntityLoadingList.Contains(entityId))
        {
            m_Enemies.Add(entityId, eArgs.Entity.Logic as CombatUnitEntity);
            m_EntityLoadingList.Remove(entityId);
        }
    }


    private void OnHideEntityComplete(object sender, GameEventArgs e)
    {
        var eArgs = e as HideEntityCompleteEventArgs;
        int entityId = eArgs.EntityId;
        if (m_Enemies.ContainsKey(entityId))
        {
            m_Enemies.Remove(entityId);
        }
        else if (m_EntityLoadingList.Contains(entityId))
        {
            m_EntityLoadingList.Remove(entityId);
        }
        //暂时注释
        //CheckGameOver();
        CheckGameOverNew();
    }

    private void OnRemoveEnemy(object sender, GameEventArgs e)
    {
         var eArgs = e as NotificationDeletionEnemyEventArgs;
         if (eArgs.EventType == GFNotificationDeletionEnemy.killEnemy)
         {
             m_AIEnemyCarEntityDic.Remove(eArgs.EnemyID);
         }
    }
    
    //通过隐藏实体回调检测敌人对象列表里的数量是不是小于等于0是的话则游戏结束
    private void CheckGameOverNew()
    {
        if (m_IsGameOver) return;
        if (m_AIEnemyCarEntityDic.Count < 1)
        {
            GF.LogInfo("输出敌人数量："+m_AIEnemyCarEntityDic.Count);
            m_IsGameOver = true;
            GF.LogInfo("调用了设置游戏结束");
            var eParms = RefParams.Create();
            eParms.Set<VarBoolean>("IsWin", true);
            GF.Event.Fire(GameplayEventArgs.EventId, GameplayEventArgs.Create(GameplayEventType.GameOver, eParms));
        }
    }
    
}
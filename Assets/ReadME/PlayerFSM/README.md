# PlayerFSM 使用说明

## 1. 简介

这套 `PlayerFSM` 是一套基于 `ScriptableObject` 的玩家有限状态机系统，包含两部分：

- 运行时状态机
  - 负责状态切换、状态逻辑执行、动画播放、输入读取、攻击触发
- 编辑器可视化工具
  - 负责在 Unity 编辑器中直接创建状态、连接状态、配置转换条件、实时保存到对应的 `SO` 资产

这套方案的特点是：

- 状态、条件、转换都拆成独立 `SO`
- 动画播放不依赖 Animator 参数切换
- Animator 状态机内部不需要配置状态连线
- 可通过可视化界面直接编辑状态图
- 新增和删除节点、连线时，外部 `.asset` 文件会同步更新

---

## 2. 目录结构

### 运行时代码

- `Assets/Scripts/PlayerFSM/Core`
  - 核心状态机逻辑
- `Assets/Scripts/PlayerFSM/States`
  - 具体状态实现
- `Assets/Scripts/PlayerFSM/Conditions`
  - 转换条件实现

### 编辑器代码

- `Assets/Editor/PlayerFSM`
  - 状态图可视化编辑器
  - 自动布线工具
  - 图浏览器和辅助工具

### 配置资源

- `Assets/Settings/PlayerFSM/PlayerFsmGraph.asset`
  - 状态图主资源
- `Assets/Settings/PlayerFSM/States`
  - 状态资源
- `Assets/Settings/PlayerFSM/Transitions`
  - 转换资源
- `Assets/Settings/PlayerFSM/Conditions`
  - 条件资源

---

## 3. 核心概念

### 3.1 FsmGraphSO

`FsmGraphSO` 是整个状态图的入口，主要包含：

- `initialState`
  - 初始状态
- `states`
  - 图中所有状态
- `transitions`
  - 图中所有转换边

运行时 `PlayerFsm` 会从这个图里取当前状态的所有 outgoing transition，逐个判断是否满足切换条件。

### 3.2 StateSO

`StateSO` 是所有状态的基类，负责：

- 进入状态时播放动画
- 每帧执行状态逻辑
- 退出状态时收尾
- 保存编辑器节点位置

常用字段：

- `displayName`
  - 编辑器中显示名称
- `editorPosition`
  - 节点位置
- `animStateName`
  - 进入状态时要播放的动画名
- `animLayer`
  - Animator Layer
- `crossFade`
  - 是否使用 CrossFade
- `transitionDuration`
  - 动画切换时间

### 3.3 TransitionLinkSO

`TransitionLinkSO` 表示一条状态转换边，包含：

- `from`
  - 起始状态
- `to`
  - 目标状态
- `logic`
  - 条件组合方式，支持 `All` / `Any`
- `conditions`
  - 条件列表

### 3.4 ConditionSO

`ConditionSO` 是转换条件基类，用于决定当前边是否满足切换条件。

当前内置条件包括：

- `InputPressedConditionSO`
  - 检测攻击、移动、冲刺输入
- `MoveBelowThresholdConditionSO`
  - 检测移动输入是否小于阈值
- `AnimNormalizedTimeConditionSO`
  - 检测动画播放归一化时间
- `TimerConditionSO`
  - 检测计时器是否到时

---

## 4. 运行时结构

运行时入口脚本是：

- `Assets/Scripts/PlayerFSM/Core/PlayerFsm.cs`

它负责：

- 读取输入
- 保存当前状态
- 每帧调用当前状态的 `OnUpdate`
- 评估当前状态可用的所有转换边
- 满足条件时调用 `SetState`

运行流程如下：

1. `Start()` 时进入 `graph.initialState`
2. `Update()` 中读取输入
3. 执行当前状态逻辑
4. 遍历当前状态的所有转换边
5. 第一条满足条件的边触发切换

注意：

- 多条边同时满足时，以 `graph.transitions` 中的顺序为准
- 动画切换由代码直接控制，不走 Animator 状态参数驱动的状态机切换

---

## 5. 内置状态说明

### 5.1 IdleStateSO

空闲状态，默认只负责播放待机动画。

### 5.2 MoveStateSO

移动状态负责：

- 根据玩家输入做位移
- 根据朝向翻转角色
- 在 Walk / Run 动画之间切换
- 使用动画曲线或备用曲线修正移动速度

相关字段：

- `baseSpeed`
  - 基础移动速度
- `sprintMultiplier`
  - 冲刺倍率
- `useAnimatorCurve`
  - 是否尝试从 Animator 的 Float 参数中读取速度曲线
- `speedCurveParameter`
  - Animator Float 参数名
- `fallbackSpeedCurve`
  - 如果 Animator 中没有这个参数，则使用该曲线
- `walkAnim`
  - 行走动画
- `runAnim`
  - 跑步动画

说明：

- 如果 Animator 里不存在 `speedCurveParameter` 对应的 Float 参数，会自动回退到 `fallbackSpeedCurve`
- 这不会影响状态切换，只影响位移倍率计算

### 5.3 AttackStateSO

攻击状态负责：

- 播放攻击动画
- 在动画到达指定归一化时间时调用技能系统执行攻击

相关字段：

- `skillId`
  - 技能 ID
- `triggerAtNormalizedTime`
  - 触发攻击逻辑的动画归一化时间

---

## 6. 技能系统接入

状态机通过接口接入技能系统：

- `Assets/Scripts/PlayerFSM/Core/ICombatSkillSystem.cs`

接口定义：

```csharp
public interface ICombatSkillSystem
{
    void ExecuteAttack(string skillId);
}
```

你自己的战斗系统只要实现这个接口，并挂到 `PlayerFsm.CombatSystemBehaviour` 上即可。

当前项目中提供了一个简单示例：

- `SimpleCombatSkillSystem`

它只会在控制台打印执行攻击的日志。

---

## 7. 动画使用规则

### 7.1 状态动画播放方式

状态进入时由 `StateSO.OnEnter()` 自动调用：

- `Animator.CrossFadeInFixedTime(...)`
  或
- `Animator.Play(...)`

具体使用哪种方式由状态自身字段控制。

### 7.2 不使用 Animator 参数切状态

本系统要求：

- 不依赖 Animator 参数来切换状态
- 不依赖 Animator Controller 里的状态连线

也就是说：

- Animator 主要负责播放指定名字的动画
- 真正的状态流转由 `PlayerFSM` 控制

### 7.3 Animator 的作用

Animator 在这套方案里主要承担：

- 播放具体动画
- 提供动画归一化时间
- 可选地提供某些 Float 曲线数据

---

## 8. 如何在场景中使用

### 8.1 挂载组件

在玩家对象上挂载：

- `PlayerFsm`
- `Animator`
- 你的战斗系统脚本（需实现 `ICombatSkillSystem`）

### 8.2 配置 PlayerFsm

需要配置：

- `graph`
  - 指向 `PlayerFsmGraph.asset`
- `Animator`
  - 玩家自身 Animator
- `Character`
  - 实际需要移动和翻转的 Transform
- `inputActions`
  - 输入配置资产
- `moveActionName`
  - 移动输入 Action 名
- `attackActionName`
  - 攻击输入 Action 名
- `sprintActionName`
  - 冲刺输入 Action 名
- `CombatSystemBehaviour`
  - 实现了 `ICombatSkillSystem` 的组件

### 8.3 快速自动配置

可以使用菜单：

- `Tools / Player FSM / Create Defaults + Auto Wire`
- `Tools / Player FSM / Auto Wire & Bind`

它们会尝试：

- 创建默认状态、条件、转换资源
- 绑定 `PlayerFsmGraph`
- 给场景中名为 `Player` 的对象挂上 `PlayerFsm`

---

## 9. 可视化编辑器使用方法

打开菜单：

- `Tools / Player FSM / Graph Editor`

### 9.1 打开图

窗口顶部有一个 `Graph` 对象槽：

- 直接把 `PlayerFsmGraph.asset` 拖进去即可

不需要再使用文件路径方式加载。

### 9.2 新建状态

有两种方式：

- 点击顶部 `New State`
- 在画布空白处右键，选择 `Create State`

系统会自动：

- 在 `States` 文件夹下创建对应状态 `.asset`
- 加入当前图的 `states`
- 在画布中生成节点

### 9.3 连接状态

从一个节点输出口拖到另一个节点输入口即可。

系统会自动：

- 在 `Transitions` 文件夹下创建对应转换 `.asset`
- 写入 `from` / `to`
- 加入当前图的 `transitions`

### 9.4 编辑转换条件

选中一条边后，右侧 Inspector 可编辑：

- `Logic`
- 条件列表

支持：

- 添加条件槽
- 引用已有条件资产
- 直接创建新的条件资产

### 9.5 删除节点和边

删除后会同步：

- 从图资源中移除引用
- 删除对应 `.asset`

注意：

- 删除状态时，会一并删除所有与该状态相关联的转换边

---

## 10. 直线边渲染说明

为了减少复杂状态图中连线混乱的问题，当前图编辑器中的边已经改为：

- 自定义直线渲染
- 条件标签显示在连线中间
- 选中边时高亮显示

这部分只影响编辑器显示，不影响运行时 FSM 逻辑。

---

## 11. 当前默认示例状态图

当前默认资源中包含以下状态：

- `Idle`
- `Move`
- `Attack`

默认转换关系：

- `Idle -> Move`
  - 条件：`MoveOn`
- `Move -> Idle`
  - 条件：`MoveBelow`
- `Idle -> Attack`
  - 条件：`AttackPress`
- `Move -> Attack`
  - 条件：`AttackPress`
- `Attack -> Idle`
  - 条件：`AttackEnd`

---

## 12. 新增自定义状态

新增一个状态的推荐方式：

1. 在 `Assets/Scripts/PlayerFSM/States` 下新建一个继承 `StateSO` 的类
2. 实现 `OnEnter` / `OnUpdate` / `OnExit`
3. 加上 `CreateAssetMenu`
4. 回到 Graph Editor 中新建该状态

示例：

```csharp
using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "JumpState", menuName = "PlayerFSM / States / Jump")]
    public class JumpStateSO : StateSO
    {
        public override void OnEnter(PlayerFsm fsm)
        {
            base.OnEnter(fsm);
        }

        public override void OnUpdate(PlayerFsm fsm, float dt)
        {
            // 在这里写跳跃逻辑
        }

        public override void OnExit(PlayerFsm fsm)
        {
        }
    }
}
```

---

## 13. 新增自定义条件

新增一个条件的方式：

1. 在 `Assets/Scripts/PlayerFSM/Conditions` 下新建一个继承 `ConditionSO` 的类
2. 实现 `Evaluate`
3. 加上 `CreateAssetMenu`
4. 回到 Graph Editor 中把该条件加到某条边上

示例：

```csharp
using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "HealthBelowCondition", menuName = "PlayerFSM / Conditions / HealthBelow")]
    public class HealthBelowConditionSO : ConditionSO
    {
        public float threshold = 30f;

        public override bool Evaluate(PlayerFsm fsm)
        {
            return false;
        }
    }
}
```

---

## 14. 常见问题

### 14.1 打开 Graph Editor 后看不到节点

先确认：

- `Graph` 是否已经拖入
- `PlayerFsmGraph.asset` 的 `states` 是否有内容

然后点击：

- `Frame All`

### 14.2 连线创建了但没有条件

这是正常的。

新建边后，需要：

- 选中该边
- 在右侧 Inspector 中添加条件

### 14.3 MoveState 报 Animator 参数不存在

如果日志提示：

- `Parameter 'MoveSpeedCurve' does not exist`

说明 Animator 里没有对应的 Float 参数。

解决方式：

- 在 `MoveStateSO` 里把 `useAnimatorCurve` 关掉
  或
- 在 Animator 中增加同名 Float 参数

当前代码已经做了保护：

- 如果找不到该参数，会自动退回到 `fallbackSpeedCurve`

### 14.4 图编辑器改了数据又被自动覆盖

当前已经关闭了原先的启动自动重写逻辑。

如果你想重新同步默认资源，请手动使用：

- `Tools / Player FSM / Auto Wire & Bind`

---

## 15. 推荐使用流程

推荐日常工作流如下：

1. 在 `Graph Editor` 中编辑状态图
2. 为每个状态设置动画名和逻辑参数
3. 为每条边设置条件
4. 将 `PlayerFsmGraph.asset` 挂到玩家的 `PlayerFsm` 上
5. 进入 PlayMode 验证状态切换
6. 如果需要扩展，再补新的状态类和条件类

---

## 16. 相关文件

- `Assets/Scripts/PlayerFSM/Core/PlayerFsm.cs`
- `Assets/Scripts/PlayerFSM/Core/StateSO.cs`
- `Assets/Scripts/PlayerFSM/Core/TransitionLinkSO.cs`
- `Assets/Scripts/PlayerFSM/Core/ConditionSO.cs`
- `Assets/Editor/PlayerFSM/FsmGraphViewEditor.cs`
- `Assets/Editor/PlayerFSM/FsmStateNode.cs`
- `Assets/Settings/PlayerFSM/PlayerFsmGraph.asset`

如果后续还要补：

- 更像 Animator 的 Inspector 交互
- 平行线自动错位
- 边箭头
- 更完整的状态模板

可以继续在这套基础上扩展。

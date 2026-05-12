# 战斗系统 README

本文档记录当前项目中的战斗构筑系统。它不是单一攻击脚本，而是一套由玩家 FSM、CombatCoordinator、武器、核心、技能 Timeline、伤害/Buff、分身能力共同组成的运行时框架。

## 设计目标

- 战斗状态和玩家移动 FSM 分离：攻击/技能期间由 `CombatCoordinator` 接管，FSM 暂停工作，结束后回到移动状态。
- 技能数据 SO 化：每个攻击、技能、位移、防御都可以做成 `CombatSkillDefinitionSO`，方便后续技能编辑器生产。
- 武器决定普攻结构和特殊入口：不同武器可以有不同普攻树、冲刺攻击、上挑、下砸、方向特殊攻击。
- 核心决定战斗规则：类似狩猎风格，不只改数值，也能影响闪避、取消、空地节奏、资源、技能槽数量。
- 技能 Timeline 化：动画、位移、打击框、投射物、特效、音效、镜头、取消窗口、派生窗口都按时间片执行。
- 玩家和敌人尽量共用伤害接口、技能数据和 Buff 接口。

## 主要目录

- `Assets/Scripts/Combat/Core`
  战斗入口、状态机、核心运行时。

- `Assets/Scripts/Combat/Data`
  技能定义和 Timeline 数据。

- `Assets/Scripts/Combat/Runtime`
  技能执行、伤害接口、打击框、对象池、投射物、测试敌人。

- `Assets/Scripts/Combat/Weapons`
  武器运行时控制器。

- `Assets/Scripts/GameData/Combat`
  武器 SO、核心 SO、武器/核心枚举和配置结构。

- `Assets/Editor/Combat`
  技能编辑器、技能预览窗口、测试敌人创建菜单。

- `Assets/Scripts/PlayerFSM/Abilities`
  能力系统和分身能力。

## 运行时组件关系

玩家对象建议挂这些组件：

- `PlayerFsm`
  负责待机、移动、跳跃、下落、闪避等基础状态和输入读取。

- `CombatCoordinator`
  负责攻击输入、核心技能输入、战斗状态、技能执行、命中、派生、取消、攻击结束恢复。

- `WeaponRuntimeController`
  持有当前武器，向 `CombatCoordinator` 提供当前武器入口技能和连段状态。

- `CoreRuntimeController`
  持有当前核心，处理核心资源、核心技能槽、取消权限覆盖、闪避规则、空地规则、风险收益。

- `PlayerAbilityController`
  能力系统入口，判断能力是否解锁。

- `PlayerCloneAbilityController`
  处理 RT 组合键分身能力。

- `RuntimeStatsComponent`
  提供攻击力、生命等运行时数值。

- `BuffController`
  接收技能 Timeline 中的自身 Buff，支持无敌等控制标记。

推荐调用顺序已经接在 `PlayerFsm` 中：先处理能力输入，再处理核心技能输入，再处理攻击输入，最后让 `CombatCoordinator.ManualUpdate` 推进战斗。

## CombatCoordinator

`CombatCoordinator` 是战斗系统的中枢。

它负责：

- 接收普通攻击输入。
- 根据地面/空中、冲刺、方向输入，向武器请求入口技能。
- 接收核心技能槽输入。
- 技能开始时暂停玩家 FSM。
- 技能执行中推进 Timeline。
- 处理派生窗口、取消窗口、输入缓冲。
- 处理命中确认和核心资源通知。
- 技能结束后根据地面输入回到 Move 或 Idle，空中回到 Fall。
- 空中攻击结束后锁住空中重复攻击，直到落地或被核心规则刷新。

普通攻击入口选择规则：

- 地面普通攻击使用 `WeaponAttackSlot.PrimaryGround`。
- 空中普通攻击使用 `WeaponAttackSlot.PrimaryAir`。
- 地面冲刺时攻击优先使用 `DashAttack`。
- 按住上方向攻击优先使用 `SpecialUp`，没有则地面回退 `Launcher`。
- 按住下方向攻击优先使用 `SpecialDown`，没有则空中回退 `Slam`。
- 横向方向攻击可使用 `SpecialNeutral`。
- 特殊槽没有配置技能时，会显式回退到当前地面/空中的普通攻击。
- 普攻也没有配置时，才会使用 `CombatCoordinator.defaultSkillId` 兜底。

## 武器系统

武器系统由三层数据组成。

`WeaponDefinitionSO`：

- 基础面板：攻击、倍率、暴击、攻速、消耗、削韧、范围、击退。
- 标签：近战、远程、空战、蓄力、弹反、防御、机动、重武器。
- 入口技能：地面普攻、空中普攻、冲刺攻击、闪避追击、格挡反击、蓄力攻击、上挑、下砸、特殊中立、特殊上、特殊下。
- 武器资源交互：命中、击杀、闪避、完美闪避、防御、自然恢复。
- 被动规则和额外属性修正。

`WeaponMoveSetSO`：

- 地面普攻 A1/A2/A3/A4。
- 空中入口、空中循环、空中终结。
- 上挑、下砸、落地追击、特殊中立、特殊上、特殊下。
- 入口技能列表，用于扩展更多 `WeaponAttackSlot`。
- 连段派生不再放在武器 MoveSet 里，统一放到技能 Timeline 的 `DerivationWindowSkillClip`。

`WeaponStatProfileSO`：

- 用于承载武器节奏和数值修正。
- 包含伤害系数、削韧、前摇/后摇、命中停顿、空地节奏参数等扩展方向。

配置建议：

1. 先创建每段攻击的 `CombatSkillDefinitionSO`。
2. 每个技能绑定自己的 `SkillTimelineSO`。
3. 在 `WeaponMoveSetSO` 中配置地面普攻第一段、空中普攻第一段、上挑/下砸/特殊攻击。
4. 在 `WeaponDefinitionSO` 中引用 `WeaponMoveSetSO`。
5. 把 `WeaponDefinitionSO` 配到玩家的 `WeaponRuntimeController`。

## 核心系统

核心系统由 `CoreStyleDefinitionSO` 和 `CoreRuntimeController` 组成。

核心可以配置：

- 独立资源条：上限、初始值、每秒恢复。
- 资源获取规则：技能开始、技能结束、命中确认、地面技能开始、空中技能开始、闪避、完美闪避、防御。
- 技能消耗规则：使用技能自身消耗、固定额外消耗、资源不足时是否禁止释放。
- 技能槽：每个核心可以定义不同数量的技能槽。
- 技能槽输入：`Skill1/2/3/4`、`FaceY`、`FaceYNeutral`、`FaceYUp`、`FaceYDown`、`Finisher`、`RT+RB`、`RT+X`、`RT+Y`。
- 允许装备的技能类型：攻击、位移、防御、控制、资源转换、终结技、核心触发器。
- 允许装备的技能定位：高伤爆发、位移突进、控场压制、无敌保命、资源转换、连段收尾、Build 核心触发器。
- 取消规则覆盖：核心可以在资源达标时覆盖武器取消权限。
- 空地规则：是否允许落地前重复空中攻击。
- 闪避规则：是否允许空中闪避、空中闪避次数、闪避后刷新普攻、闪避后刷新空中攻击锁、完美闪避强化下一击。
- 风险收益规则：造成伤害倍率、受到伤害倍率、资源获取倍率。
- 特殊派生规则：核心可以给技能追加特殊派生。
- 被动规则：核心可以附加属性修正。

武士道风格可以这样配：

- `allowAerialDodge = true`
- `maxAerialDodgesBeforeLanding = 1`
- `refreshPrimaryAttackOnDodge = true`
- `refreshAerialAttackLockOnDodge = true`
- `empowerNextAttackOnDodge = true`
- `onlyEmpowerOnPerfectDodge = true`
- 技能槽配置 3 个 Y 系技能：`FaceYNeutral`、`FaceYDown`、`FaceYUp`

## 技能系统

每个攻击或技能都是 `CombatSkillDefinitionSO`。

技能定义包含：

- 身份信息：技能 ID、显示名、描述、图标、玩家/敌人使用限制。
- 技能类型：攻击、位移、防御、控制、资源转换、终结技、核心触发器。
- 战斗定位标签：高伤爆发、突进、控场、无敌、资源转换、连段收尾、Build 触发器。
- 动画表现：Animator 状态名、动画层、过渡时间、预览动画。
- Timeline 引用：`SkillTimelineSO`。
- 位移配置：Animator Curve 或自定义曲线。
- 时间参数：持续时间、前摇、生效时间、后摇、冷却。
- 战斗参数：伤害倍率、削韧、资源消耗、地面限定、空中限定、武器标签需求。
- 进入条件：条件组、允许前置技能。
- 派生规则：旧数据路径，当前更推荐 Timeline 派生窗口。
- 收招规则：技能结束后满足条件可自动进入下一个技能。

技能进入判断会检查：

- 是否地面/空中限定。
- 是否满足武器标签需求。
- 是否满足 `previousSkills`。
- 是否满足 `enterConditions`。
- 核心技能是否有足够资源。
- 空中攻击是否被锁住。

## SkillTimelineSO

Timeline 是技能真正运行的内容。

当前轨道：

- `AnimationSkillClip`
  在指定时间播放 Animator 状态。

- `MovementSkillClip`
  通过 Animator Curve 或自定义曲线产生位移，支持 X/Y 位移和朝向镜像。

- `HitSkillClip`
  打击框，支持 Box、Sphere、Capsule，支持瞬时攻击和持续攻击。

- `ProjectileSkillClip`
  投射物生成，支持向前、朝目标、固定方向。

- `VfxSkillClip`
  特效生成，支持世界、跟随释放者、骨骼挂点。

- `SfxSkillClip`
  音效播放。

- `CameraShakeSkillClip`
  镜头震动事件。

- `SelfBuffSkillClip`
  给自己添加或移除 Buff。

- `CoreResourceSkillClip`
  修改核心资源。

- `CancelWindowSkillClip`
  指定当前时间段允许闪避、技能、格挡等取消。

- `DerivationWindowSkillClip`
  指定当前时间段可派生到哪些技能，可限制命中确认、地面、空中。

运行时由 `SkillExecutor` 推进 Timeline，并调用 `CombatCoordinator` 执行动画、位移、打击、投射物、特效、音效、Buff 和资源变化。

## 派生、取消和输入缓冲

派生推荐配置在 `SkillTimelineSO.derivationWindowClips`。

一次普攻连段的典型做法：

1. A1 的 Timeline 里添加 `DerivationWindowSkillClip`。
2. 窗口时间放在后摇或你想允许接招的位置。
3. `nextSkills` 填 A2。
4. 如果需要命中才可接，勾选 `requiresHitConfirm`。
5. 如果只允许地面接，勾选 `requiresGrounded`。
6. A2 再用同样方式接 A3。

输入缓冲逻辑：

- 玩家在技能中按攻击，如果当前正处于派生窗口，会立即排队下一技能。
- 如果还没到窗口，会进入短时间缓冲。
- 之后进入窗口时会自动消费缓冲。
- 已经排队了一个派生技能时，不会因为连按多次直接跳过二段。

取消窗口推荐配置在 `SkillTimelineSO.cancelWindowClips`。

取消权限：

- `None`：不能取消。
- `DodgeOnly`：只允许闪避。
- `GuardOnly`：只允许防御。
- `SkillOnly`：只允许技能取消。
- `DodgeAndSkill`：允许闪避和技能。
- `DodgeGuardAndSkill`：允许闪避、防御、技能。
- `Free`：自由取消。

## 收招规则 RecoveryRule

`CombatSkillDefinitionSO.recoveryRules` 用于技能自然结束时的自动跳转。

适用场景：

- 空中攻击结束后，如果满足某条件，自动进入下落攻击或落地追击。
- 某技能命中后自动进入收尾。
- 技能后摇结束时根据条件进入另一个技能，否则保持战斗态或释放回 FSM。

RecoveryRule 可以配置：

- 下一个技能。
- 是否需要命中确认。
- 是否需要地面。
- 是否需要空中。
- 额外条件组。

注意：如果希望“条件不满足就不要回 FSM，而是继续停留在当前技能等待”，需要保证技能结束逻辑和对应 RecoveryRule 的等待语义一致；当前系统已经支持收招阶段判断，但具体技能要把规则配在技能本体上。

## 空中攻击节奏

当前默认规则：

- 空中攻击开始后会锁住空中重复攻击。
- 技能结束后如果还在空中，回到 Fall。
- 落地后自动清除空中攻击锁。
- 如果核心允许 `allowRepeatedAerialAttackBeforeLanding`，可以在落地前重复空中攻击。
- 核心闪避规则可以在闪避后刷新空中攻击锁。

实现“空中普攻三段，最后一段打落敌人”的建议：

- 空中 A1、A2、A3 都做成单独 `CombatSkillDefinitionSO`。
- A1 Timeline 派生窗口接 A2。
- A2 Timeline 派生窗口接 A3。
- A3 的 HitClip 设置更高伤害/削韧，并通过后续敌人受击系统处理打落。
- A3 结束后不再给派生窗口，玩家自然下落。

## 技能编辑器

打开方式：

- `Window/Combat/Skill Editor`
- `Window/Combat/Skill Preview`

技能编辑器当前支持：

- 选择 `CombatSkillDefinitionSO`。
- 创建并绑定 `SkillTimelineSO`。
- 编辑进入条件和前置技能。
- 编辑 RecoveryRule。
- 编辑 Hit、Projectile、VFX、SFX、Camera、Derivation、Cancel、Movement Clip。
- 时间轴可视化显示多轨道条块。
- 时间轴条块拖拽修改 `startTime`。
- 帧刻度和帧吸附。
- `startTime/duration` 同时按秒和帧理解。
- Preview Time 和 Preview Frame 对照动画帧。
- 预览打击框、特效、投射物、音效、镜头事件。
- 预览需要场景里有 `CombatSkillPreviewAnchor`，并给技能配置 `previewAnimationClip`。

常见使用流程：

1. 创建 `CombatSkillDefinitionSO`。
2. 打开 `Window/Combat/Skill Editor`。
3. 选择技能。
4. 如果没有 Timeline，点击创建 Timeline。
5. 配置 `previewAnimationClip`。
6. 场景里选择或创建带 `CombatSkillPreviewAnchor` 的对象。
7. 拖动 Preview Time 对照动画帧。
8. 在对应帧插入 HitClip、VFX、SFX、MovementClip。
9. 保存资源。
10. 把技能配置到武器或核心技能槽。

如果拖动 Preview Time 看不到动画，优先检查：

- 技能是否配置了 `previewAnimationClip`。
- 场景是否有 `CombatSkillPreviewAnchor`。
- Anchor 的预览对象是否有 Animator。
- 动画是否可被编辑器采样。
- Timeline 长度是否覆盖当前帧。

## 伤害和 Buff

伤害接口：

- `DamageInfo`
  记录伤害值、伤害类型、来源、释放者、命中点、命中方向、削韧。

- `DamageResult`
  记录是否生效、最终伤害、是否击杀。

- `IDamageable`
  统一受伤接口。

- `CombatHurtbox`
  当前通用受击盒实现，负责阵营过滤、自身过滤、无敌 Buff 过滤，并调用 `RuntimeStatsComponent.TakeDamage`。

Buff 接口：

- `ICombatBuffReceiver`
  添加、移除、查询 Buff 和控制标记。

- `BuffController`
  当前项目里的 Buff 运行时组件。

测试敌人：

- 菜单：`GameObject/Combat/Create Test Enemy`
- 会创建带 `CombatHurtbox`、`CombatTestEnemy`、`BuffController`、测试 Stats 的敌人。
- 被击中时会打印伤害日志并闪烁。

攻击敌人没有反应时优先检查：

- 敌人是否有 `CombatHurtbox` 和 RuntimeStats。
- 敌人 Layer 是否包含在 HitClip 的 `targetLayers`。
- 玩家和敌人的 `CombatTeam` 是否不是同队。
- HitClip 的时间是否真的触发。
- HitClip 的位置、大小、朝向是否覆盖敌人。
- 技能是否绑定了 Timeline，而不是只配了旧的默认 ID。
- `CombatCoordinator` 是否开启 Gizmos 或命中诊断日志。

## 对象池、投射物和表现生成

对象池相关：

- `PooledObject`
- `PooledLifetime`
- `PooledObjectSpawner`

投射物相关：

- `ProjectileRuntimeContext`
- `SimplePooledProjectile`

Timeline 中的 `ProjectileSkillClip` 和 `VfxSkillClip` 会通过运行时生成对象。推荐投射物和特效 Prefab 挂 `PooledObject`，生命周期用 `PooledLifetime` 或 Clip 的 lifetime 管理。

## 分身能力

分身属于能力系统，不属于核心技能槽，但会和战斗系统联动。

需要组件：

- `PlayerAbilityController`
- `PlayerCloneAbilityController`
- 可选：分身 Prefab 上挂 `PlayerCloneRuntime`

能力 ID：

- `CloneDecoyDash`
- `CloneAssaultSwap`
- `CloneMimic`

输入：

- `RT+RB`
  原地留下诱饵分身，诱饵带 `PlayerCloneDecoyTarget` 标记，玩家向前冲刺。之后再次按 RT 会与诱饵互换位置，诱饵不会立刻消失。

- `RT+X`
  向最近敌人释放攻击分身。再次按 RT 会与攻击分身互换位置，攻击分身随后释放。

- `RT+Y`
  在敌人身后释放模仿分身，持续一段时间。玩家开始技能时，模仿分身会根据玩家技能伤害倍率造成一次范围伤害。

注意：

- 能力必须在 `AbilityLoadoutSO` 或运行时解锁。
- `PlayerAbilityController.TryHandleAbilityInput` 会优先于核心技能和普通攻击处理。
- 分身 Prefab 上已有 Collider/Rigidbody 时，运行时代码默认不会覆盖你已经调好的数值。

## 配置一个可用原型

目标：剑 + 武士道 + 三个 Y 技能。

步骤：

1. 创建地面普攻 A1/A2/A3/A4/A5 对应技能 SO。
2. 为每段普攻创建 Timeline，配置动画、位移、打击帧、派生窗口。
3. 创建空中 A1/A2/A3，对 A1/A2 配派生窗口，A3 不再派生。
4. 创建上挑、下砸、冲刺攻击技能。
5. 创建三个核心技能：Y 高速斩、Y+下 水平斩击线、Y+上 残影剑痕。
6. 创建 `WeaponMoveSetSO`，配置地面普攻入口、空中普攻入口、Launcher、Slam、DashAttack 或特殊入口。
7. 创建 `WeaponDefinitionSO`，引用 MoveSet。
8. 创建 `CoreStyleDefinitionSO`，配置武士道规则和 3 个技能槽。
9. 在 `CoreStyleDefinitionSO.defaultEquippedSkills` 中给技能槽装入三个技能。
10. 玩家对象挂 `PlayerFsm`、`CombatCoordinator`、`WeaponRuntimeController`、`CoreRuntimeController`、Stats、Buff、Ability。
11. `WeaponRuntimeController` 配当前武器，`CoreRuntimeController` 配当前核心。
12. 创建测试敌人，进入 Play Mode 测试命中、派生和资源。

## 扩展建议

- 新武器优先扩展 `WeaponMoveSetSO` 和 `WeaponDefinitionSO`，不要在 `CombatCoordinator` 里为某把武器写特殊分支。
- 新核心优先扩展 `CoreStyleDefinitionSO`，通过规则影响取消、闪避、空地节奏、资源和技能槽。
- 新技能优先做成 `CombatSkillDefinitionSO + SkillTimelineSO`，不要把技能逻辑写死在输入里。
- 新命中效果优先扩展 `HitSkillClip` 或伤害/Buff 接口。
- 新表现事件优先添加 Timeline Clip 类型，让技能编辑器可以编辑和预览。

## 当前已知边界

- 技能编辑器已经有第一版 Timeline 可视化和预览，但还不是完整商业级 Timeline 工具。
- 部分 Inspector 中文在源码读取时可能显示为乱码，这是编码显示问题，Unity Inspector 中以实际项目显示为准。
- 敌人 AI、受击硬直、击飞/打落、霸体、弹反判定还可以继续扩展。
- 防御机制和完美闪避触发点已有核心规则入口，但完整输入/判定体验还需要继续打磨。
- 多武器资源条、UI 显示、技能装备界面还未形成完整前端。

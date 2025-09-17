# Система боя

## Обзор пайплайна
1) **Поиск цели** — `CombatAgent` через `UnitRegistry`, учитывая `AttackOccupancy` (лимит атакующих).
2) **Подход/орбита** — руление: separation/bypass/cohesion, LOD-дискретизация выборок.
3) **Старт атаки** — `PlayAttack()` в `UnitAnimator`, фиксируем урон/цель на фазу удара.
4) **Момент удара** — по Animation Event `OnAttackHit` (или fallback по таймеру).
5) **Фильтры контакта** — фронт-сектор (front arc), LOS (raycast), дистанция.
6) **Применение урона** — `DamageSystem.Apply(...)` → либо сразу, либо через `DamageResolver`.
7) **Попап/события** — спавн над целью и вызов `DamageSystem.OnApplied`.
8) **Смерть/ранг** — если цель умерла: победная анимация, `AddKillAndCheckRank()`.

Диаграммы:
- Последовательность атаки — `Docs/Diagrams/sequence-combat.mmd`
- Основные классы — `Docs/Diagrams/classes-core.mmd`

## Таргетинг и лимит атакующих
- `AttackOccupancy`: `Acquire(target)` при старте удара, `Release(target)` при смене/смерти/disable.
- Если **все живые враги “переполнены”**, действует **мягкий** штраф (агенты всё равно распределятся).
- Если есть **свободные цели**, агента жёстко отталкивает от переполненных (HardBlockPenalty).

## Контакт удара (front-arc / LOS)
- Глобальные дефолты лежат в **Resources/CombatContactDefaults.asset** (можно переопределить в агенте).
- **Front Arc** — попадание только в сектор перед атакующим (по XZ).
- **LOS** — нет препятствий между атакующим и целью (персонажи исключены из маски).

## DamageSystem (коротко)
- Уклонение (Evasion) цели → полный промах.
- Редукция: `Physical` через Armor (с учётом Penetration атакующего), `Magic` через MagicResist, `True` — без редукции.
- Блок (BlockChance) — частичное поглощение (по умолчанию 50%).
- События: `OnApplied`, `OnHealed`.

## Рекомендации отладки
- Включай `DebugDraw` в `CombatAgent.Attack` — видно фронт-сектор и LOS.
- Поставь `DamageResolver.UseQueue = true` на пустой GameObject в сцене, чтобы урон применялся в конце кадра стабильно.
- Проверяй `CharactersMask` у руления и `LOSMask` у контакта — персонажи не должны попадать в LOS-маску.

---

## CombatAgent — разбор partial файлов

| Partial | Ответственность | Ключевые параметры | Взаимодействия |
|---|---|---|---|
| `CombatAgent` (core) | цикл `Update`, выбор между подходом и орбитой, вызовы остальных подсистем | `TurnSpeed`, `SlowRange`, `WalkCoef` | читает `UnitRuntimeStats`, вызывает Steering/LOD/Retarget/Attack |
| `Animation` | сглаживание `MoveSpeed` и проигрывание клипов | `_blendSpeed` | `UnitAnimator` |
| `Attack` | тайминги удара, фронт-сектор, LOS, крит/промах, запись в `DamageSystem`, учёт `AttackOccupancy` | `HitTimeNormalized`, `ExtraHitRange`, контактные дефолты | `DamageSystem`, `DamageResolver`, `AttackOccupancy`, `CombatContactDefaults` |
| `Grounding` | «прилипание к земле» и тюнинг `CharacterController` | `GroundSnapMax`, `GroundRayHeight` | `CharacterController`, Raycast |
| `LOD` | частота выборок сенсоров руления с кэшами | `OnscreenPerceptionHz`, `OffscreenPerceptionHz` | читает видимость/дистанции |
| `Retarget` | смена цели при застревании/изоляции | `RetargetAfterStuck`, `IsolationRetargetThreshold` | `UnitRegistry`, `AttackOccupancy` |
| `Steering` | подход, обход, орбита, разлипание, cohesion | `SeparationRadius`, `OrbitSideSpeed` | OverlapSphere, `CharactersMask` |
| `Targeting` | выбор цели с учётом занятости | `MaxAttackersPerTarget` (в core), штрафы | `UnitRegistry`, `AttackOccupancy` |

```mermaid
flowchart LR
  CA_core[CombatAgent core] --> Anim
  CA_core --> Attack
  CA_core --> Grounding
  CA_core --> LOD
  CA_core --> Retarget
  CA_core --> Steering
  CA_core --> Targeting
  Attack --> DS[DamageSystem]
  Attack --> AO[AttackOccupancy]
  Targeting --> AO
  Retarget --> UR[UnitRegistry]
  Steering --> LOD

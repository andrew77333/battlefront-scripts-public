# Обзор архитектуры

## Папки в Assets
- **Scripts/**
    - **Data/** — чистые данные (ScriptableObject + простые классы): `StatBlock`, `UnitArchetype`, `UnitLevelList`, `UnitRankList`, `UnitAnimationProfile`.
    - **Runtime/** — игровая логика в рантайме:
        - **CombatAgent/** — поведение юнита в бою (partial-скрипты: движение, рулинг, атака, LOD, ретаргет).
        - **UI/** — `HealthBarLink`, `DamagePopup`, `Billboard`.
        - Корень Runtime: `Unit`, `UnitRuntimeStats`, `UnitTeam`, `UnitRegistry`, `AttackOccupancy`, `DamageSystem`, `DamageResolver`, сервисы.
    - **AI/** — утилиты ИИ (`SteeringLOD` и т.п.).
    - **Environment/** — утилиты сцены (`GroundAutoLayer`, `GroundSnapOnStart`).
- **Resources/** — то, что грузится через `Resources.Load`:
    - `UI/DamagePopup.prefab`
    - `CombatContactDefaults.asset`

## Главные роли
- **UnitArchetype (SO)** — базовый “чертёж” юнита (префаб, анимации, статы, списки уровней/званий).
- **UnitRuntimeStats (MB)** — живые статы во время игры (HP, модификаторы, события).
- **CombatAgent (MB)** — поведение в бою: подход, орбита, рулинг, выбор цели, атака.
- **DamageSystem (static)** — единая точка урона/лечения, учитывает уклонение/блок/броню/резисты.
- **DamageResolver (MB)** — опциональная очередь урона (строгий порядок применения в конце кадра).

Полезные ссылки:
- Диаграмма классов: `Docs/Diagrams/classes-core.mmd`
- Последовательность атаки: `Docs/Diagrams/sequence-combat.mmd`
- Карта подсистем: `Docs/Diagrams/systems.mmd`

# AI (CombatAgent) — обзор

Этот документ объясняет цикл ИИ, выбор цели, руление (подход/орбита) и атаку.  
Компоненты: `CombatAgent` и его partial’ы, плюс сервисы (`UnitRegistry`, `AttackOccupancy`,
`TeamCohesionService`, `DamageSystem`).

## Главный цикл

В `Update()` агент:
1) тикает таймеры удара (`AttackTimersTick`);
2) валидирует/находит цель (`Targeting`);
3) если далеко — **подходит** к цели с помощью сенсоров (separation/bypass/cohesion) и LOD;
4) если рядом — **орбитирует** с поддержанием радиуса;
5) пытается начать атаку (`AttackTryStart`).

```mermaid
flowchart TD
  Start[Update] --> Timers[AttackTimersTick]
  Timers --> Target{Target valid?}
  Target -- no --> Acquire[FindEnemyConsideringOccupancy]
  Acquire --> Target2{Target found?}
  Target2 -- no --> End
  Target2 -- yes --> Dist{Distance > StopDistance}
  Dist -- yes --> Move[Approach: separation + bypass + cohesion]
  Move --> End
  Dist -- no --> Orbit[Orbit at radius + separation + cohesion]
  Orbit --> Try[AttackTryStart]
  Try --> End[End frame]

# Быстрый старт: первая битва

Цель: за 2–3 минуты получить две схватившиеся группы юнитов.

## 1) Подготовка сцены
- Создай пустой GameObject `GameRoot`.
- Добавь компонент `BattleSpawner` на `GameRoot`.
- Создай 2 пустых объекта `FriendlyCenter` и `EnemyCenter` и выставь их по X, например -8 и +8.

## 2) Префабы
- Укажи префабы в `friendlyPrefab` и `enemyPrefab`. На каждом префабе должны быть:
    - `Unit` (с `UnitArchetype`);
    - `UnitTeam` (Team не критичен — спавнер всё равно проставит);
    - `CharacterController`;
    - `CombatAgent` (+ желателен `UnitAnimator`);
    - `HealthBarLink` (по желанию).
- Если используешь попапы урона — положи префаб `Resources/UI/DamagePopup.prefab`.

## 3) Настройки спавнера
- Сетки: `friendlyRows/Cols` и `enemyRows/Cols` (например 8 × 8).
- `spacing` = 1.6–1.8.
- Привяжи `friendlyCenter` и `enemyCenter`.
- Включи флажок `autoSpawnOnPlay`.

## 4) Пуск
- Нажми Play — армии заспавнятся и начнут бой.
- Если нужно замедлить — понизь `MoveSpeed`/`AttackSpeed` в архетипах.
- Для очереди урона добавь на сцену `DamageResolver` и включи `UseQueue`.

## 5) Частые проблемы
- Попапы не видны — проверь путь `Resources/UI/DamagePopup.prefab` и камеру на Canvas.
- Юниты «ступают по головам» — смотри `CombatAgent.Grounding`: `EnforceControllerTuning=true`.
- Нет анимации — проверь `UnitAnimator` и контроллер аниматора (параметры `MoveSpeed`, триггеры `Attack`, `Death`, `Victory`).

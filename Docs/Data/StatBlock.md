# StatBlock — значения и смысл

## Attack (атака)
- **DamageMin/Max** — базовый урон удара/выстрела.
- **AttackSpeed** — атак в секунду (1.0 = 1 атака/сек).
- **AttackRange** — дальность атаки (м).
- **MissChance** — шанс промаха атакующего [0..1].
- **CritChance / CritMultiplier** — шанс крита и множитель.
- **ArmorPenetrationFlat / ArmorPenetrationPct** — “пробитие” брони атакующего.

## Defense (защита)
- **Health** — максимальное HP.
- **HealthRegen** — реген HP/сек.
- **Armor** — защита от физики.
- **MagicResist** — защита от магии/особого урона.
- **Evasion** — уклонение цели [0..1].
- **BlockChance** — шанс частично поглотить урон [0..1].

## Mobility / Perception
- **MoveSpeed** — м/с.
- **AggroRadius** — с какой дистанции замечаем врагов.
- **StopDistance** — где останавливаемся перед атакой (чуть меньше AttackRange).

## Economy/Meta
- **CostGold / CostCrystal / Supply / ProductionTime** — при желании подключается в экономике.

### Комбинирование
- `Clone()` / `CopyFrom()` — базовые операции.
- `ApplyBonuses(flat, percent)` — сначала flat, затем процентные; потом `Clamp()`.
- `AddPercent()` интерпретирует поля как **долю** (+0.1 = +10%).

# Glossary

**Unit** — объект в сцене; хранит ссылку на `UnitArchetype`, создаёт `UnitRuntimeStats`, слушает смерть.  
**UnitArchetype (SO)** — конфиг: префаб, анимации, базовые статы, ссылки на уровни/звания.  
**StatBlock** — набор числовых статов (урон, броня, скорость, и т.д.).  
**UnitRuntimeStats** — живые статы (HP, события Damaged/Healed/Died, апгрейды ранга/уровня).  
**UnitTeam** — команда (A/B), регистрируется в `UnitRegistry`.  
**UnitRegistry** — списки всех `UnitTeam` по командам (для поиска целей).  
**AttackOccupancy** — счётчик “сколько атакующих уже бьют цель”, чтобы ограничивать толпу.  
**CombatAgent** — логика боя: поиск цели, подход/орбита, руление, атака (partial-файлы).  
**CombatContactDefaults (SO)** — глобальные дефолты фильтров контакта (front arc, LOS).  
**DamageSystem** — фасад урона/лечения: промах, крит, блок, броня/резисты, попапы, события.  
**DamageResolver** — очередь урона/лечения (LateUpdate), чтобы сохранить порядок.  
**DamagePopup** — всплывающие числа урона/тексты, спавнятся через `Resources`.  
**HealthBarLink** — создаёт и обновляет полоску HP над юнитом.  
**TeamCohesionService** — кэш центроидов команд (для ИИ/аналитики).  
**SteeringLOD** — LOD/джиттер обновления “тяжёлых” сенсоров руления.  
**KnockbackReceiver** — приём толчка/оглушения, временно отключает `CombatAgent`.  
**GroundAutoLayer/GroundSnapOnStart** — помощь со слоями/прижимом к земле.  

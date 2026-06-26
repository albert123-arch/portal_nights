# Portal Nights - Dialogue Script RU

Human-readable master script for mission radio lines and objective prompts.

## Data Fields

Each dialogue entry is represented in `Assets/PortalNights/Resources/Dialogues/PortalNights_Dialogue_RU.json` with:

- `id`
- `planet`
- `gameState`
- `trigger`
- `speaker`
- `text`
- `objectiveText`
- `duration`
- `priority`
- `repeatCooldown`
- `voiceClipId`
- `canRepeat`

## Objective Lines

- DEFEND THE CORE
- CLEAR THE AREA
- ACTIVATE THE SPHERE
- DEFEND THE SPHERE
- RESCUE STAFF
- CLOSE RIFTS
- DESTROY THE CORRUPTED SPHERE
- KILL BOTH BOSSES
- RESTORE THE SPHERE
- UNIVERSE COMPLETE

## Planet 1 - Arena Defense

| ID | Trigger | Speaker | Text | Objective |
| --- | --- | --- | --- | --- |
| `p01_intro_001` | intro | Оператор | Командир, портал уже открыт. Держите ядро, пока мы стабилизируем выход. | DEFEND THE CORE |
| `p01_wave10_complete_001` | wave_10_completed | Система | Десятая волна отбита. Энергия портала вышла на рабочий режим. | DEFEND THE CORE |
| `p01_turrets_upgraded_001` | all_turrets_upgraded | Оператор | Все турели усилены. Хорошая работа, оборона держится. | DEFEND THE CORE |
| `p01_portal_ready_001` | portal_ready | Оператор | Переход готов. Когда будете готовы, входите в портал. | ENTER THE PORTAL |
| `p01_entering_portal_001` | entering_portal | Система | Сдвиг сигнала подтвержден. Перенос на следующую планету. | PORTAL TRANSIT |

## Planet 2 - Crystal Moon

| ID | Trigger | Speaker | Text | Objective |
| --- | --- | --- | --- | --- |
| `p02_arrival_001` | arrival | Оператор | Кристальная Луна. В зоне тихо, но два портала уже просыпаются. | CLEAR THE AREA |
| `p02_clear_area_001` | clear_area_objective | Союзник | Сначала зачистим площадку. Потом можно будет активировать сферу. | CLEAR THE AREA |
| `p02_sphere_ready_001` | sphere_ready | Система | Центральная сфера принимает заряд. Активируйте ее, чтобы начать оборону. | ACTIVATE THE SPHERE |
| `p02_two_portals_active_001` | two_portals_active | Оператор | Оба портала активны. Держите левый и правый фланг. | DEFEND THE SPHERE |
| `p02_planet_cleared_001` | planet_cleared | Система | Кристальная Луна стабилизирована. Следующий переход открыт. | UNIVERSE COMPLETE |
| `p02_activate_sphere_hint_001` | reminder_activate_sphere | Оператор | Сфера готова. Подойдите к ней и запустите защитный контур. | ACTIVATE THE SPHERE |

## Planet 3 - Ash Relay Station

| ID | Trigger | Speaker | Text | Objective |
| --- | --- | --- | --- | --- |
| `p03_arrival_001` | arrival | Оператор | Пепельная станция на связи. Наш персонал застрял среди реле. | RESCUE STAFF |
| `p03_staff_objective_001` | staff_objective | Персонал | Мы слышим вас. Найдите нас и проведите к сфере. | RESCUE STAFF |
| `p03_staff_found_001` | staff_found | Персонал | Я с вами. Только не бросайте меня на открытой линии. | RESCUE STAFF |
| `p03_staff_rescued_001` | staff_rescued | Система | Сотрудник эвакуирован. Продолжайте спасение. | RESCUE STAFF |
| `p03_sphere_ready_001` | sphere_ready | Оператор | Персонал внутри контура. Активируйте сферу и держите позицию. | ACTIVATE THE SPHERE |
| `p03_defend_sphere_001` | defend_sphere | Союзник | Станция пошлет все, что осталось. Не дайте сфере погаснуть. | DEFEND THE SPHERE |
| `p03_planet_cleared_001` | planet_cleared | Система | Пепельная станция восстановлена. Маршрут к Рою открыт. | UNIVERSE COMPLETE |
| `p03_rescue_staff_hint_001` | reminder_rescue_staff | Оператор | Командир, персонал все еще в поле. Найдите их маяки. | RESCUE STAFF |
| `p03_escort_staff_hint_001` | reminder_escort_staff | Персонал | Я рядом, ведите меня к сфере. Здесь долго не продержаться. | RESCUE STAFF |

## Planet 4 - Swarm Expanse

| ID | Trigger | Speaker | Text | Objective |
| --- | --- | --- | --- | --- |
| `p04_arrival_001` | arrival | Оператор | Рой захватил пространство. Рифты питают волну за волной. | CLOSE RIFTS |
| `p04_kill_swarm_001` | kill_swarm_objective | Союзник | Нужно проредить рой, иначе к рифтам не подойти. | CLOSE RIFTS |
| `p04_rift_weakened_001` | rift_weakened | Система | Рифт ослаблен. Давление энергии падает. | CLOSE RIFTS |
| `p04_close_rift_001` | close_rift | Оператор | Подойдите к ослабленному рифту и закройте его вручную. | CLOSE RIFTS |
| `p04_all_rifts_closed_001` | all_rifts_closed | Система | Все рифты закрыты. Сигнатура Роя исчезает. | CLOSE RIFTS |
| `p04_portal_to_planet5_ready_001` | portal_to_planet5_ready | Оператор | Дальше источник разлома. Портал на Пятую планету готов. | ENTER THE PORTAL |
| `p04_close_rift_hint_001` | reminder_close_rift | Оператор | Ослабленный рифт открыт слишком долго. Закройте его сейчас. | CLOSE RIFTS |

## Planet 5 - Crimson Singularity

| ID | Trigger | Speaker | Text | Objective |
| --- | --- | --- | --- | --- |
| `p05_arrival_001` | arrival | Оператор | Багровая Сингулярность. Здесь держится вся петля вселенной. | DESTROY THE CORRUPTED SPHERE |
| `p05_corrupted_sphere_001` | corrupted_sphere_explanation | Помощник 1 | Сфера заражена. Пока она цела, боссы будут возвращать силу. | DESTROY THE CORRUPTED SPHERE |
| `p05_destroy_sphere_hint_001` | reminder_destroy_sphere | Помощник 1 | Командир, нам сначала нужно разрушить сферу! | DESTROY THE CORRUPTED SPHERE |
| `p05_bosses_healed_hint_001` | reminder_bosses_healed | Помощник 2 | Она их исцеляет! Разбейте сферу, иначе боссы не погибнут! | DESTROY THE CORRUPTED SPHERE |
| `p05_sphere_destroyed_001` | sphere_destroyed | Система | Зараженная сфера разрушена. Щит боссов отключен. | KILL BOTH BOSSES |
| `p05_bosses_defeated_001` | bosses_defeated | Оператор | Оба босса подавлены. Теперь восстановите центральную сферу. | RESTORE THE SPHERE |
| `p05_restore_sphere_001` | restore_sphere_objective | Система | Стабилизаторы готовы. Удерживайте их до восстановления сферы. | RESTORE THE SPHERE |
| `p05_restore_sphere_hint_001` | reminder_restore_sphere | Оператор | Стабилизаторы ждут запуска. Восстановите сферу, пока контур не рухнул. | RESTORE THE SPHERE |
| `p05_stabilizer_completed_001` | stabilizer_completed | Система | Стабилизатор закреплен. Продолжайте восстановление. | RESTORE THE SPHERE |
| `p05_sphere_restored_001` | sphere_restored | Система | Сфера восстановлена. Сингулярность стабилизируется. | RESTORE THE SPHERE |
| `p05_universe_complete_001` | universe_complete | Оператор | Вселенная спасена. Но сигнал показывает новую, более опасную петлю. | UNIVERSE COMPLETE |
| `p05_enter_next_universe_001` | enter_next_universe | Система | Переход в следующую вселенную готов. Подтвердите вход. | ENTER NEXT UNIVERSE |

## Universe Loop

| ID | Trigger | Speaker | Text | Objective |
| --- | --- | --- | --- | --- |
| `universe_next_001` | universe_2_begins | Система | Новая вселенная активна. Враги адаптировались к вашей тактике. | PLANET 1 - ENEMIES EMPOWERED |
| `universe_enemies_empowered_001` | enemies_empowered | Оператор | Цели усилены. Не экономьте улучшения и держите турели в работе. | DEFEND THE CORE |
| `universe_score_multiplier_001` | score_multiplier_increased | Система | Множитель счета повышен. Риск и награда растут. | DEFEND THE CORE |

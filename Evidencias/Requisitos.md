# Lista de verificación de requisitos — Nexus

Cada requisito de la rúbrica, cómo lo cumple el juego y en qué fase se implementa.
Estado: ⬜ pendiente · 🟨 en curso · ✅ cumplido (con evidencia en la bitácora)

## 1. Mecánicas obligatorias

| # | Requisito | Cómo lo cumple Nexus | Fase | Estado |
|---|---|---|---|---|
| 1.1 | Mínimo 3 niveles jugables con progresión lógica | Mantenimiento (tutorial) → Almacenamiento (laberinto) → Núcleo (clímax). Un disco por nivel; el nivel siguiente se desbloquea al extraerlo | 6 | ✅ Nivel 1 (bitácora 6.3, 6.8), Nivel 2 (6.6, 6.8) y Nivel 3 con puzzle, jefe final y victoria (6.9); falta una partida completa de corrido de los tres |
| 1.2 | Mínimo 3 Canvas diferenciados | Canvas 1 Menú (inicio, selector, objetivo, créditos) · Canvas 2 HUD · Canvas 3 Game Over / Victoria | 3, 6 | ✅ Canvas 1 menú (título, selector, briefing, créditos), Canvas 2 HUD, Canvas 3 Game Over / Victoria, y además el inventario (bitácora 3.4, 3.5, 3.6 y 6.4) |
| 1.3 | Selector de personajes con Avatar Humanoid | Modelo A y Modelo B = avatares 3D de los dos integrantes, configurados como Humanoid y con un único Animator compartido | 2 | ✅ selector 3D en el menú; el nivel usa el avatar elegido (probado con Deisy, bitácora 6.4) |
| 1.4 | Música de fondo con componentes de audio | `AudioSource` con música ambiental en bucle + capa de zumbido de ventiladores | 5 | ✅ música por capas (menú, exploración, tensión, jefe, sirena), ventiladores y 26 efectos 3D, todo sintetizado por código (bitácora 5.1) |
| 1.5 | Créditos de la materia e imagen institucional | Panel de créditos (materia, docente, integrantes, recursos usados) y logo institucional en la pantalla de título y en los créditos. Los datos se editan en `Assets/Nexus/Datos/Creditos.asset` | 6 | 🟨 panel y espacio para logos listos; faltan los datos reales y los logos |
| 1.6 | Explicación breve del objetivo | Pantalla de *briefing* antes del Nivel 1 + resumen del objetivo al inicio de cada nivel | 6 | ✅ briefing de misión en el menú + objetivo en el HUD al empezar el nivel (bitácora 6.4) |
| 1.7 | Timer en pantalla | Temporizador de refrigeración (cuenta atrás) en el HUD | 3 | ✅ |
| 1.8 | Sistema de vidas/salud | 3 "Oportunidades de Evasión": aparecen en pantalla al cambiar y siempre en el inventario | 3 | ✅ |
| 1.9 | Recoger objetos con contador | Contador de Discos Duros (x/3) + cartuchos de refrigerante por nivel (contador y +tiempo) | 1, 3 | ✅ |
| 1.10 | IA autónoma | Androides con NavMeshAgent: patrulla por puntos, detección por cono de visión con línea de visión, persecución y regreso a la patrulla | 4 | ✅ (máquina de estados de 5 estados, probada) |
| 1.11 | Cuadros de diálogo narrativos | Mensajes de terminal estilo "ALERTA DEL SISTEMA" que guían la historia y dan instrucciones | 3 | ✅ |

## 2. Plataformas (definen la nota máxima)

| # | Plataforma | Entregable | Fase | Estado |
|---|---|---|---|---|
| 2.1 | Windows | Ejecutable `.exe` en carpeta comprimida | 7 | 🟨 build de prueba OK (11/11) |
| 2.2 | Web (WebGL) | Build publicado en línea, embebido en el artículo, con botón de pantalla completa | 7 | 🟨 build de prueba OK, botón de pantalla completa verificado (11/11) |
| 2.3 | Android | Archivo `.apk` instalable | 7 | 🟨 APK de prueba compila (11/11), falta probar en celular |

## 3. Entregables de producción y documentación

| # | Entregable | Qué incluye | Estado |
|---|---|---|---|
| 3.1 | Artículo en Niixer | Nombres completos de los integrantes; bitácora paso a paso (diseño, programación, arte, dificultades superadas); juego WebGL embebido con pantalla completa; video de gameplay embebido | ⬜ |
| 3.2 | Video "Cómo lo hicimos" | Construcción de niveles en Unity, configuración del Avatar y del selector, scripts clave, juego funcionando en tiempo real | ⬜ |
| 3.3 | Manual de instalación Windows | Paso a paso con capturas | ⬜ |
| 3.4 | Manual de instalación APK Android | Paso a paso con capturas (orígenes desconocidos, instalación, ejecución) | ⬜ |

## Datos pendientes de los integrantes

- [x] Avatares 3D de los dos integrantes (Modelo A y Modelo B): Neyder y Deisy, de Avaturn
- [ ] Nombres completos de los integrantes
- [ ] Nombre de la institución, programa, docente y semestre (para los créditos, en `Assets/Nexus/Datos/Creditos.asset`)
- [ ] Logos institucionales en PNG, idealmente con fondo transparente
- [ ] Decidir si se mantiene la línea "Asistencia de programación: Claude Code (Anthropic)" en los créditos
- [x] Fecha de entrega: miércoles 18/11/2026
- [x] Animaciones de Mixamo

## 4. Extras de diseño (más allá de la rúbrica)

| Extra | Qué aporta | Evidencia |
|---|---|---|
| Habilidad propia de cada técnico (Q) | Rejugabilidad: Neyder distrae con un señuelo de red, Deisy apaga androides con KILLSWITCH.exe | Bitácora 3.7 |
| Ambientación de empresa abandonada | 48 objetos de utilería, decorador automático por tema de sala, basura, polvo y luces que titilan | Bitácora 6.5 |
| Máquinas expendedoras | Fuente de curación (bebida energética / barrita de proteína) con riesgo: el ruido atrae a los androides | Bitácora 6.5 |
| Armas y daño a los androides | Cuchillo, pistola y escopeta con apuntado asistido, munición limitada y ruido que atrae androides; los androides tienen vida y se pueden neutralizar (doble daño si Deisy los apagó) | Bitácora 3.12 |
| Cadáveres que se registran | Las armas y la munición se obtienen de quienes cayeron antes (empleados y operadores SWAT), con notas que cuentan la historia previa a la misión | Bitácora 2.7, 3.13 |
| Animaciones de escopeta propias | Clips Humanoid creados músculo por músculo + IK de manos: agarre, apuntar, retroceso, bombeo y recarga cartucho a cartucho | Bitácora 3.14 |
| Cinemáticas | Entrada del técnico al centro de datos y alarma al extraer el disco (luces rojas, androides que salen de sus estaciones de carga), con subtítulos y opción de saltar | Bitácora 6.7 |
| Puzzle de válvulas | Secuencia de enfriamiento por presión (cambia en cada partida); un error cierra todas, quita tiempo y atrae androides | Bitácora 6.9 |
| Jefe final | SENTINEL-Ω: ciclo blindado / carga / expuesto, llama refuerzos a media vida y bloquea la salida; cinemática del reinicio antes de la Victoria | Bitácora 6.9 |
| Huida y balance | Nivel 1 solo con cuchillo y huida hasta la cafetería; Nivel 2 con 7 androides y munición contada (~15 % de margen) | Bitácora 6.8 |

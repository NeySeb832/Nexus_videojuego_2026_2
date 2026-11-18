# NEXUS — Protocolo de emergencia

Videojuego de terror y sigilo en tercera persona hecho en **Unity 6** para el trabajo final de
**Modelado 3D y Videojuegos** (2026). El técnico de guardia queda atrapado en el centro de datos de
Nexus Corp.: la refrigeración falló, la actualización CERBERUS 7.1 volvió hostiles a los androides
de seguridad SENTINEL y hay que recuperar los 3 Discos Duros Maestros antes del colapso térmico.

**Equipo:** Neyder y Deisy. Sus avatares 3D son los dos personajes jugables.

## Características

- Cámaras fijas al estilo *survival horror* clásico con **controles tipo tanque**; cada sala tiene
  varios ángulos y la cámara cambia sola cuando el personaje se sale del cuadro.
- Selector de personaje con avatares **Humanoid**, cada uno con una habilidad propia:
  - **Neyder:** *Señuelo de red*, atrae a los androides cercanos.
  - **Deisy:** *KILLSWITCH.exe*, apaga un androide durante 10 s.
- **IA** de los androides con NavMesh: patrulla, cono de visión con línea de vista, oído,
  persecución, búsqueda y reacción a ruidos y señuelos.
- Inventario de terminal portátil (objetos, archivo de notas y mapa), curación con bebidas energéticas y
  barritas de proteína, y guardado en terminales con memoria USB.
- Temporizador de refrigeración, salud, coleccionables, diálogos de sistema y pantallas animadas de
  Game Over y Victoria.
- Escenarios generados desde mapas de texto, con decoración automática de empresa abandonada:
  oficinas, cafetería con máquinas expendedoras, almacén, basura, polvo y luces que fallan.
- Armas (bayoneta, pistola y escopeta) que se obtienen al registrar los cadáveres de quienes
  entraron antes, con notas que cuentan la historia; animaciones de escopeta propias (IK).
- Cinemáticas con el motor del juego: la entrada al centro de datos, la alarma al extraer el disco y
  el reinicio final.
- Nivel 3 con un puzzle de válvulas (orden por presión, distinto en cada partida) y un jefe final,
  la unidad prototipo SENTINEL-Ω.
- Música y efectos de sonido de **Pixabay** (autores en
  [Evidencias/Creditos_Audio.md](Evidencias/Creditos_Audio.md)), mezclados por capas: la tensión
  entra cuando un androide te persigue y la sirena con la alarma.
- Plataformas: **Windows, WebGL y Android**.

## Estructura del repositorio

| Carpeta | Contenido |
|---|---|
| `Nexus/` | Proyecto de Unity (abrir esta carpeta con Unity Hub) |
| `Nexus/Assets/Nexus/Scripts/` | Código del juego (jugador, IA, interacción, UI, partida) |
| `Nexus/Assets/Nexus/Editor/` | Herramientas que construyen modelos, texturas, niveles y UI por código (menú **Nexus** del editor) |
| `Evidencias/` | Bitácora de desarrollo paso a paso, capturas numeradas y lista de requisitos |
| `Avatares/` | Archivos originales de los avatares de los integrantes |

## Cómo abrir el proyecto

1. Instala **Unity 6000.6.3f1** desde Unity Hub, con los módulos de Android y WebGL si vas a compilar.
2. En Unity Hub: *Add project from disk* → selecciona la carpeta `Nexus`.
3. La primera apertura tarda varios minutos (Unity regenera la carpeta `Library`).
4. Escena inicial: `Assets/Nexus/Escenas/00_Menu.unity`.

**Controles tipo tanque (teclado / mando):**

| Acción | Teclado | Mando |
|---|---|---|
| Avanzar / retroceder | W / S | Stick izquierdo arriba / abajo |
| Girar | A / D | Stick izquierdo a los lados |
| Correr | Shift | Presionar el stick |
| Media vuelta rápida | Shift + S | — |
| Interactuar | E | Y / triángulo |
| Habilidad | Q | X / cuadrado |
| Inventario | TAB / I | Start |
| Equipar cuchillo / pistola / escopeta | 1 / 2 / 3 (0 guarda el arma) | Cruceta |
| Apuntar | Clic derecho / K | LT |
| Disparar o acuchillar | Clic izquierdo / J | RT |
| Recargar | R | RB |

Las cámaras son fijas y cambian solas cuando el personaje sale del cuadro.

## Documentación

- [Bitácora de desarrollo](Evidencias/Bitacora.md)
- [Requisitos del curso y su estado](Evidencias/Requisitos.md)
- [Diseño de niveles](Evidencias/Diseno_Niveles.md)

# Diseño de niveles — Nexus (propuesta)

Objetivo: un juego completo de **15–18 minutos**. Cada nivel tiene varias salas conectadas, objetivos
encadenados (que obligan a explorar y a volver), objetos opcionales y androides con patrullas
distintas. La dificultad sube en cada nivel: más androides, más rápidos y menos tiempo.

**Leyenda de objetos:**

| Objeto | Función |
|---|---|
| 💾 Disco Duro Maestro | Objetivo principal de cada nivel (contador x/3) |
| 🟥 / 🟦 Tarjeta de acceso | Abre las puertas de su color |
| 🔌 Fusible | Objeto de puzle: devuelve la energía a una zona |
| 📄 Nota | Historia (*lore*) y pistas, por ejemplo códigos |
| ⚡ Bebida energética | Recupera parte de la salud |
| 🍫 Barrita de proteína | Recupera toda la salud |
| ❄️ Cartucho de refrigerante | Coleccionable con contador: suma segundos al temporizador |
| 🔑 USB de respaldo | Se gasta al guardar en una terminal de respaldo |
| 🖥️ Terminal | Cuadros de diálogo con alertas e instrucciones |

---

## Nivel 1 — Cuarto de Mantenimiento (tutorial) · ~5 min · 1–2 androides

Enseña los controles, las cámaras fijas, el inventario, las puertas y el guardado, en un recorrido
con una sola ruta principal.

```
                 [Montacargas] ← SALIDA
                       │
              [Sala de servidores]  💾 Disco 1 (al tomarlo suena la alarma)
                       │ puerta 🟥
 [Almacén de    ]──[ Cruce de ]──[Cuarto eléctrico ]
 [repuestos     ]  [ pasillos ]  [panel de fusibles]
  🔌 ⚡ ❄️              │          🔑 🖥️ guardado
                [Oficina de seguridad] ← puerta sin energía
                  🟥 tarjeta · 📄 nota 2
                       │
               [Pasillo de racks A]  🤖 androide con patrulla predecible
                       │
            [Sala de mantenimiento]  INICIO · 🖥️ terminal · 📄 nota 1 · ⚡
```

**Cadena de objetivos:**
1. La terminal de la sala de mantenimiento da la primera alerta y la misión: conseguir acceso de
   nivel 2 en la Oficina de Seguridad.
2. Cruzar el pasillo esquivando al androide (enseña a esconderse tras los racks).
3. La oficina no tiene energía, así que hay que buscar un fusible en el almacén.
4. Colocar el fusible en el panel del cuarto eléctrico. Ahí están también el primer USB y la
   terminal de guardado.
5. Tomar la tarjeta roja en la oficina (y la nota con la historia de la IA).
6. Abrir la puerta roja y extraer el **Disco 1**. Suena la alarma, se activa un segundo androide y
   hay que huir al montacargas.

## Nivel 2 — Pasillos de Almacenamiento (laberinto) · ~6–7 min · 3 androides

Laberinto de filas de racks con **luces rojas de emergencia**. Exige sigilo y exploración, y hay
que volver sobre los propios pasos.

```
 [Oficina del supervisor]                  [Sala de seguridad]
   📄 nota con el CÓDIGO          ┌─────┐     🟦 tarjeta azul
          ╲                       │BÓVEDA│      ╱
           ╲   laberinto de       │ 💾 2 │     ╱   laberinto de
            ╲  racks (3 🤖)       └─────┘    ╱    racks
   callejones con 🍫 ❄️ ❄️     🖥️ guardado   callejones con ⚡ ❄️ 🔑
                     [Montacargas: entrada y salida]
```

**Cadena de objetivos:**
1. La bóveda central tiene dos cierres: **puerta azul** y un **teclado con código**.
2. La tarjeta azul está en la Sala de Seguridad, en un extremo del laberinto.
3. El código está escrito en una nota de la Oficina del Supervisor, en el extremo opuesto.
4. Con los dos, se abre la bóveda y se extrae el **Disco 2**. Luego hay que volver al montacargas.
5. Los cartuchos de refrigerante de los callejones dan tiempo extra, a cambio de arriesgarse a
   cruzarse con un androide.

## Nivel 3 — El Núcleo de Datos (clímax) · ~4–5 min · 4–5 androides rápidos

Sala central **enorme y simétrica** con el núcleo en el centro. El temporizador empieza en estado
crítico (**2:00**).

```
                 [Ala Norte: válvula 1]
                          │
 [Ala Oeste: válvula 2]──[ NÚCLEO 💾 3 ]──[Ala Este: válvula 3]
                    (escudo térmico)
                          │
                 [Ala Sur: SALIDA DE EMERGENCIA]
```

**Cadena de objetivos:**
1. El Disco 3 está protegido por un **escudo térmico**.
2. Hay que abrir las **3 válvulas de refrigeración**, una en cada ala. Cada válvula suma tiempo y
   apaga un tercio del escudo.
3. Los androides patrullan en anillos alrededor del núcleo: hay que memorizar su patrón para entrar.
4. Con el escudo apagado, se toma el **Disco 3**, se abre la salida de emergencia y hay que llegar a
   ella antes de que se acabe el tiempo. → **Pantalla de Victoria**.

---

## Cómo se construyen

- Cada nivel se dibuja como un **mapa de texto**, donde cada carácter es una celda de 3 × 3 m: muro,
  suelo, fila de racks, puerta u objeto. Una herramienta de editor lo convierte en el nivel 3D. Así
  se pueden hacer niveles grandes rápido, y los mapas sirven de documentación del diseño.
- Las filas de racks se combinan en un solo mesh por celda, para que un laberinto de cientos de
  racks siga funcionando fluido en Android y WebGL.
- Cada sala o tramo de pasillo tiene su cámara fija y su zona de cambio.

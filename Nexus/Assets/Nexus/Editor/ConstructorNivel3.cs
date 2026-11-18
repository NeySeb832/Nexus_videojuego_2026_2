using UnityEditor;
using UnityEngine;

namespace Nexus.Herramientas
{
    /// <summary>Nivel 3: El Núcleo de Datos (clímax). Usa ConstructorNiveles.Construir.</summary>
    public static class ConstructorNivel3
    {
        [MenuItem("Nexus/Niveles/Construir Nivel 3 - Nucleo")]
        public static void Construir()
        {
            var n = Definir();

            // Comprobación: todas las filas deben medir lo mismo
            if (n.Mapa.Length != n.Zonas.Length) { Debug.LogError("[Nexus] Mapa y Zonas tienen distinto número de filas"); return; }
            for (int i = 0; i < n.Mapa.Length; i++)
                if (n.Mapa[i].Length != n.Mapa[0].Length || n.Zonas[i].Length != n.Mapa[0].Length)
                { Debug.LogError($"[Nexus] La fila {i} del mapa o de las zonas tiene una longitud distinta"); return; }

            ConstructorNiveles.Construir(n);
        }

        static DefNivel Definir()
        {
            var n = new DefNivel
            {
                Escena = "03_Nucleo",
                Nombre = "El Núcleo de Datos",
                EscenaSiguiente = "",   // último nivel: ver NivelNexus (debe mostrar la Victoria)
                ObjetivoInicial = "Revisa la terminal",
                Tiempo = 540f,
                TemperaturaInicial = 92f,
                TextoSalida = "Usar montacargas de emergencia",
                ObjetivoPanel = "Energía restablecida. Ve a la Sala de Control (noroeste) por la tarjeta azul",
                Niebla = 0.035f,
                DialogoDisco = new[]
                {
                    "DISCO DURO MAESTRO 3 EXTRAÍDO.",
                    "NÚCLEO DE DATOS: INTEGRIDAD CRÍTICA. REINICIO FORZADO EN CURSO.",
                    "SENTINEL: TODAS LAS UNIDADES, PURGA TOTAL DEL SECTOR. NO DEJEN SALIR AL TÉCNICO."
                }
            };

            n.Mapa = new[]
            {
                "#####################", // 0
                "#.....#.......#..G..#", // 1
                "#..a..#...D...#....n#", // 2
                "#.....#.......#.u.c.#", // 3
                "###X######B######R###", // 4
                "#...................#", // 5
                "#.||..===...===..||.#", // 6
                "#.||.....k.....||...#", // 7
                "#...................#", // 8
                "#.===...|.....|..==.#", // 9
                "#...................#", // 10
                "#.|...===.....===.|.#", // 11
                "#...................#", // 12
                "###L####.....####L###", // 13
                "#.....#.......#.....#", // 14
                "#.f.m.#T..S...#....P#", // 15
                "#ng.b.#.....E.#.....#", // 16
                "#####################"  // 17
            };

            n.Zonas = new[]
            {
                "#####################",
                "#aaaaa#bbbbbbb#ccccc#",
                "#aaaaa#bbbbbbb#ccccc#",
                "#aaaaa#bbbbbbb#ccccc#",
                "###a######b######c###",
                "#dddddddddeeeeeeeeee#",
                "#dddddddddeeeeeeeeee#",
                "#dddddddddeeeeeeeeee#",
                "#dddddddddeeeeeeeeee#",
                "#fffffffffgggggggggg#",
                "#fffffffffgggggggggg#",
                "#fffffffffgggggggggg#",
                "#fffffffffgggggggggg#",
                "###h####iiiii####j###",
                "#hhhhh#iiiiiii#jjjjj#",
                "#hhhhh#iiiiiii#jjjjj#",
                "#hhhhh#iiiiiii#jjjjj#",
                "#####################"
            };

            // ---- Salas ----
            Color frio = new Color(0.7f, 0.85f, 1f), azul = new Color(0.4f, 0.65f, 1f),
                  rojo = new Color(1f, 0.45f, 0.4f), ambar = new Color(1f, 0.8f, 0.5f);

            n.Salas['a'] = new DefZona { Nombre = "Sala_de_Control", Esquina = "NE", Luz = frio, Intensidad = 8f, Tema = TemaSala.Oficina, SinEnergia = true };
            n.Salas['b'] = new DefZona { Nombre = "Nucleo_de_Datos", Esquina = "NO", Luz = azul, Intensidad = 11f, Tema = TemaSala.Servidores };
            n.Salas['c'] = new DefZona { Nombre = "Sala_de_Respaldo", Esquina = "NO", Luz = frio, Intensidad = 8f, Tema = TemaSala.Seguridad };
            n.Salas['d'] = new DefZona { Nombre = "Pasillo_Norte_Oeste", Esquina = "SE", Luz = rojo, Intensidad = 8f, Parpadeo = true };
            n.Salas['e'] = new DefZona { Nombre = "Pasillo_Norte_Este", Esquina = "SO", Luz = rojo, Intensidad = 8f, Parpadeo = true };
            n.Salas['f'] = new DefZona { Nombre = "Pasillo_Sur_Oeste", Esquina = "SE", Luz = ambar, Intensidad = 8f };
            n.Salas['g'] = new DefZona { Nombre = "Pasillo_Sur_Este", Esquina = "SO", Luz = ambar, Intensidad = 8f, Parpadeo = true };
            n.Salas['h'] = new DefZona { Nombre = "Almacen", Esquina = "SE", Luz = ambar, Intensidad = 8f, Tema = TemaSala.Almacen };
            n.Salas['i'] = new DefZona
            {
                Nombre = "Sala_de_Inicio",
                Luz = frio,
                Intensidad = 8f,
                Tema = TemaSala.Mantenimiento,
                Camara = new Vector2(9.5f, 16.6f),    // detrás del técnico, pegada a la pared sur
                Mirar = new Vector2(10.5f, 13.5f),    // mirando al norte, hacia el pasillo
                AlturaCamara = 3.0f
            };
            n.Salas['j'] = new DefZona { Nombre = "Cuarto_Electrico", Esquina = "SO", Luz = ambar, Intensidad = 8f, Tema = TemaSala.Mantenimiento, Parpadeo = true };

            // ---- Objetivos al recoger ----
            n.ObjetivoAlRecoger['f'] = "Instala el fusible en el panel del Cuarto Eléctrico (sureste)";
            n.ObjetivoAlRecoger['a'] = "Abre la puerta azul del Núcleo de Datos (norte, al centro)";
            n.ObjetivoAlRecoger['k'] = "La tarjeta roja abre la Sala de Respaldo (noreste): guardado y curación";
            n.ObjetivoAlRecoger['D'] = "Alarma total. Escapa por el montacargas (sur)";

            // ---- Terminal (la 'T' del mapa) ----
            n.Terminales.Add(("TERMINAL · NÚCLEO DE DATOS", new[]
            {
                "NEXUS OS v3.2 > NÚCLEO CENTRAL. TEMPERATURA: CRÍTICA. REFRIGERACIÓN: AL LÍMITE.",
                "Dos Discos Duros Maestros ya fueron extraídos. El último está en el Núcleo, al norte.",
                "La puerta del Núcleo exige la tarjeta AZUL, guardada en la Sala de Control.",
                "La Sala de Control no tiene energía. Hay un fusible de repuesto en el Almacén (suroeste).",
                "Cuatro SENTINEL patrullan el pasillo. Al extraer el disco, la purga será total."
            }, "Busca un fusible de repuesto en el Almacén (suroeste)"));

            // ---- Notas (en orden de lectura del mapa) ----
            n.Notas.Add(("Registro · Sala de Respaldo",
                "Si estás leyendo esto, llegaste hasta el final. El disco no se puede sacar sin que el sistema lo note. " +
                "Cuando lo tomes, corre. No intentes esconderte: van a buscar en cada rincón.",
                null));
            n.Notas.Add(("Nota del último turno",
                "Dejé un fusible en el Almacén y la tarjeta azul en la Sala de Control. Si el sistema se reinicia sin vigilancia, " +
                "los SENTINEL ya no distinguen entre intrusos y técnicos. Perdón por no avisar antes.",
                null));

            // ---- Androides (coordenadas = columna, fila del mapa de texto) ----
            n.Androides.Add(new DefAndroide   // 1: pasillo norte oeste
            {
                Ruta = new[] { new Vector2Int(2, 5), new Vector2Int(9, 5), new Vector2Int(9, 8), new Vector2Int(2, 8) },
                VelPatrulla = 1.5f
            });
            n.Androides.Add(new DefAndroide   // 2: pasillo norte este
            {
                Ruta = new[] { new Vector2Int(11, 5), new Vector2Int(18, 5), new Vector2Int(18, 8), new Vector2Int(11, 8) },
                VelPatrulla = 1.5f
            });
            n.Androides.Add(new DefAndroide   // 3: pasillo sur oeste
            {
                Ruta = new[] { new Vector2Int(2, 10), new Vector2Int(9, 10), new Vector2Int(9, 12), new Vector2Int(2, 12) },
                VelPatrulla = 1.6f
            });
            n.Androides.Add(new DefAndroide   // 4: pasillo sur este
            {
                Ruta = new[] { new Vector2Int(11, 10), new Vector2Int(18, 10), new Vector2Int(18, 12), new Vector2Int(11, 12) },
                VelPatrulla = 1.6f
            });
            n.Androides.Add(new DefAndroide   // 5: dormido frente al Núcleo, despierta con la alarma
            {
                Ruta = new[] { new Vector2Int(10, 5), new Vector2Int(10, 9) },
                ConAlarma = true
            });
            n.Androides.Add(new DefAndroide   // 6: dormido junto al inicio, bloquea el regreso
            {
                Ruta = new[] { new Vector2Int(10, 12), new Vector2Int(10, 10) },
                ConAlarma = true
            });

            return n;
        }
    }
}
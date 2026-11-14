using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nexus.Herramientas
{
    /// <summary>Nivel 2: Pasillos de Almacenamiento (laberinto). Usa ConstructorNiveles.Construir.</summary>
    public static class ConstructorNivel2
    {
        [MenuItem("Nexus/Niveles/Construir Nivel 2 - Almacenamiento")]
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
                Escena = "02_Almacenamiento",
                Nombre = "Pasillos de Almacenamiento",
                EscenaSiguiente = "03_Nucleo",
                ObjetivoInicial = "Revisa la terminal",
                Tiempo = 480f,
                TemperaturaInicial = 86f,
                TextoSalida = "Usar montacargas",
                Niebla = 0.035f,
                DialogoDisco = new[]
                {
                    "DISCO DURO MAESTRO 2 EXTRAÍDO.",
                    "ALERTA: INTRUSIÓN CONFIRMADA EN EL NÚCLEO DE DATOS.",
                    "SENTINEL: TODAS LAS UNIDADES, CONVERGER EN LA SALA DEL NÚCLEO."
                }
            };

            n.Mapa = new[]
            {
                "#####################", // 0
                "#......#.....#..m.c.#", // 1
                "#.a..n.#..D..#G....u#", // 2
                "#......#.....#......#", // 3
                "###R######B######L###", // 4
                "#...................#", // 5
                "#.....|.......|.....#", // 6
                "#================...#", // 7
                "#...................#", // 8
                "#.......|......|....#", // 9
                "#...================#", // 10
                "#.....=.......=.....#", // 11
                "#...................#", // 12
                "###L#####...#####L###", // 13
                "#V...Y.#.....#......#", // 14
                "#..M...#T.S..#.b..k.#", // 15
                "#n..m..#..E..#....g.#", // 16
                "#####################"  // 17
            };

            n.Zonas = new[]
            {
                "#####################",
                "#aaaaaa#bbbbb#cccccc#",
                "#aaaaaa#bbbbb#cccccc#",
                "#aaaaaa#bbbbb#cccccc#",
                "###a######b######c###",
                "#dddddddddeeeeeeeeee#",
                "#dddddddddeeeeeeeeee#",
                "#dddddddddeeeeeeeeee#",
                "#fffffffffgggggggggg#",
                "#fffffffffgggggggggg#",
                "#fffffffffgggggggggg#",
                "#hhhhhhhhhiiiiiiiiii#",
                "#hhhhhhhhhiiiiiiiiii#",
                "###j#####kkk#####l###",
                "#jjjjjj#kkkkk#llllll#",
                "#jjjjjj#kkkkk#llllll#",
                "#jjjjjj#kkkkk#llllll#",
                "#####################"
            };

            // ---- Salas (cada letra de Zonas) ----
            Color frio = new Color(0.7f, 0.85f, 1f), verde = new Color(0.65f, 0.95f, 0.75f),
                  ambar = new Color(1f, 0.8f, 0.5f), azul = new Color(0.4f, 0.65f, 1f);

            n.Salas['a'] = new DefZona { Nombre = "Oficina_de_Control", Esquina = "SE", Luz = frio, Intensidad = 8f, Tema = TemaSala.Oficina };
            n.Salas['b'] = new DefZona { Nombre = "Nucleo_de_Datos", Esquina = "NE", Luz = azul, Intensidad = 9f, Tema = TemaSala.Servidores };
            n.Salas['c'] = new DefZona { Nombre = "Sala_de_Respaldo", Esquina = "NE", Luz = frio, Intensidad = 8f, Tema = TemaSala.Seguridad };
            n.Salas['d'] = new DefZona { Nombre = "Pasillo_Norte_Oeste", Esquina = "SE", Luz = verde, Intensidad = 8f, Parpadeo = true };
            n.Salas['e'] = new DefZona { Nombre = "Pasillo_Norte_Este", Esquina = "SO", Luz = verde, Intensidad = 8f, Parpadeo = true };
            n.Salas['f'] = new DefZona { Nombre = "Pasillo_Central_Oeste", Esquina = "SE", Luz = ambar, Intensidad = 8f };
            n.Salas['g'] = new DefZona { Nombre = "Pasillo_Central_Este", Esquina = "SO", Luz = ambar, Intensidad = 8f, Parpadeo = true };
            n.Salas['h'] = new DefZona { Nombre = "Pasillo_Sur_Oeste", Esquina = "SE", Luz = verde, Intensidad = 8f };
            n.Salas['i'] = new DefZona { Nombre = "Pasillo_Sur_Este", Esquina = "SO", Luz = verde, Intensidad = 8f, Parpadeo = true };
            n.Salas['j'] = new DefZona { Nombre = "Cafeteria", Esquina = "SE", Luz = ambar, Intensidad = 9f, Tema = TemaSala.Cafeteria };
            n.Salas['k'] = new DefZona { Nombre = "Sala_de_Inicio", Esquina = "SE", Luz = frio, Intensidad = 8f, Tema = TemaSala.Mantenimiento };
            n.Salas['l'] = new DefZona { Nombre = "Almacen_de_Carga", Esquina = "SO", Luz = ambar, Intensidad = 8f, Tema = TemaSala.Almacen };

            // ---- Objetivos al recoger ----
            n.ObjetivoAlRecoger['k'] = "Abre la puerta roja de la Oficina de Control (noroeste)";
            n.ObjetivoAlRecoger['a'] = "Abre la puerta azul del Núcleo de Datos (norte)";
            n.ObjetivoAlRecoger['D'] = "Alarma activa. Escapa por el montacargas (sur)";

            // ---- Terminal (la 'T' del mapa) ----
            n.Terminales.Add(("TERMINAL · SECTOR DE ALMACENAMIENTO", new[]
            {
                "NEXUS OS v3.2 > SECTOR C · ALMACENAMIENTO MASIVO. ESTADO: DEGRADADO.",
                "Los pasillos de racks fueron reconfigurados por el sistema. El plano ya no coincide con la realidad.",
                "Tres unidades SENTINEL patrullan el sector. Protocolo de purga activo.",
                "Disco Duro Maestro 2 localizado en el Núcleo de Datos (norte). Acceso: tarjeta AZUL.",
                "La tarjeta azul está en la Oficina de Control, cuya puerta exige la tarjeta ROJA, guardada en el Almacén de Carga."
            }, "Encuentra la tarjeta roja en el Almacén de Carga (este)"));

            // ---- Notas (en orden de lectura del mapa: primero la de la oficina, luego la de la cafetería) ----
            n.Notas.Add(("Memo · Control de Acceso",
                "Moví la tarjeta azul a esta oficina por seguridad. El Núcleo está al norte, detrás de la puerta azul. " +
                "No corran por los pasillos: los SENTINEL oyen las pisadas rápidas. Caminen y usen los racks para esconderse.",
                null));
            n.Notas.Add(("Aviso de Administración",
                "Los racks de este sector se movieron solos durante la noche. Dos compañeros entraron al laberinto y no salieron. " +
                "Si tienen que cruzarlo, no se detengan en los cruces.",
                null));

            // ---- Androides (coordenadas = columna, fila del mapa de texto) ----
            n.Androides.Add(new DefAndroide   // 1: franja inferior oeste
            {
                Ruta = new[] { new Vector2Int(2, 11), new Vector2Int(2, 12), new Vector2Int(8, 12), new Vector2Int(8, 11) },
                VelPatrulla = 1.4f
            });
            n.Androides.Add(new DefAndroide   // 2: franja media
            {
                Ruta = new[] { new Vector2Int(2, 8), new Vector2Int(9, 8), new Vector2Int(9, 9), new Vector2Int(14, 9),
                               new Vector2Int(18, 9), new Vector2Int(18, 8), new Vector2Int(12, 8) },
                VelPatrulla = 1.5f
            });
            n.Androides.Add(new DefAndroide   // 3: franja superior
            {
                Ruta = new[] { new Vector2Int(2, 5), new Vector2Int(8, 5), new Vector2Int(8, 6), new Vector2Int(12, 5),
                               new Vector2Int(18, 5), new Vector2Int(18, 6), new Vector2Int(15, 6) },
                VelPatrulla = 1.5f
            });
            n.Androides.Add(new DefAndroide   // 4: dormido en la franja inferior este, despierta con la alarma
            {
                Ruta = new[] { new Vector2Int(15, 12), new Vector2Int(11, 12) },
                ConAlarma = true
            });

            return n;
        }
    }
}
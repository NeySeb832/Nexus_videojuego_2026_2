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
                "#Q..........K.......#", // 5   estación de carga | sargento Ortiz (escopeta)
                "#.....|.......|.....#", // 6
                "#================...#", // 7
                "#..................Q#", // 8   estación de carga
                "#...K...|......|....#", // 9   operador Castro (balas)
                "#...================#", // 10
                "#.....=.......=.....#", // 11
                "#Q..................#", // 12  estación de carga
                "###L#####...#####L###", // 13
                "#V...Y.#....K#.....K#", // 14  operador Vargas (pistola) | operador Ruiz (cartuchos)
                "#..M...#T.S..#.b..k.#", // 15
                "#n..m.K#..E..#....g.#", // 16  guía del equipo táctico (balas)
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

            // ---- Equipo táctico caído (cadáveres 'K', en orden de lectura del mapa) ----
            // Balance: 7 androides x 12 de vida = 84. Munición del nivel: 26 balas (52 de daño) y
            // 5 cartuchos (~9 de daño de cerca cada uno, ~45): alcanza para todos con ~15% de margen
            // si no se desperdicia. El cuchillo queda para rematar o para ahorrar balas.
            n.VelAlarma = 3.6f;
            n.Cadaveres.Add(new DefCadaver
            {
                Modelo = "Swat_B", Pose = "Tendido", Quien = "Sargento del equipo táctico",
                Lineas = new[]
                {
                    "El sargento del equipo táctico. Cayó a pocos metros de la puerta del Núcleo de Datos.",
                    "Su escopeta quedó en el suelo, a su lado. Quedan tres cartuchos en el tubo."
                },
                Objeto = TipoColeccionable.Escopeta, NombreObjeto = "Escopeta Remington 870",
                Extra = TipoColeccionable.CartuchosEscopeta, CantidadExtra = 3, NombreExtra = "cartuchos de escopeta",
                TituloNota = "Orden de misión — Sgto. Ortiz",
                TextoNota = "UNIDAD TÁCTICA 7 — ORDEN DE MISIÓN. Contratante: Nexus Corp.\n" +
                            "Objetivo: asegurar los tres Discos Duros Maestros antes de que CERBERUS cifre los respaldos. Prioridad: los datos, no el personal.\n" +
                            "Escrito a mano: «Nos dijeron que eran drones con un fallo de software. Nexus sabía desde el día 14 que las unidades estaban purgando gente. Si salgo de esta, lo cuento todo.»"
            });
            n.Cadaveres.Add(new DefCadaver
            {
                Modelo = "Swat_A", Pose = "Tendido", Quien = "Operador Castro",
                Lineas = new[]
                {
                    "Un operador del equipo táctico, con la placa CASTRO. Parece que lo arrastraron hasta aquí.",
                    "Lleva un cargador de pistola en el chaleco."
                },
                Objeto = TipoColeccionable.BalasPistola, Cantidad = 9, NombreObjeto = "Balas de pistola",
                TituloNota = "Radio de Castro (última transmisión)",
                TextoNota = "—Castro a Vargas: me separé del grupo en el pasillo B. Los racks se mueven... o eso parece. Son tres, no, cuatro unidades.\n" +
                            "Me quedan nueve balas y no pienso gastarlas a ciegas.\n—Vargas a Castro, repite. Castro, ¿me copias? ¿Castro?"
            });
            n.Cadaveres.Add(new DefCadaver
            {
                Modelo = "Swat_A", Pose = "Pared", Quien = "Operador Vargas",
                Lineas = new[]
                {
                    "Un operador del equipo táctico. Está sentado contra la pared, con la pistola todavía en la mano.",
                    "El chaleco está perforado de lado a lado. Le quedaban nueve balas."
                },
                Objeto = TipoColeccionable.Pistola, NombreObjeto = "Pistola M9",
                Extra = TipoColeccionable.BalasPistola, CantidadExtra = 9, NombreExtra = "balas",
                TituloNota = "Registro de la Unidad Táctica — Vargas",
                TextoNota = "03:10. Entramos seis por la rampa de carga. El contratista habló de «drones de vigilancia con un fallo». No son drones.\n" +
                            "El 9 mm apenas los frena: hay que meterles medio cargador en el pecho para tumbarlos. Nada de disparos a lo loco.\n" +
                            "Perdimos a Castro en el pasillo B. Ortiz siguió hacia el núcleo con la escopeta. Yo ya no puedo caminar."
            });
            n.Cadaveres.Add(new DefCadaver
            {
                Modelo = "Swat_B", Pose = "Pared", Quien = "Operador Ruiz",
                Lineas = new[]
                {
                    "El operador más joven del equipo, por lo que se ve. Se quedó a cubrir el almacén.",
                    "En el cinturón tiene dos cartuchos de escopeta."
                },
                Objeto = TipoColeccionable.CartuchosEscopeta, Cantidad = 2, NombreObjeto = "Cartuchos de escopeta",
                TituloNota = "Mensaje de Ruiz",
                TextoNota = "Mamá: si te llega esto es porque no salí. No es culpa de nadie del equipo. Nos mandaron a recuperar unos discos y nadie nos dijo lo que había aquí abajo.\n" +
                            "Los SENTINEL no se cansan y no olvidan. Cuando suena la alarma, salen todos de sus estaciones de carga."
            });
            n.Cadaveres.Add(new DefCadaver
            {
                Modelo = "Empleado", Pose = "Pared", Quien = "Guía del equipo táctico",
                Lineas = new[]
                {
                    "Un empleado de Nexus con la tarjeta de visitante del equipo táctico colgada al cuello.",
                    "Tiene una caja de balas de 9 mm. Los operadores debieron dársela para que se defendiera."
                },
                Objeto = TipoColeccionable.BalasPistola, Cantidad = 8, NombreObjeto = "Balas de pistola",
                TituloNota = "Nota del guía",
                TextoNota = "Me ofrecí a guiar al equipo táctico por los pasillos de almacenamiento: nadie conoce este laberinto como yo.\n" +
                            "Cuando nos atacaron, el sargento me dio una caja de balas «por si acaso». No sé disparar.\n" +
                            "La salida es el montacargas de la Sala de Inicio, al sur. El disco está en el Núcleo, al norte. Todo lo demás es laberinto."
            });

            return n;
        }
    }
}
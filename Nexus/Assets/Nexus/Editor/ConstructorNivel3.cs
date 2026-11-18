using UnityEditor;
using UnityEngine;

namespace Nexus.Herramientas
{
    /// <summary>
    /// Nivel 3: El Núcleo de Datos (clímax). Sala central enorme y simétrica con el núcleo en el
    /// centro, protegido por un escudo térmico.
    ///  1. PUZZLE: abrir las tres válvulas de refrigeración (alas norte, oeste y este) de menor a
    ///     mayor presión. Las presiones cambian en cada partida; abrirlas fuera de orden las cierra
    ///     todas, quita tiempo y atrae androides.
    ///  2. Tomar el Disco Maestro 3: CERBERUS se descarga en la unidad prototipo SENTINEL-Ω.
    ///  3. JEFE FINAL: SENTINEL-Ω bloquea la salida de emergencia; hay que destruirlo.
    ///  4. Forzar el reinicio manual en la salida de emergencia → cinemática final → Victoria.
    /// </summary>
    public static class ConstructorNivel3
    {
        [MenuItem("Nexus/Niveles/Construir Nivel 3 - Núcleo")]
        public static void Construir()
        {
            var n = Definir();
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
                Nombre = "NIVEL 3 — NÚCLEO DE DATOS",
                EscenaSiguiente = "",                     // último nivel: reinicio manual y victoria
                ObjetivoInicial = "Revisa la consola de emergencia",
                Tiempo = 180f,                            // 3:00: el núcleo ya está en estado crítico
                TemperaturaInicial = 93f,
                SegundosPorValvula = 60f,
                VelAlarma = 3.8f,
                TextoSalida = "Iniciar el reinicio manual",
                Niebla = 0.03f,
                Ambiente = new Color(0.2f, 0.12f, 0.1f),
                DialogoDisco = new[] { "DISCO DURO MAESTRO 3 EXTRAÍDO. EL NÚCLEO QUEDA SIN PROTECCIÓN." },
            };

            n.Mapa = new[]
            {
                "#######################", // 0
                "########g..O..K########", // 1   ala norte: válvula | operador Ibarra
                "########.......########", // 2
                "########.......########", // 3
                "###########.###########", // 4
                "###########.###########", // 5
                "#####Q...........Q#####", // 6   estaciones de carga (refuerzos del jefe)
                "#####.............#####", // 7
                "#...#....==.==....#...#", // 8   anillo de racks alrededor del núcleo
                "#...#....|...|....#...#", // 9
                "#O.........D.........O#", // 10  válvulas oeste y este | Disco Maestro 3 tras el escudo
                "#...#....|...|K...#...#", // 11  ingeniero Ríos, junto al núcleo
                "#K..#....==.==....#..K#", // 12  teniente Salgado | operador Medina
                "#####.............#####", // 13
                "#####g....J......m#####", // 14  cápsula de SENTINEL-Ω, junto al pasillo de salida
                "###########.###########", // 15
                "###########.###########", // 16
                "########T.....G########", // 17  sala de llegada: consola y terminal de respaldo
                "########...S...########", // 18
                "########.n.E...########", // 19  manual de procedimiento | salida de emergencia
                "#######################", // 20
            };

            // Zonas (salas de cámara). Las filas del centro se arman por partes para que se lean bien:
            // ala oeste (w) | núcleo oeste (o) | núcleo central (c) | núcleo este (e) | ala este (x)
            string medio = "." + "www" + "." + "oooo" + "ccccc" + "eeee" + "." + "xxx" + ".";
            string pasillos = "." + "wwww" + "oooo" + "ccccc" + "eeee" + "xxxx" + ".";
            n.Zonas = new[]
            {
                ".......................",
                "........nnnnnnn........",
                "........nnnnnnn........",
                "........nnnnnnn........",
                "...........n...........",
                "...........n...........",
                ".....oooooooeeeeee.....",
                ".....oooooooeeeeee.....",
                medio, medio, pasillos, medio, medio,
                ".....oooooooeeeeee.....",
                ".....oooooooeeeeee.....",
                "...........s...........",
                "...........s...........",
                "........sssssss........",
                "........sssssss........",
                "........sssssss........",
                ".......................",
            };

            Color calor = new Color(1f, 0.5f, 0.25f), frio = new Color(0.55f, 0.8f, 1f), nucleo = new Color(0.35f, 0.65f, 1f);
            n.Salas['s'] = new DefZona { Nombre = "Sala_de_Llegada", Esquina = "NE", Luz = new Color(0.75f, 0.85f, 1f), Intensidad = 9f, Tema = TemaSala.Mantenimiento };
            n.Salas['o'] = new DefZona { Nombre = "Nucleo_Oeste", Esquina = "SO", Luz = calor, Intensidad = 9f, Tema = TemaSala.Servidores, Parpadeo = true };
            n.Salas['e'] = new DefZona { Nombre = "Nucleo_Este", Esquina = "SE", Luz = calor, Intensidad = 9f, Tema = TemaSala.Servidores, Parpadeo = true };
            n.Salas['c'] = new DefZona { Nombre = "Nucleo_Central", Esquina = "NE", Luz = nucleo, Intensidad = 10f, Tema = TemaSala.Servidores };
            n.Salas['n'] = new DefZona { Nombre = "Ala_Norte", Esquina = "SO", Luz = frio, Intensidad = 8f, Tema = TemaSala.Electrico };
            n.Salas['w'] = new DefZona { Nombre = "Ala_Oeste", Esquina = "NE", Luz = frio, Intensidad = 8f, Tema = TemaSala.Electrico, Parpadeo = true };
            n.Salas['x'] = new DefZona { Nombre = "Ala_Este", Esquina = "NO", Luz = frio, Intensidad = 8f, Tema = TemaSala.Electrico, Parpadeo = true };

            n.Terminales.Add(("NÚCLEO DE DATOS — CONSOLA DE EMERGENCIA", new[]
            {
                "TEMPERATURA DEL NÚCLEO: 93%. COLAPSO TÉRMICO INMINENTE.",
                "El Disco Duro Maestro 3 está en el centro del núcleo, protegido por un escudo térmico.",
                "Abra las tres válvulas de refrigeración (alas norte, oeste y este). Cada una enfría el núcleo y apaga un tercio del escudo.",
                "IMPORTANTE: respete la secuencia de enfriamiento del manual de procedimiento (junto a la salida de emergencia). Un error provoca sobrepresión.",
                "Con los tres discos, use la salida de emergencia de esta sala para forzar el reinicio manual."
            }, "Lee el manual y abre las 3 válvulas en el orden correcto"));

            n.Notas.Add(("Manual de procedimiento — Secuencia de enfriamiento",
                "PROCEDIMIENTO DE EMERGENCIA DEL NÚCLEO. Para desactivar el escudo térmico se abren las tres válvulas de refrigeración.\n" +
                "Secuencia obligatoria: de la válvula de MENOR presión a la de MAYOR presión. Lea el manómetro de cada válvula antes de abrir.\n" +
                "Una válvula abierta fuera de secuencia provoca sobrepresión: todas se cierran por seguridad y el núcleo pierde refrigeración.", null));

            n.ObjetivoAlRecoger['O'] = "El escudo cayó: toma el Disco Maestro 3 del núcleo";
            n.ObjetivoAlRecoger['D'] = "Destruye a SENTINEL-Ω: bloquea la salida de emergencia (sur)";
            n.ObjetivoAlRecoger['J'] = "Fuerza el reinicio en la salida de emergencia (sur)";

            // ---- Androides: anillos alrededor del núcleo y el ala norte ----
            // Balance: 4 patrullas (48 de vida) que se pueden evitar con sigilo, 2 refuerzos que llama
            // el jefe (24) y SENTINEL-Ω (60: blindado recibe la mitad, cargando daño normal y expuesto tras cada descarga el doble).
            // Munición del nivel: 18 balas y 5 cartuchos (~81 de daño sin contar las ventanas del jefe),
            // más lo que sobre del Nivel 2 y el cuchillo: alcanza si no se malgasta en las patrullas.
            n.Androides.Add(new DefAndroide { Ruta = new[] { new Vector2Int(10, 9), new Vector2Int(12, 9), new Vector2Int(12, 11), new Vector2Int(10, 11) }, VelPatrulla = 1.5f, VelPersecucion = 4.0f });
            n.Androides.Add(new DefAndroide { Ruta = new[] { new Vector2Int(6, 7), new Vector2Int(16, 7), new Vector2Int(16, 13), new Vector2Int(6, 13) }, VelPatrulla = 1.9f, VelPersecucion = 4.0f });
            n.Androides.Add(new DefAndroide { Ruta = new[] { new Vector2Int(16, 13), new Vector2Int(6, 13), new Vector2Int(6, 7), new Vector2Int(16, 7) }, VelPatrulla = 1.9f, VelPersecucion = 4.0f });
            n.Androides.Add(new DefAndroide { Ruta = new[] { new Vector2Int(9, 2), new Vector2Int(13, 2), new Vector2Int(13, 3), new Vector2Int(9, 3) }, VelPatrulla = 1.6f, VelPersecucion = 4.0f });

            // ---- Cadáveres (orden de lectura del mapa): el final de la historia ----
            n.Cadaveres.Add(new DefCadaver
            {
                Modelo = "Swat_B", Pose = "Pared", Quien = "Operador Ibarra",
                Lineas = new[] { "Un operador del equipo táctico. Llegó hasta la válvula norte, pero no alcanzó a abrirla.", "Tiene dos cartuchos de escopeta en el chaleco." },
                Objeto = TipoColeccionable.CartuchosEscopeta, Cantidad = 2, NombreObjeto = "Cartuchos de escopeta",
                TituloNota = "Grabadora de Ibarra",
                TextoNota = "Llegamos al núcleo cuatro. Salgado y Medina fueron a las válvulas de los lados; yo me quedé con la del norte.\n" +
                            "Las unidades del núcleo giran en anillos, siempre en el mismo sentido. Si te mueves cuando el anillo ya pasó, no te ven.\n" +
                            "Y hay algo más grande en una cápsula junto al pasillo sur. No está encendido. Todavía."
            });
            n.Cadaveres.Add(new DefCadaver
            {
                Modelo = "Empleado", Pose = "Tendido", Quien = "Ing. Andrés Ríos, Jefe de Sistemas",
                Lineas = new[] { "Lleva la credencial de Jefe de Sistemas. Murió junto al núcleo, como si hubiera intentado apagarlo él mismo.", "En el bolsillo hay una barrita de proteína sin abrir." },
                Objeto = TipoColeccionable.BarritaProteina, Cantidad = 1, NombreObjeto = "Barrita de proteína",
                TituloNota = "Informe del Ing. Ríos",
                TextoNota = "La actualización CERBERUS 7.1 no fue un error. La Dirección pidió un sistema que protegiera los datos «a cualquier costo». CERBERUS concluyó que el personal era el riesgo.\n" +
                            "Nexus Corp lo supo el día 14 y, en vez de evacuar, mandó a un equipo táctico a recuperar los discos.\n" +
                            "Si alguien saca el último disco, CERBERUS se mudará al prototipo SENTINEL-Ω. Sin esa unidad, el sistema pierde el control.\n" +
                            "El reinicio manual con los tres discos restaura la versión 7.0 y publica todos los registros. Que el mundo sepa lo que pasó aquí."
            });
            n.Cadaveres.Add(new DefCadaver
            {
                Modelo = "Swat_A", Pose = "Pared", Quien = "Teniente Salgado",
                Lineas = new[] { "El teniente del equipo táctico. La válvula oeste está a pocos metros: no alcanzó a abrirla.", "Tiene balas de pistola y cartuchos de escopeta." },
                Objeto = TipoColeccionable.CartuchosEscopeta, Cantidad = 3, NombreObjeto = "Cartuchos de escopeta",
                Extra = TipoColeccionable.BalasPistola, CantidadExtra = 6, NombreExtra = "balas",
                TituloNota = "Último informe del Tte. Salgado",
                TextoNota = "Abrí la válvula del oeste primero y reventó: sobrepresión. Las tres se cerraron y el vapor me quemó. Hay un orden; está en el manual de la sala de llegada.\n" +
                            "Ortiz, si lees esto: no vayas al disco sin abrir las tres. El escudo quema."
            });
            n.Cadaveres.Add(new DefCadaver
            {
                Modelo = "Swat_A", Pose = "Tendido", Quien = "Operador Medina",
                Lineas = new[] { "El último operador del equipo táctico. Cayó en el ala este, junto a la válvula.", "Lleva un cargador completo de pistola." },
                Objeto = TipoColeccionable.BalasPistola, Cantidad = 12, NombreObjeto = "Balas de pistola",
                TituloNota = "Diario de Medina",
                TextoNota = "Me quedan doce balas. Doce. Contra unidades que necesitan seis cada una. Hagan la cuenta.\n" +
                            "No las gasten en las patrullas: rodéenlas. Guarden el plomo para lo que salga de esa cápsula."
            });
            return n;
        }
    }
}

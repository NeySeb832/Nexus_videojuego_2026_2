using UnityEditor;
using UnityEngine;

namespace Nexus.Herramientas
{
    /// <summary>
    /// Diseño de los niveles de Nexus como mapas de texto (ver la leyenda en ConstructorNiveles).
    /// Cada carácter es una celda de 3 x 3 m. El mapa de zonas agrupa las celdas en salas,
    /// y cada sala tiene su cámara fija y su iluminación.
    /// </summary>
    public static class NivelesNexus
    {
        [MenuItem("Nexus/Niveles/Construir Nivel 1 - Mantenimiento")]
        public static void Nivel1()
        {
            var n = new DefNivel
            {
                Escena = "01_Mantenimiento",
                Nombre = "NIVEL 1 — CUARTO DE MANTENIMIENTO",
                Tiempo = 420f,
                TemperaturaInicial = 80f,
                ObjetivoInicial = "Revisa la terminal de mantenimiento",
                EscenaSiguiente = "02_Almacenamiento",
                // El Nivel 1 no tiene armas de fuego: solo la bayoneta que lleva el cadáver de Ramírez en
                // la cafetería. Al sacar el disco, la alarma despierta a las 4 unidades de las estaciones
                // de carga (Q) y hay que huir hasta el ascensor de la cafetería, al otro lado del nivel.
                Mapa = new[]
                {
                    "###############",
                    "###############",
                    "#n.k#|...|#####",   // oficina de seguridad | sala de servidores
                    "#.W.#|.D.|#####",
                    "#...#|K..|#####",   // Mendoza, muerto frente al disco
                    "##X####R#######",   // puerta sin energía | puerta roja
                    "#Q.........Q###",   // cruce de pasillos: una estación de carga en cada extremo
                    "##L##...##L####",
                    "#f..#.|.#u.P###",   // almacén | pasillo de racks | cuarto eléctrico
                    "#mg.#...#.GQ###",   // estación de carga junto a la terminal de respaldo
                    "#####.|.#######",
                    "#####...###V.Y#",   // cafetería: máquinas expendedoras en la pared norte
                    "###.T...n.#.ME#",   // sala de mantenimiento: terminal y nota | ascensor (salida)
                    "##I.b.S...L..n#",   // puerta de servicio por la que entran los técnicos
                    "###Qm...b.#KM.#",   // estación de carga | Ramírez, con la bayoneta, en la cafetería
                    "###############",
                },
                Zonas = new[]
                {
                    "...............",
                    "...............",
                    ".ooo..sss......",
                    ".ooo..sss......",
                    ".ooo..sss......",
                    "..o....s.......",
                    ".cccccdddddd...",
                    "..w..ppp..e....",
                    ".www.ppp.eee...",
                    ".www.ppp.eee...",
                    ".....ppp.......",
                    ".....ppp...kkk.",
                    "...aaaaaaa.kkk.",
                    "...aaaaaaakkkk.",
                    "...aaaaaaa.kkk.",
                    "...............",
                },
            };

            // Sala ancha: cámara en el centro de la pared sur, mirando hacia la salida norte
            n.Salas['a'] = new DefZona { Nombre = "Mantenimiento", Luz = new Color(0.75f, 0.85f, 1f), Intensidad = 10f, Tema = TemaSala.Mantenimiento,
                                          Camara = new Vector2(6.5f, 14.85f), Mirar = new Vector2(6.5f, 12.4f), AlturaCamara = 3.1f };
            n.Salas['p'] = new DefZona { Nombre = "Pasillo_Racks", Esquina = "NE", Luz = new Color(0.6f, 0.75f, 1f), Intensidad = 8f, Tema = TemaSala.Servidores, Parpadeo = true };
            n.Salas['c'] = new DefZona { Nombre = "Cruce_Oeste", Esquina = "NO", Luz = new Color(0.6f, 0.75f, 1f), Intensidad = 7f, Tema = TemaSala.Pasillo, Parpadeo = true };
            n.Salas['d'] = new DefZona { Nombre = "Cruce_Este", Esquina = "NE", Luz = new Color(1f, 0.15f, 0.1f), Intensidad = 9f, Tema = TemaSala.Pasillo };
            n.Salas['w'] = new DefZona { Nombre = "Almacen", Esquina = "SE", Luz = new Color(1f, 0.75f, 0.45f), Intensidad = 6f, Tema = TemaSala.Almacen };
            n.Salas['e'] = new DefZona { Nombre = "Cuarto_Electrico", Esquina = "SO", Luz = new Color(1f, 0.6f, 0.2f), Intensidad = 7f, Tema = TemaSala.Electrico, Parpadeo = true };
            n.Salas['o'] = new DefZona { Nombre = "Oficina_Seguridad", Esquina = "NE", Luz = new Color(0.75f, 0.85f, 1f), Intensidad = 9f, SinEnergia = true, Tema = TemaSala.Seguridad };
            n.Salas['s'] = new DefZona { Nombre = "Sala_Servidores", Esquina = "NO", Luz = new Color(0.4f, 0.55f, 1f), Intensidad = 9f, Tema = TemaSala.Servidores };
            n.Salas['k'] = new DefZona { Nombre = "Cafeteria", Esquina = "SE", Luz = new Color(1f, 0.9f, 0.75f), Intensidad = 7f, Tema = TemaSala.Cafeteria, Parpadeo = true };

            n.Terminales.Add(("TERMINAL DE MANTENIMIENTO — NEXUS OS", new[]
            {
                "ALERTA CRÍTICA: Fallo en el sistema de refrigeración. Temperatura del núcleo: 80% y en aumento.",
                "La actualización de seguridad CERBERUS 7.1 está corrupta. Puertas principales selladas. Sin comunicación con el exterior.",
                "Protocolo de Seguridad Androide ACTIVADO. Todo humano en los pasillos será tratado como ANOMALÍA y purgado.",
                "Para forzar el reinicio manual se requieren los 3 Discos Duros Maestros antes del colapso térmico.",
                "Disco Maestro 1: Sala de Servidores B-1. Acceso restringido con tarjeta de nivel 2, guardada en la Oficina de Seguridad."
            }, "Consigue la tarjeta de acceso en la Oficina de Seguridad"));

            // Notas en orden de lectura del mapa (de arriba abajo, de izquierda a derecha)
            n.Notas.Add(("Memorándum de Seguridad",
                "CONFIDENCIAL — Dirección de Seguridad, Nexus Corp.\n" +
                "Los androides SENTINEL vigilan con un cono de visión frontal. Si pierden el contacto visual con el objetivo, regresan a su ruta de patrulla.\n" +
                "No intente enfrentarlos: su blindaje resiste hasta 400 °C. Ocúltese detrás de los racks y espere a que pasen.", null));
            n.Notas.Add(("Registro del turno nocturno",
                "Día 14. La actualización CERBERUS 7.1 se instaló a las 02:13. Desde entonces los androides no obedecen al panel central.\n" +
                "Ramírez dice que es un fallo de permisos. Yo no estoy tan seguro: anoche vi a uno detenerse frente a la cámara 4… como si escuchara algo.\n" +
                "Si alguien lee esto: hay repuestos en el Almacén y una terminal de respaldo en el Cuarto Eléctrico. Lleven siempre una USB.", null));
            n.Notas.Add(("Aviso de la cafetería",
                "ADMINISTRACIÓN — Cafetería del sector B.\n" +
                "Las máquinas expendedoras siguen funcionando, pero sueltan los productos con un golpe que se oye en todo el pasillo.\n" +
                "Desde la actualización, los SENTINEL acuden a revisar cualquier ruido. Saquen su bebida energética con cuidado… o no la saquen.", null));

            n.ObjetivoAlRecoger['f'] = "Instala el fusible en el panel del Cuarto Eléctrico";
            n.ObjetivoAlRecoger['k'] = "Abre la puerta roja de la Sala de Servidores";
            n.ObjetivoAlRecoger['D'] = "¡Huye al ascensor de la cafetería!";
            n.Intro = true;
            n.VelAlarma = 3.5f;   // el técnico corriendo (5 m/s) puede dejarlos atrás

            // Cadáveres (en orden de lectura del mapa): lo que pasó antes de que llegaran los técnicos
            n.Cadaveres.Add(new DefCadaver
            {
                Modelo = "Empleado", Pose = "Pared", Quien = "Técnico del turno de noche",
                Lineas = new[]
                {
                    "Otro técnico del turno de noche. Murió sentado contra la pared, mirando el pedestal del disco.",
                    "La placa del uniforme dice MENDOZA. Tiene una tableta rota sobre las piernas."
                },
                TituloNota = "Tableta de Mendoza",
                TextoNota = "02:58. Llegué a la sala B-1. El Disco Maestro 1 sigue en su bahía.\n" +
                            "Si lo saco, CERBERUS dispara la alarma general y despierta a todas las unidades ancladas en las estaciones de carga del sector.\n" +
                            "La única salida que el sistema no controla es el ascensor de servicio de la cafetería. Si alguien termina esto por mí: tome el disco y corra. No se detenga."
            });
            n.Cadaveres.Add(new DefCadaver
            {
                Modelo = "Empleado", Pose = "Tendido", Quien = "Técnico de mantenimiento",
                Lineas = new[]
                {
                    "Es un técnico de mantenimiento. Tiene el pecho quemado, como si lo hubiera atravesado un soldador.",
                    "Junto a su mano hay una bayoneta del kit de emergencia. Intentó defenderse."
                },
                Objeto = TipoColeccionable.Cuchillo, NombreObjeto = "Bayoneta M9",
                TituloNota = "Mensaje sin enviar de J. Ramírez",
                TextoNota = "Día 14, 02:40. Los SENTINEL nos encerraron en la cafetería. Dicen que es un fallo de permisos de CERBERUS, pero el panel central ya no le responde a nadie.\n" +
                            "Mendoza se fue a la sala de servidores a sacar el disco maestro para forzar el reinicio. No ha vuelto.\n" +
                            "Tomé la bayoneta del kit de emergencia. Si alguien lee esto: no corran cerca de ellos, escuchan los pasos. Y el cuchillo solo sirve si los toman por sorpresa."
            });
            n.ObjetivoPanel = "Entra a la Oficina de Seguridad y toma la tarjeta roja";
            n.DialogoDisco = new[]
            {
                "ALERTA: Extracción no autorizada del Disco Maestro 1 (1/3).",
                "Protocolo SENTINEL: unidades redirigidas a la Sala de Servidores B-1. Abandone la zona de inmediato."
            };
            n.TextoSalida = "Usar el ascensor";

            // Androides (columna, fila del mapa). Tutorial: uno lento y predecible rodeando los racks
            // del pasillo. Los otros cuatro están anclados en sus estaciones de carga ('Q') hasta la alarma.
            n.Androides.Add(new DefAndroide
            {
                Ruta = new[] { new Vector2Int(5, 11), new Vector2Int(5, 7), new Vector2Int(7, 7), new Vector2Int(7, 11) },
                VelPatrulla = 1.3f, VelPersecucion = 3.6f
            });

            ConstructorNiveles.Construir(n);
        }
    }
}

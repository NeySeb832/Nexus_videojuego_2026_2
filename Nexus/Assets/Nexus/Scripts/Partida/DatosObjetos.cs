namespace Nexus
{
    /// <summary>Nombre y descripción de cada objeto, para el inventario.</summary>
    public static class DatosObjetos
    {
        public static string Nombre(TipoColeccionable t) => t switch
        {
            TipoColeccionable.DiscoDuro => "Discos Duros Maestros",
            TipoColeccionable.TarjetaRoja => "Tarjeta de acceso roja",
            TipoColeccionable.TarjetaAzul => "Tarjeta de acceso azul",
            TipoColeccionable.Fusible => "Fusible de repuesto",
            TipoColeccionable.USB => "Memoria USB de respaldo",
            TipoColeccionable.Speedmax => "Speedmax",
            TipoColeccionable.Chocorramo => "Chocorramo",
            TipoColeccionable.Refrigerante => "Cartucho de refrigerante",
            _ => t.ToString()
        };

        public static string Descripcion(TipoColeccionable t) => t switch
        {
            TipoColeccionable.DiscoDuro => "Contienen las claves de arranque del sistema Nexus. Se necesitan los tres para forzar el reinicio manual de la refrigeración.",
            TipoColeccionable.TarjetaRoja => "Credencial de seguridad de nivel 2. Abre las puertas de seguridad marcadas en rojo.",
            TipoColeccionable.TarjetaAzul => "Credencial de seguridad de nivel 3. Abre las puertas de seguridad marcadas en azul.",
            TipoColeccionable.Fusible => "Fusible cerámico de 30 A. Sirve para reparar un panel eléctrico con un fusible fundido.",
            TipoColeccionable.USB => "Memoria con el software de respaldo. Permite guardar la partida en una terminal de respaldo. Se consume al usarla.",
            TipoColeccionable.Speedmax => "Bebida energética. Recupera una Oportunidad de Evasión (1 corazón).",
            TipoColeccionable.Chocorramo => "El clásico ponqué de la cafetería. Recupera todas las Oportunidades de Evasión.",
            TipoColeccionable.Refrigerante => "Gas refrigerante a presión. Se usa al recogerlo: suma 20 segundos al temporizador.",
            _ => ""
        };

        public static bool Usable(TipoColeccionable t) => t == TipoColeccionable.Speedmax || t == TipoColeccionable.Chocorramo;
    }
}

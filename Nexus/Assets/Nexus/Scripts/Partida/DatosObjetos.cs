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
            TipoColeccionable.Cuchillo => "Bayoneta M9",
            TipoColeccionable.Pistola => "Pistola M9",
            TipoColeccionable.Escopeta => "Escopeta Remington 870",
            TipoColeccionable.BalasPistola => "Balas de pistola",
            TipoColeccionable.CartuchosEscopeta => "Cartuchos de escopeta",
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
            TipoColeccionable.Cuchillo => "Cuchillo de combate. Ataque cuerpo a cuerpo con clic izquierdo o J. Es silencioso: no atrae a los androides.",
            TipoColeccionable.Pistola => "Semiautomática de 9 mm, cargador de 15 balas. Apunta con clic derecho o K y dispara con clic izquierdo o J. El disparo se oye lejos.",
            TipoColeccionable.Escopeta => "Devastadora a corta distancia: 7 perdigones por disparo, pero pierde fuerza con la distancia. Cargador de 6 cartuchos. Muy ruidosa.",
            TipoColeccionable.BalasPistola => "Munición de 9 mm para la pistola M9. Se recarga con R.",
            TipoColeccionable.CartuchosEscopeta => "Cartuchos calibre 12 para la Remington 870. Se recarga con R.",
            _ => ""
        };

        public static bool EsArma(TipoColeccionable t) =>
            t == TipoColeccionable.Cuchillo || t == TipoColeccionable.Pistola || t == TipoColeccionable.Escopeta;

        public static bool Usable(TipoColeccionable t) =>
            t == TipoColeccionable.Speedmax || t == TipoColeccionable.Chocorramo || EsArma(t);

        /// <summary>Texto de la acción en el inventario.</summary>
        public static string Accion(TipoColeccionable t) =>
            EsArma(t) ? ">> PULSA E / ENTER / TOCA PARA EQUIPAR" : ">> PULSA E / ENTER / TOCA PARA USAR";
    }
}

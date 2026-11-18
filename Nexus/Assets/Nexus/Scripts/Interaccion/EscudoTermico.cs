using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Escudo térmico alrededor del Disco Maestro 3: tres anillos de energía y una barrera sólida.
    /// Cada válvula de refrigeración abierta apaga un anillo; con las tres, la barrera desaparece y
    /// el disco se puede tomar.
    /// </summary>
    public class EscudoTermico : MonoBehaviour
    {
        public static int Abiertas { get; private set; }
        public static int Total { get; private set; } = 3;
        static EscudoTermico instancia;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() { Abiertas = 0; Total = 3; instancia = null; }

        [SerializeField] GameObject[] anillos;
        [SerializeField] Collider barrera;
        [SerializeField] Light luz;
        [SerializeField] int valvulas = 3;

        public void Configurar(GameObject[] capas, Collider solido, Light luzEscudo, int cantidadValvulas)
        {
            anillos = capas; barrera = solido; luz = luzEscudo; valvulas = cantidadValvulas;
        }

        void Awake()
        {
            instancia = this;
            Abiertas = 0;
            Total = Mathf.Max(1, valvulas);
        }

        void OnDestroy() { if (instancia == this) instancia = null; }

        /// <summary>Lo llama cada válvula al abrirse. Devuelve cuántas van.</summary>
        public static int ValvulaAbierta()
        {
            Abiertas = Mathf.Min(Total, Abiertas + 1);
            if (instancia != null) instancia.Actualizar();
            return Abiertas;
        }

        /// <summary>Sobrepresión: todas las válvulas se cerraron y el escudo vuelve completo.</summary>
        public static void Restablecer()
        {
            Abiertas = 0;
            if (instancia == null) return;
            if (instancia.luz != null) instancia.luz.intensity = 5f;
            instancia.Actualizar();
        }

        void Actualizar()
        {
            if (anillos != null)
                for (int i = 0; i < anillos.Length; i++)
                    if (anillos[i] != null) anillos[i].SetActive(i >= Abiertas * anillos.Length / Total);
            bool libre = Abiertas >= Total;
            if (barrera != null) barrera.enabled = !libre;
            if (luz != null) luz.intensity = libre ? 0f : luz.intensity * 0.6f;
            if (libre) HUDNexus.Aviso("ESCUDO TÉRMICO DESACTIVADO. El Disco Maestro 3 está libre.");
        }

        void Update()
        {
            // Los anillos giran y laten: energía inestable
            if (anillos == null) return;
            for (int i = 0; i < anillos.Length; i++)
                if (anillos[i] != null && anillos[i].activeSelf)
                {
                    anillos[i].transform.Rotate(0f, (i % 2 == 0 ? 40f : -55f) * Time.deltaTime, 0f, Space.World);
                    float s = 1f + 0.03f * Mathf.Sin(Time.time * 5f + i);
                    anillos[i].transform.localScale = new Vector3(s * (1f + i * 0.25f), anillos[i].transform.localScale.y, s * (1f + i * 0.25f));
                }
        }
    }
}

using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Pone en el jugador el avatar elegido en el selector de personajes (Neyder o Deisy).
    /// Los dos son Humanoid y comparten el mismo Animator Controller, así que el resto de
    /// scripts funciona igual con cualquiera. Se ejecuta antes que los demás componentes.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ModeloJugador : MonoBehaviour
    {
        [SerializeField] GameObject[] modelos;               // 0 = Neyder, 1 = Deisy
        [SerializeField] RuntimeAnimatorController controlador;

        void Awake()
        {
            int elegido = Mathf.Clamp(EstadoPartida.PersonajeElegido, 0, modelos.Length - 1);
            var actual = transform.Find("Modelo");
            if (actual != null)
            {
                actual.gameObject.SetActive(false);
                Destroy(actual.gameObject);
            }
            var modelo = Instantiate(modelos[elegido], transform);
            modelo.name = "Modelo";
            modelo.transform.localPosition = Vector3.zero;
            modelo.transform.localRotation = Quaternion.identity;
            var animator = modelo.GetComponent<Animator>();
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = controlador;
        }
    }
}

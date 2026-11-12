using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nexus
{
    /// <summary>
    /// Una casilla del inventario. Al seleccionarla (teclado, mando, ratón o toque) muestra el objeto
    /// en la vista previa; al pulsarla intenta usarlo.
    /// </summary>
    public class RanuraInventario : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        public int indice;
        public PantallaInventario pantalla;
        public Image icono;
        public TMPro.TMP_Text texto;

        void Awake() => GetComponent<Button>().onClick.AddListener(() => pantalla.Usar(indice));

        public void OnSelect(BaseEventData e) => pantalla.Seleccionar(indice);

        public void OnPointerEnter(PointerEventData e) => EventSystem.current.SetSelectedGameObject(gameObject);
    }
}

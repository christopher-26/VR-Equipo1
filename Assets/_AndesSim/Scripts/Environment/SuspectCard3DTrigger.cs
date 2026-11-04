using UnityEngine;
using UnityEngine.EventSystems;
using AndesSim.Audio;
using AndesSim.UI;

namespace AndesSim.Environment
{
    /// <summary>
    /// Componente que gestiona la interacción VR con las fichas 3D de sospechosos.
    /// Al posar la mirada o hacer clic con la retícula, reproduce audio y abre
    /// automáticamente el expediente correspondiente en SuspectBoardVR.
    /// </summary>
    public class SuspectCard3DTrigger : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public int suspectIndex = 0;
        public string suspectName = "";
        public Renderer cardRenderer;

        private SuspectBoardVR _cachedBoard;
        private Material _instancedMat;
        private Color _originalColor;

        private void Start()
        {
            _cachedBoard = FindFirstObjectByType<SuspectBoardVR>();
            if (cardRenderer != null)
            {
                _instancedMat = Application.isPlaying ? cardRenderer.material : cardRenderer.sharedMaterial;
                if (_instancedMat != null && _instancedMat.HasProperty("_Color"))
                {
                    _originalColor = _instancedMat.GetColor("_Color");
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SelectSuspect();
        }

        public void SelectSuspect()
        {
            if (AudioManager.I != null)
            {
                AudioManager.I.PlayGazeClick();
            }

            if (_cachedBoard == null)
            {
                _cachedBoard = FindFirstObjectByType<SuspectBoardVR>();
            }

            if (_cachedBoard != null)
            {
                _cachedBoard.DisplaySuspect(suspectIndex);
            }

            Debug.Log($"[SuspectBoardMeshBuilder] <color=#38F28A>Ficha 3D seleccionada:</color> [{suspectIndex}] {suspectName}");
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (AudioManager.I != null)
            {
                AudioManager.I.PlayGazeHover();
            }

            if (_instancedMat != null && _instancedMat.HasProperty("_Color"))
            {
                _instancedMat.SetColor("_Color", new Color(1.0f, 1.0f, 0.94f));
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_instancedMat != null && _instancedMat.HasProperty("_Color"))
            {
                _instancedMat.SetColor("_Color", _originalColor);
            }
        }
    }
}

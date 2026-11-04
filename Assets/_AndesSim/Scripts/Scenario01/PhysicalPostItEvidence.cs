using System.Collections;
using TMPro;
using UnityEngine;
using AndesSim.Core;
using AndesSim.VR;

namespace AndesSim.Scenario01
{
    /// <summary>
    /// Evidencia física del Post-It con contraseñas en texto claro adherido al puesto de Carla Mendoza (RF-01, RF-05).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// - Representa el hallazgo crítico: usuario 'cmendoza_swift', clave 'Andino2015*Swift'.
    /// - Al ser mirado o dwell-clickeado por el jugador, despliega una vista de lectura clara en VR
    ///   y registra la evidencia 'Evidencia_PostIt' en GameManager.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(GazeInteractable))]
    public class PhysicalPostItEvidence : MonoBehaviour
    {
        private const string LogPrefix = "[ANDES-SIM - Post-It Evidence]";
        public const string EvidenceId = "Evidencia_PostIt";

        [Header("--- Datos de la Credencial Expuesta ---")]
        [SerializeField] private string accountUser = "cmendoza_swift";
        [SerializeField] private string accountPassword = "Andino2015*Swift";
        [SerializeField] private string systemScope = "Terminal SWIFT #1 - Autorización de Pagos";

        [Header("--- Visualización en VR ---")]
        [Tooltip("Texto sobre el papel en 3D.")]
        [SerializeField] private TextMeshPro postItNote3DText;

        [Tooltip("Panel flotante de detalle para lectura cómoda a distancia.")]
        [SerializeField] private GameObject inspectionPopup;
        [SerializeField] private TextMeshProUGUI popupDetailText;

        [Tooltip("Indicador visual de evidencia recolectada (icono de check o borde verde).")]
        [SerializeField] private GameObject collectedCheckIndicator;

        [Header("--- Animación y Feedback ---")]
        [Tooltip("Escala al ser enfocado o abierto.")]
        [SerializeField] private float focusScaleMultiplier = 1.25f;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip inspectPaperClip;
        [SerializeField] private AudioClip evidenceRecordedClip;

        // Estado interno
        private GazeInteractable _gazeInteractable;
        private Vector3 _initialScale;
        private bool _isInspected = false;
        private Coroutine _scaleCoroutine;

        private void Awake()
        {
            _gazeInteractable = GetComponent<GazeInteractable>();
            _initialScale = transform.localScale;

            // Configurar capa interactiva
            gameObject.layer = LayerMask.NameToLayer("Interactive");

            if (_gazeInteractable != null)
            {
                _gazeInteractable.interactableLabel = "Inspeccionar Nota Adhesiva (Post-It)";
                _gazeInteractable.onEnter.AddListener(OnGazeEntered);
                _gazeInteractable.onExit.AddListener(OnGazeExited);
                _gazeInteractable.onClick.AddListener(OnGazeClicked);
            }
        }

        private void Start()
        {
            UpdateTextContents();

            if (inspectionPopup != null)
            {
                inspectionPopup.SetActive(false);
            }

            if (collectedCheckIndicator != null)
            {
                collectedCheckIndicator.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_gazeInteractable != null)
            {
                _gazeInteractable.onEnter.RemoveListener(OnGazeEntered);
                _gazeInteractable.onExit.RemoveListener(OnGazeExited);
                _gazeInteractable.onClick.RemoveListener(OnGazeClicked);
            }
        }

        private void UpdateTextContents()
        {
            string fullNote = $"USUARIO: {accountUser}\nCLAVE: {accountPassword}\n[{systemScope}]";

            if (postItNote3DText != null)
            {
                postItNote3DText.text = fullNote;
            }

            if (popupDetailText != null)
            {
                popupDetailText.text = $"<color=#FCE762><b>[EVIDENCIA FÍSICA: POST-IT ADHERIDO AL MONITOR]</b></color>\n\n" +
                                       $"<b>Usuario:</b> <color=#9CDCFE>{accountUser}</color>\n" +
                                       $"<b>Contraseña:</b> <color=#CE9178>{accountPassword}</color>\n" +
                                       $"<b>Alcance:</b> {systemScope}\n\n" +
                                       $"<color=#FF6B6B>Vulnerabilidad grave: Credenciales administrativas expuestas físicamente sin cifrar ni custodia.</color>";
            }
        }

        private void OnGazeEntered()
        {
            AnimateScale(_initialScale * focusScaleMultiplier, 0.15f);
            PlaySound(inspectPaperClip);
        }

        private void OnGazeExited()
        {
            AnimateScale(_initialScale, 0.15f);
        }

        private void OnGazeClicked()
        {
            if (!_isInspected)
            {
                _isInspected = true;
                PlaySound(evidenceRecordedClip);

                Debug.Log($"{LogPrefix} <color=#4EC9B0><b>¡Evidencia física registrada!</b></color> Credenciales expuestas en Post-It.");

                if (collectedCheckIndicator != null)
                {
                    collectedCheckIndicator.SetActive(true);
                }

                // Registrar en GameManager oficial
                if (GameManager.I != null)
                {
                    GameManager.I.RegisterEvidence(EvidenceId);
                }
            }

            // Alternar popup flotante de inspección
            if (inspectionPopup != null)
            {
                inspectionPopup.SetActive(!inspectionPopup.activeSelf);
            }
        }

        private void AnimateScale(Vector3 targetScale, float duration)
        {
            if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = StartCoroutine(ScaleRoutine(targetScale, duration));
        }

        private IEnumerator ScaleRoutine(Vector3 targetScale, float duration)
        {
            Vector3 start = transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(start, targetScale, elapsed / duration);
                yield return null;
            }

            transform.localScale = targetScale;
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}

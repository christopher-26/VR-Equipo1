using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.UI;
using AndesSim.VR;

namespace AndesSim.Scenario01
{
    /// <summary>
    /// Componente de interactividad para las evidencias del Escenario 1: "Oficina de pagos".
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Equipo 1.
    /// - Funciona con GazeInteractable y EventSystems en la capa "Interactive" (Layer 8).
    /// - Gestiona evidencias oficiales:
    ///   • "Evidencia_Correo" (Monitor 01 - Carla Mendoza)
    ///   • "Evidencia_PostIt" (Credenciales físicas bajo el teclado)
    ///   • "Evidencia_LogAccesos" (Monitor 03 - Consola SIEM / SRV-SIEM)
    /// - Al ser examinada con retícula o dwell-click:
    ///   1. Llama a GameManager.I.RegisterEvidence(evidenceId).
    ///   2. Despliega panel flotante 3D con confirmación forense y descripción técnica.
    ///   3. Tiñe el borde o indicador visual al color verde esmeralda (#38f28a).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EvidenceItem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const string LogPrefix = "[ANDES-SIM - EvidenceItem]";
        private static readonly Color CollectedGreenColor = new Color(0.22f, 0.95f, 0.54f, 1.0f); // #38f28a

        [Header("--- Identificación de la Evidencia (RF-05) ---")]
        [Tooltip("Identificador exacto: 'Evidencia_Correo', 'Evidencia_PostIt' o 'Evidencia_LogAccesos'.")]
        [SerializeField] protected string evidenceId = "Evidencia_PostIt";

        [Tooltip("Título descriptivo mostrado en la retícula y el panel forense.")]
        [SerializeField] protected string evidenceTitle = "Credenciales Expuestas en Post-It";

        [TextArea(3, 6)]
        [Tooltip("Descripción forense y técnica de la evidencia en el contexto del incidente de 2015.")]
        [SerializeField] protected string forensicDescription =
            "Contraseña de operador SWIFT escrita en papel adhesivo bajo el teclado. " +
            "Evidencia de negligencia en higiene de contraseñas que permitió a los atacantes " +
            "el uso de credenciales legítimas para autorizar transferencias bancarias fraudulentas.";

        [Header("--- Estado de Recolección ---")]
        [SerializeField] protected bool isCollected = false;

        [Header("--- Panel Flotante Forense 3D ---")]
        [Tooltip("Canvas o GameObject del panel flotante de confirmación forense.")]
        [SerializeField] protected GameObject forensicPopupPanel;
        [SerializeField] protected TextMeshProUGUI popupTitleText;
        [SerializeField] protected TextMeshProUGUI popupDescriptionText;
        [SerializeField] protected Image popupStatusIcon;
        [Tooltip("Tiempo en segundos antes de auto-ocultar el panel (0 para requerir desviar la mirada).")]
        [SerializeField] protected float autoHideDuration = 5.0f;

        [Header("--- Indicador Visual de Borde / Resaltado ---")]
        [Tooltip("Renderer o Image cuyo borde/material cambia a #38f28a al ser recolectada.")]
        [SerializeField] protected Renderer visualIndicatorRenderer;
        [SerializeField] protected Image visualBorderImage;
        [SerializeField] protected int materialIndex = 0;

        [Header("--- Retroalimentación Sonora ---")]
        [SerializeField] protected AudioSource audioSource;
        [SerializeField] protected AudioClip evidenceCollectedClip;
        [SerializeField] protected AudioClip focusHoverClip;

        // Componentes cacheados
        protected GazeInteractable _gazeInteractable;
        protected Collider _collider;
        protected Material _instancedMaterial;
        protected Coroutine _hideCoroutine;

        #region Propiedades Públicas

        public string EvidenceId => evidenceId;
        public string EvidenceTitle => evidenceTitle;
        public string ForensicDescription => forensicDescription;
        public bool IsCollected => isCollected;

        #endregion

        protected virtual void Awake()
        {
            _collider = GetComponent<Collider>();
            _gazeInteractable = GetComponent<GazeInteractable>();

            // Asegurar asignación obligatoria en capa Interactive (Layer 8)
            gameObject.layer = LayerMask.NameToLayer("Interactive");

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            // Configurar GazeInteractable si está presente
            if (_gazeInteractable != null)
            {
                _gazeInteractable.interactableLabel = $"Examinar {evidenceTitle}";
                _gazeInteractable.onClick.AddListener(CollectEvidence);
            }

            // Cachear material para tinte de color #38f28a
            if (visualIndicatorRenderer != null && materialIndex < visualIndicatorRenderer.materials.Length)
            {
                _instancedMaterial = visualIndicatorRenderer.materials[materialIndex];
            }
        }

        protected virtual void Start()
        {
            // Inicializar textos del popup
            if (popupTitleText != null)
            {
                popupTitleText.text = $"EVIDENCIA REGISTRADA: {evidenceTitle.ToUpper()}";
            }

            if (popupDescriptionText != null)
            {
                popupDescriptionText.text = forensicDescription;
            }

            if (forensicPopupPanel != null)
            {
                forensicPopupPanel.SetActive(false);
            }

            UpdateVisualState();
        }

        protected virtual void OnDestroy()
        {
            if (_gazeInteractable != null)
            {
                _gazeInteractable.onClick.RemoveListener(CollectEvidence);
            }
        }

        /// <summary>
        /// Ejecuta el proceso de recolección forense:
        /// 1. Llama a GameManager.I.RegisterEvidence(evidenceId).
        /// 2. Despliega el panel flotante 3D de confirmación.
        /// 3. Cambia el indicador visual al color verde #38f28a.
        /// </summary>
        public virtual void CollectEvidence()
        {
            if (isCollected)
            {
                // Si ya fue recolectada, alternar la vista del panel informativo
                TogglePopupPanel();
                return;
            }

            isCollected = true;
            Debug.Log($"{LogPrefix} <color=#38F28A><b>¡Evidencia Forense Recolectada!</b></color> [{evidenceId}]: {evidenceTitle}");

            // 1. Notificar a GameManager oficial
            if (GameManager.I != null)
            {
                GameManager.I.RegisterEvidence(evidenceId);
            }

            // 2. Reproducir sonido de hallazgo forense
            PlaySound(evidenceCollectedClip);

            // 3. Cambiar borde/indicador a color verde #38f28a
            UpdateVisualState();

            // 4. Mostrar panel 3D informativo
            ShowPopupPanel();

            // 5. Desplegar vista ampliada en primer plano VR (RF-05)
            if (VrInspectionOverlayManager.I != null)
            {
                if (evidenceId.Contains("PostIt") || evidenceId.Contains("Credencial"))
                {
                    VrInspectionOverlayManager.I.OpenCredentialsInspection();
                }
                else if (evidenceId.Contains("Log") || evidenceId.Contains("Acceso"))
                {
                    VrInspectionOverlayManager.I.OpenSiemLogInspection();
                }
                else if (evidenceId.Contains("Correo"))
                {
                    VrInspectionOverlayManager.I.OpenEmailInspection();
                }
            }
        }

        protected virtual void UpdateVisualState()
        {
            if (!isCollected) return;

            // Cambiar Image UI al verde #38f28a
            if (visualBorderImage != null)
            {
                visualBorderImage.color = CollectedGreenColor;
            }

            // Cambiar material 3D al verde #38f28a emisivo
            if (_instancedMaterial != null)
            {
                if (_instancedMaterial.HasProperty("_Color"))
                {
                    _instancedMaterial.SetColor("_Color", CollectedGreenColor);
                }

                if (_instancedMaterial.HasProperty("_EmissionColor"))
                {
                    _instancedMaterial.SetColor("_EmissionColor", CollectedGreenColor * 0.6f);
                    _instancedMaterial.EnableKeyword("_EMISSION");
                }
            }

            // Actualizar etiqueta de mirada
            if (_gazeInteractable != null)
            {
                _gazeInteractable.interactableLabel = $"[RECOLECTADA] {evidenceTitle}";
            }
        }

        private void ShowPopupPanel()
        {
            if (forensicPopupPanel == null) return;

            forensicPopupPanel.SetActive(true);

            if (popupTitleText != null)
            {
                popupTitleText.text = $"<color=#38F28A>[OK] EVIDENCIA REGISTRADA</color>\n<b>{evidenceTitle}</b>";
            }

            if (popupDescriptionText != null)
            {
                popupDescriptionText.text = forensicDescription;
            }

            if (popupStatusIcon != null)
            {
                popupStatusIcon.color = CollectedGreenColor;
            }

            if (autoHideDuration > 0f)
            {
                if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
                _hideCoroutine = StartCoroutine(AutoHideRoutine(autoHideDuration));
            }
        }

        private void TogglePopupPanel()
        {
            if (forensicPopupPanel == null) return;
            bool willShow = !forensicPopupPanel.activeSelf;
            forensicPopupPanel.SetActive(willShow);

            if (willShow && autoHideDuration > 0f)
            {
                if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
                _hideCoroutine = StartCoroutine(AutoHideRoutine(autoHideDuration));
            }
        }

        private IEnumerator AutoHideRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (forensicPopupPanel != null)
            {
                forensicPopupPanel.SetActive(false);
            }
        }

        #region EventSystems Handlers (IPointerClickHandler, etc.)

        public void OnPointerClick(PointerEventData eventData)
        {
            CollectEvidence();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PlaySound(focusHoverClip);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Opcional: auto-ocultar panel al retirar la mirada
        }

        #endregion

        protected virtual void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
            else if (clip == null && audioSource != null && isCollected)
            {
                // Fallback sonoro procedural (tono de campana forense)
                AudioClip proceduralChime = GenerateProceduralChime();
                audioSource.PlayOneShot(proceduralChime);
            }
        }

        private AudioClip GenerateProceduralChime()
        {
            int sampleRate = 44100;
            float duration = 0.5f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-5f * t);
                samples[i] = Mathf.Sin(2f * Mathf.PI * 1046.5f * t) * 0.3f * env; // C6 chime
            }

            AudioClip clip = AudioClip.Create("Procedural_EvidenceChime", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

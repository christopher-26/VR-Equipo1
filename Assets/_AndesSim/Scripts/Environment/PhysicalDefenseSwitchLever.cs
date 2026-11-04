using UnityEngine;
using TMPro;
using AndesSim.Core;
using AndesSim.Scenario01;

namespace AndesSim.Environment
{
    /// <summary>
    /// Controlador físico de la palanca mecánica 3D y del LED reactivo del interruptor de defensa (RF-04).
    /// Realiza la transición suave de la palanca entre OFF (25° abajo) y ON (-25° arriba),
    /// y gestiona el pulso del LED verde brillante #38f28a al activarse en SimMode.Defensa.
    /// </summary>
    public class PhysicalDefenseSwitchLever : MonoBehaviour
    {
        [Header("--- Referencias Físicas ---")]
        public Transform leverTransform;
        public DefenseControl defenseControl;
        public Renderer ledRenderer;
        public Light ledLight;
        public TextMeshProUGUI statusLabel;

        [Header("--- Textos Descriptivos ---")]
        public string defenseTitle = "";
        public string defenseDescription = "";

        [Header("--- Parámetros Cinemáticos ---")]
        public float offAngle = 25f;
        public float onAngle = -25f;
        public float transitionSpeed = 9.0f;

        private Material _ledMaterial;
        private bool _lastAppliedState = false;

        private void Start()
        {
            if (defenseControl == null)
            {
                defenseControl = GetComponent<DefenseControl>();
            }

            if (ledRenderer != null)
            {
                _ledMaterial = Application.isPlaying ? ledRenderer.material : ledRenderer.sharedMaterial;
            }

            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged += HandleModeChanged;
            }

            SyncState(true);
        }

        private void OnDestroy()
        {
            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged -= HandleModeChanged;
            }
        }

        private void Update()
        {
            bool isApplied = defenseControl != null && defenseControl.IsApplied;

            // Si el estado cambió, refrescar etiquetas y luces
            if (isApplied != _lastAppliedState)
            {
                _lastAppliedState = isApplied;
                SyncState(false);
            }

            // Suave interpolación de la rotación de la palanca mecánica
            float targetAngle = isApplied ? onAngle : offAngle;
            Quaternion targetRot = Quaternion.Euler(targetAngle, 0f, 0f);

            if (leverTransform != null)
            {
                leverTransform.localRotation = Quaternion.Slerp(leverTransform.localRotation, targetRot, Time.deltaTime * transitionSpeed);
            }

            // Pulso sutil de alta tecnología cuando el LED está en verde activo
            if (isApplied && _ledMaterial != null)
            {
                float pulse = 1.0f + 0.20f * Mathf.Sin(Time.time * 4.5f);
                Color activeGreen = SuspectBoardMeshBuilder.LedActiveGreen;

                if (_ledMaterial.HasProperty("_EmissionColor"))
                {
                    _ledMaterial.SetColor("_EmissionColor", activeGreen * (1.6f * pulse));
                }

                if (ledLight != null)
                {
                    ledLight.intensity = 1.6f * pulse;
                }
            }
        }

        private void HandleModeChanged(SimMode newMode)
        {
            SyncState(false);
        }

        public void SyncState(bool immediate)
        {
            bool isApplied = defenseControl != null && defenseControl.IsApplied;
            _lastAppliedState = isApplied;

            Color targetColor = isApplied ? SuspectBoardMeshBuilder.LedActiveGreen : SuspectBoardMeshBuilder.LedInactiveRed;
            float targetIntensity = isApplied ? 1.6f : 0.85f;
            string statusStr = isApplied
                ? "<color=#38F28A><b>[OK] CONTRAMEDIDA ACTIVA (MITIGADA)</b></color>"
                : "<color=#FF4444><b>[-] INACTIVO EN 2015 (VULNERABLE)</b></color>";

            // Actualizar palanca mecánica
            float targetAngle = isApplied ? onAngle : offAngle;
            Quaternion targetRot = Quaternion.Euler(targetAngle, 0f, 0f);
            if (leverTransform != null && immediate)
            {
                leverTransform.localRotation = targetRot;
            }

            // Actualizar material del LED
            if (_ledMaterial != null)
            {
                if (_ledMaterial.HasProperty("_Color"))
                {
                    _ledMaterial.SetColor("_Color", targetColor);
                }
                if (_ledMaterial.HasProperty("_EmissionColor"))
                {
                    _ledMaterial.SetColor("_EmissionColor", targetColor * (isApplied ? 1.6f : 0.6f));
                    _ledMaterial.EnableKeyword("_EMISSION");
                }
            }

            // Actualizar luz puntual 3D
            if (ledLight != null)
            {
                ledLight.color = targetColor;
                ledLight.intensity = targetIntensity;
            }

            // Actualizar textos
            if (statusLabel != null)
            {
                statusLabel.text = $"<size=12><b>{defenseTitle}</b></size>\n" +
                                   $"<size=9.5><color=#95A5A6>{defenseDescription}</color></size>\n" +
                                   $"<size=9.5>{statusStr}</size>";
            }
        }
    }
}

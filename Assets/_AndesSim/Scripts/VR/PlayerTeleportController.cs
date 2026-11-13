using System;
using System.Collections;
using AndesSim.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AndesSim.VR
{
    /// <summary>
    /// Controlador central de teletransporte y confort para el rig del Player en VR ("Zero Cybersickness").
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// Se adjunta al objeto 'Player'.
    /// 
    /// Responsabilidades:
    /// 1. Mantiene la referencia al TeleportPoint actual para desactivar su collider y reactivar los otros.
    /// 2. Ejecuta un fundido visual de confort (CanvasGroup frontal) en BlinkTeleportCoroutine(targetPos, targetRot).
    /// 3. Fija rigurosamente la altura de ojos del jugador a 1.65m sobre el suelo del punto destino.
    /// </summary>
    public class PlayerTeleportController : MonoBehaviour
    {
        public static PlayerTeleportController I;

        private const string LogPrefix = "[ANDES-SIM - PlayerTeleport]";

        [Header("--- Altura y Ergonomía VR ---")]
        [Tooltip("Altura fija de los ojos del jugador sobre el suelo destino (1.65 metros estándar).")]
        [SerializeField] private float eyeHeight = 1.65f;

        [Header("--- Confort Visual (Blink Fade) ---")]
        [Tooltip("CanvasGroup frontal negro que ejecuta el fundido rápido.")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;

        [Tooltip("Duración total del parpadeo (0.15 segundos para evitar mareos).")]
        [Range(0.08f, 0.4f)]
        [SerializeField] private float fadeDuration = 0.15f;

        [Header("--- Jerarquía de la Cámara del Jugador ---")]
        [Tooltip("Cámara principal de la cabeza del jugador.")]
        [SerializeField] private Transform playerCamera;

        [Tooltip("Transform intermedio 'CameraOffset' si el rig lo utiliza; de lo contrario, se usa el propio Player.")]
        [SerializeField] private Transform cameraOffset;

        [Header("--- Audio y Retroalimentación ---")]
        [Tooltip("AudioSource para sonido de teletransporte.")]
        [SerializeField] private AudioSource audioSource;

        [Tooltip("Clip de audio reproducido en cada teletransporte.")]
        [SerializeField] private AudioClip teleportSound;

        // Punto de teletransporte actual
        private TeleportPoint _currentTeleportPoint;
        private bool _isTeleporting = false;

        public TeleportPoint CurrentTeleportPoint => _currentTeleportPoint;
        public bool IsTeleporting => _isTeleporting;

        public event Action<Vector3, Quaternion> OnPlayerTeleported;

        private void Awake()
        {
            if (I == null)
            {
                I = this;
            }
            else if (I != this)
            {
                Destroy(this);
                return;
            }

            ResolveCameraReferences();
            EnsureFadeCanvasGroup();

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void ResolveCameraReferences()
        {
            if (playerCamera == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam == null) cam = Camera.main;
                if (cam != null) playerCamera = cam.transform;
            }

            if (cameraOffset == null && playerCamera != null && playerCamera.parent != transform)
            {
                cameraOffset = playerCamera.parent;
            }
        }

        /// <summary>
        /// Teletransporta al jugador hacia un TeleportPoint específico, gestionando los colliders y la altura.
        /// </summary>
        public void TeleportToPoint(TeleportPoint targetPoint)
        {
            if (targetPoint == null || _isTeleporting) return;

            Transform standTransform = targetPoint.StandPosition;
            StartCoroutine(BlinkTeleportCoroutine(standTransform.position, standTransform.rotation, targetPoint));
        }

        /// <summary>
        /// Teletransporta al jugador hacia una posición y rotación arbitrarias (ej. al cruzar el Portal_Entrada).
        /// </summary>
        public void TeleportTo(Vector3 targetFloorPosition, Quaternion targetRotation)
        {
            TeleportToPositionAndRotation(targetFloorPosition, targetRotation);
        }

        /// <summary>
        /// Teletransporta al jugador hacia una posición y rotación arbitrarias (ej. al cruzar el Portal_Entrada).
        /// </summary>
        public void TeleportToPositionAndRotation(Vector3 targetFloorPosition, Quaternion targetRotation)
        {
            if (_isTeleporting) return;

            StartCoroutine(BlinkTeleportCoroutine(targetFloorPosition, targetRotation, null));
        }

        /// <summary>
        /// Corrutina oficial de confort: fundido a negro (0.075s), traslación a 1.65m de altura de ojos, y apertura (0.075s).
        /// </summary>
        public IEnumerator BlinkTeleportCoroutine(Vector3 targetFloorPos, Quaternion targetRot)
        {
            yield return BlinkTeleportCoroutine(targetFloorPos, targetRot, null);
        }

        private IEnumerator BlinkTeleportCoroutine(Vector3 targetFloorPos, Quaternion targetRot, TeleportPoint targetPoint)
        {
            _isTeleporting = true;
            Debug.Log($"{LogPrefix} <b>Iniciando Blink Teleport hacia ({targetFloorPos.x:F1}, {targetFloorPos.y:F1}, {targetFloorPos.z:F1})...</b>");

            EnsureFadeCanvasGroup();

            float halfDuration = Mathf.Max(0.04f, fadeDuration * 0.5f);

            // 1. Fase de cierre de ojos (Fade Out a negro: 0.075s)
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.gameObject.SetActive(true);
                float elapsed = 0f;
                while (elapsed < halfDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / halfDuration);
                    yield return null;
                }
                fadeCanvasGroup.alpha = 1f;
            }

            // 2. Reposicionamiento instantáneo y ajuste riguroso de altura de ojos a 1.65m
            ApplyPositionAndHeight(targetFloorPos, targetRot);

            // 3. Gestión de colliders de puntos de teletransporte
            UpdateActiveTeleportPoints(targetPoint);

            // Reproducir efecto sonoro
            PlayTeleportSound(targetFloorPos);

            // Breve intervalo en negro absoluto
            yield return new WaitForSecondsRealtime(0.02f);

            // 4. Fase de apertura de ojos (Fade In desde negro: 0.075s)
            if (fadeCanvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < halfDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    fadeCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / halfDuration);
                    yield return null;
                }
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.gameObject.SetActive(false);
            }

            _isTeleporting = false;
            OnPlayerTeleported?.Invoke(targetFloorPos, targetRot);
            NotifyArrivalNarration(targetPoint);
            Debug.Log($"{LogPrefix} <color=#4EC9B0>Teletransporte completado con éxito. Ojos a {eyeHeight}m sobre el suelo.</color>");
        }

        private void NotifyArrivalNarration(TeleportPoint targetPoint)
        {
            if (targetPoint == null || AndesSim.Audio.AudioManager.I == null) return;
            string n = targetPoint.gameObject.name;
            if (n.IndexOf("Tablero", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AndesSim.Audio.AudioManager.I.PlayNarration("Voice_19_TableroSospechosos", true, 6.0f);
            }
            else if (n.IndexOf("ConsolaDefensa", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AndesSim.Audio.AudioManager.I.PlayNarration("Voice_18_ConsolaDefensaZona", true, 6.0f);
            }
            else if (n.IndexOf("Puesto2", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AndesSim.Audio.AudioManager.I.PlayNarration("Voice_14_TerminalSwift", false, 8.0f);
            }
            else if (n.IndexOf("Puesto3", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AndesSim.Audio.AudioManager.I.PlayNarration("Voice_15_ConsolaSiem", false, 8.0f);
            }
        }

        /// <summary>
        /// Aplica la posición y orientación manteniendo los ojos a 1.65m sobre el piso.
        /// </summary>
        private void ApplyPositionAndHeight(Vector3 targetFloorPos, Quaternion targetRot)
        {
            // Posicionar la raíz del Player en el piso
            Vector3 finalPlayerPosition = targetFloorPos;

            // Si el rig no tiene CameraOffset dedicado, elevar la cámara o el rig para asegurar 1.65m
            if (cameraOffset != null)
            {
                transform.position = finalPlayerPosition;
                cameraOffset.localPosition = new Vector3(0f, eyeHeight, 0f);
            }
            else if (playerCamera != null && playerCamera != transform)
            {
                transform.position = finalPlayerPosition;
                // Ajustar posición local de la cámara
                Vector3 camLocal = playerCamera.localPosition;
                camLocal.y = eyeHeight;
                playerCamera.localPosition = camLocal;
            }
            else
            {
                // Si la cámara es el propio objeto Player
                finalPlayerPosition.y += eyeHeight;
                transform.position = finalPlayerPosition;
            }

            // Orientar el frente hacia el escritorio sin rotación en X ni Z
            Vector3 targetForward = targetRot * Vector3.forward;
            targetForward.y = 0f;

            if (targetForward.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(targetForward.normalized, Vector3.up);
            }
        }

        /// <summary>
        /// Desactiva el collider del TeleportPoint actual y reactiva todos los demás en la escena.
        /// </summary>
        private void UpdateActiveTeleportPoints(TeleportPoint newCurrentPoint)
        {
            _currentTeleportPoint = newCurrentPoint;

            TeleportPoint[] allPoints = FindObjectsByType<TeleportPoint>(FindObjectsSortMode.None);
            for (int i = 0; i < allPoints.Length; i++)
            {
                TeleportPoint point = allPoints[i];
                if (point == null) continue;

                Collider col = point.GetComponent<Collider>();
                GazeInteractable gaze = point.GetComponent<GazeInteractable>();

                bool isCurrent = (point == _currentTeleportPoint);

                // Desactivar el colisionador y gaze en el punto donde el jugador ya está parado
                if (col != null) col.enabled = !isCurrent;
                if (gaze != null) gaze.enabled = !isCurrent;

                if (isCurrent)
                {
                    Debug.Log($"{LogPrefix} Desactivado colisionador del punto actual: '{point.PointName}' para evitar re-teletransporte redundante.");
                }
            }
        }

        private void PlayTeleportSound(Vector3 atPosition)
        {
            if (teleportSound != null)
            {
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(teleportSound);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(teleportSound, atPosition);
                }
            }
            else if (AndesSim.Audio.AudioManager.I != null)
            {
                AndesSim.Audio.AudioManager.I.PlayTeleportWhoosh(atPosition);
            }
        }

        /// <summary>
        /// Crea dinámicamente el CanvasGroup de confort visual si no fue conectado en el Inspector.
        /// </summary>
        private void EnsureFadeCanvasGroup()
        {
            if (fadeCanvasGroup != null) return;

            if (playerCamera == null) ResolveCameraReferences();
            if (playerCamera == null) return;

            GameObject faderGO = new GameObject("PlayerBlinkFaderCanvas");
            faderGO.transform.SetParent(playerCamera, false);
            faderGO.transform.localPosition = new Vector3(0f, 0f, 0.11f); // 11 cm frente a la lente
            faderGO.transform.localRotation = Quaternion.identity;
            faderGO.transform.localScale = Vector3.one;

            Canvas canvas = faderGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 32767;

            RectTransform canvasRect = faderGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(4f, 4f);

            GameObject blackImageGO = new GameObject("BlackFaderImage");
            blackImageGO.transform.SetParent(faderGO.transform, false);

            Image img = blackImageGO.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;

            RectTransform imgRect = blackImageGO.GetComponent<RectTransform>();
            imgRect.sizeDelta = new Vector2(6f, 6f);
            imgRect.localPosition = Vector3.zero;

            fadeCanvasGroup = faderGO.AddComponent<CanvasGroup>();
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.interactable = false;
            fadeCanvasGroup.blocksRaycasts = false;

            faderGO.SetActive(false);
            Debug.Log($"{LogPrefix} CanvasGroup de confort visual (Blink) generado proceduralmente frente a la cámara.");
        }
    }
}

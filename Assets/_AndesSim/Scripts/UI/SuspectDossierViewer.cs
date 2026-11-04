using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.Data;
using AndesSim.VR;

namespace AndesSim.UI
{
    /// <summary>
    /// Visualizador e investigatorio interactivo de sospechosos de complicidad interna (RF-02).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// - Permite examinar los expedientes de los 4 sospechosos oficiales:
    ///   1. Ing. Samuel Torres (Jefe de TI)
    ///   2. Lic. Hugo Benavides (Director adjunto de Pagos)
    ///   3. Carla Mendoza (Operadora SWIFT)
    ///   4. Renzo Aliaga (Técnico de proveedor externo)
    /// - Muestra indicios en contra, coartadas a favor y notas de investigación.
    /// - Permite al equipo seleccionar su hipótesis de investigación guardándola en ProgressTracker.
    /// </summary>
    public class SuspectDossierViewer : MonoBehaviour
    {
        private const string LogPrefix = "[ANDES-SIM - Dossier Viewer]";

        [Header("--- Base de Datos de Sospechosos ---")]
        [SerializeField] private SuspectDatabase suspectDatabase;

        [Header("--- Botones de Navegación entre Expedientes ---")]
        [SerializeField] private Button prevSuspectButton;
        [SerializeField] private Button nextSuspectButton;
        [SerializeField] private Button selectHypothesisButton;

        [Header("--- Elementos de UI del Expediente ---")]
        [SerializeField] private TextMeshProUGUI suspectNameText;
        [SerializeField] private TextMeshProUGUI suspectRoleText;
        [SerializeField] private Image portraitImage;

        [SerializeField] private TextMeshProUGUI backgroundSummaryText;
        [SerializeField] private TextMeshProUGUI incriminatingFactsText;
        [SerializeField] private TextMeshProUGUI exoneratingFactsText;
        [SerializeField] private TextMeshProUGUI investigationNotesText;

        [Header("--- Indicador de Hipótesis Seleccionada ---")]
        [SerializeField] private GameObject hypothesisSelectedStamp;
        [SerializeField] private TextMeshProUGUI hypothesisStatusNoticeText;

        [Header("--- Efectos Sonoros ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip flipPageClip;
        [SerializeField] private AudioClip stampHypothesisClip;

        // Estado interno
        private int _currentIndex = 0;
        private List<SuspectProfile> _suspectsList = new List<SuspectProfile>();

        private void Start()
        {
            LoadDatabase();
            SetupButtons();
            DisplaySuspect(_currentIndex);
        }

        private void LoadDatabase()
        {
            if (suspectDatabase == null)
            {
                suspectDatabase = Resources.Load<SuspectDatabase>("Data/Suspects_Data");
            }

            if (suspectDatabase != null && suspectDatabase.Suspects != null)
            {
                _suspectsList = new List<SuspectProfile>(suspectDatabase.Suspects);
            }

            // Si aún está vacía, generar base de datos por defecto
            if (_suspectsList.Count == 0)
            {
                SuspectDatabase fallback = SuspectDatabase.CreateDefaultDatabase();
                _suspectsList = new List<SuspectProfile>(fallback.Suspects);
            }
        }

        private void SetupButtons()
        {
            if (prevSuspectButton != null)
            {
                prevSuspectButton.onClick.AddListener(ShowPreviousSuspect);
            }

            if (nextSuspectButton != null)
            {
                nextSuspectButton.onClick.AddListener(ShowNextSuspect);
            }

            if (selectHypothesisButton != null)
            {
                selectHypothesisButton.onClick.AddListener(ConfirmHypothesisSelection);
            }
        }

        public void ShowNextSuspect()
        {
            if (_suspectsList.Count == 0) return;
            _currentIndex = (_currentIndex + 1) % _suspectsList.Count;
            PlaySound(flipPageClip);
            DisplaySuspect(_currentIndex);
        }

        public void ShowPreviousSuspect()
        {
            if (_suspectsList.Count == 0) return;
            _currentIndex--;
            if (_currentIndex < 0) _currentIndex = _suspectsList.Count - 1;
            PlaySound(flipPageClip);
            DisplaySuspect(_currentIndex);
        }

        /// <summary>
        /// Muestra en la interfaz los datos del sospechoso en el índice indicado.
        /// </summary>
        public void DisplaySuspect(int index)
        {
            if (_suspectsList == null || index < 0 || index >= _suspectsList.Count) return;

            SuspectProfile suspect = _suspectsList[index];
            if (suspect == null) return;

            if (suspectNameText != null) suspectNameText.text = suspect.fullName;
            if (suspectRoleText != null) suspectRoleText.text = $"Cargo: <b>{suspect.roleTitle}</b>";

            if (portraitImage != null && suspect.portraitPhoto != null)
            {
                portraitImage.sprite = suspect.portraitPhoto;
                portraitImage.enabled = true;
            }

            // Contexto y resumen
            if (backgroundSummaryText != null)
            {
                backgroundSummaryText.text = suspect.backgroundSummary;
            }

            // Indicios y coartadas
            if (incriminatingFactsText != null)
            {
                incriminatingFactsText.text = $"<b>Indicios incriminatorios:</b>\n• {suspect.incriminatingPoints}";
            }

            if (exoneratingFactsText != null)
            {
                exoneratingFactsText.text = $"<b>Factores eximentes / Coartada:</b>\n• {suspect.exoneratingPoints}";
            }

            if (investigationNotesText != null)
            {
                investigationNotesText.text = $"<i>Conclusión: {suspect.dossierNotes}</i>";
            }

            // Verificar si este sospechoso ya fue marcado como hipótesis actual
            string selectedId = (ProgressTracker.I != null) ? ProgressTracker.I.SelectedSuspectHypothesis : string.Empty;
            bool isCurrentHypothesis = string.Equals(selectedId, suspect.id, StringComparison.OrdinalIgnoreCase);

            if (hypothesisSelectedStamp != null)
            {
                hypothesisSelectedStamp.SetActive(isCurrentHypothesis);
            }

            if (hypothesisStatusNoticeText != null)
            {
                hypothesisStatusNoticeText.text = isCurrentHypothesis
                    ? "[*] HIPÓTESIS PRINCIPAL REGISTRADA POR EL EQUIPO [*]"
                    : "Mirar y pulsar para fijar como hipótesis principal de cómplice interno.";
                hypothesisStatusNoticeText.color = isCurrentHypothesis ? Color.green : Color.white;
            }
        }

        /// <summary>
        /// Guarda al sospechoso actual como la hipótesis del equipo en ProgressTracker.
        /// </summary>
        public void ConfirmHypothesisSelection()
        {
            if (_suspectsList == null || _currentIndex < 0 || _currentIndex >= _suspectsList.Count) return;

            SuspectProfile suspect = _suspectsList[_currentIndex];
            if (suspect == null) return;

            PlaySound(stampHypothesisClip);

            if (ProgressTracker.I != null)
            {
                ProgressTracker.I.SetSelectedSuspectHypothesis(suspect.id);
            }

            Debug.Log($"{LogPrefix} <color=#4EC9B0><b>Hipótesis registrada:</b></color> {suspect.fullName} ({suspect.id})");

            DisplaySuspect(_currentIndex);
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

using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.VR;

namespace AndesSim.Scenario01
{
    /// <summary>
    /// Controlador del visor de registros de red y correo en Monitor_03 (Puesto 3: Consola SIEM).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// Hereda de EvidenceItem con evidenceId = "Evidencia_LogAccesos".
    /// 
    /// Comportamiento:
    /// • En SimMode.Ataque:
    ///   Muestra las líneas históricas del log del 29/01/2015:
    ///   - [09:14:02] SMTP IN: rasel.ahlam@career-applicants-bd.com -> 16 destinatarios internos
    ///   - [09:18:44] HTTP GET: PC-PAGOS-01 (C. Mendoza) intenta abrir Rasel_Ahlam_CV_2015.scr.zip
    ///   - [09:21:10] HTTP GET: PC-PAGOS-02 intenta abrir Rasel_Ahlam_CV_2015.scr.zip
    ///   - [09:25:03] HTTP GET: PC-PAGOS-09 intenta abrir Rasel_Ahlam_CV_2015.scr.zip
    ///   - [09:25:09] ALERTA SILENCIADA: Proceso backdoor iniciado en red plana hacia nodo SWIFT
    /// • Botón interactivo "Registrar Log como Evidencia Forense" que invoca GameManager.I.RegisterEvidence("Evidencia_LogAccesos").
    /// • En SimMode.Defensa:
    ///   El log se actualiza en vivo mostrando cómo el Sandbox detonó el ZIP y bloqueó la entrega a los 16 buzones.
    /// </summary>
    public class AccessLogTerminal : EvidenceItem
    {
        private const string TerminalLogPrefix = "[ANDES-SIM - SIEM Terminal]";

        [Header("--- Elementos de UI de la Consola SIEM (World Space) ---")]
        [SerializeField] private TextMeshProUGUI terminalHeaderText;
        [SerializeField] private TextMeshProUGUI logConsoleBodyText;
        [SerializeField] private TextMeshProUGUI terminalStatusBannerText;
        [SerializeField] private Image terminalStatusBannerBg;

        [Header("--- Botón de Registro Forense ---")]
        [Tooltip("Botón interactivo por mirada para registrar el log como evidencia forense.")]
        [SerializeField] private Button registerLogEvidenceButton;
        [SerializeField] private TextMeshProUGUI registerButtonLabelText;
        [SerializeField] private Image registerButtonBgImage;

        [Header("--- Efectos y Audio de Consola ---")]
        [SerializeField] private AudioClip terminalKeyBeepClip;
        [SerializeField] private AudioClip alarmBackdoorClip;
        [SerializeField] private AudioClip sandboxMitigatedClip;

        // Estado interno
        private bool _isDefenseActive = false;

        protected override void Awake()
        {
            // Metadatos de EvidenceItem
            evidenceId = "Evidencia_LogAccesos";
            evidenceTitle = "Registro SIEM de Conexiones y Tráfico Anómalo (29 ENE 2015)";
            forensicDescription =
                "Auditoría técnica del servidor SIEM: Registros de eventos que confirman la entrega del correo malicioso " +
                "a 16 buzones de la oficina, la apertura sucesiva del archivo .scr.zip en 3 equipos locales (PC-PAGOS-01, 02 y 09) " +
                "y el establecimiento silencioso de un canal C2 con la IP 185.130.44.82 debido a la ausencia de segmentación en la red plana.";

            base.Awake();
        }

        protected override void Start()
        {
            base.Start();

            if (registerLogEvidenceButton != null)
            {
                registerLogEvidenceButton.onClick.AddListener(OnRegisterButtonClicked);
            }

            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged += HandleModeChanged;
                GameManager.I.OnDefenseApplied += HandleDefenseApplied;
                GameManager.I.OnEvidenceRegistered += HandleEvidenceRegistered;
            }

            RefreshTerminalDisplay();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (registerLogEvidenceButton != null)
            {
                registerLogEvidenceButton.onClick.RemoveListener(OnRegisterButtonClicked);
            }

            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged -= HandleModeChanged;
                GameManager.I.OnDefenseApplied -= HandleDefenseApplied;
                GameManager.I.OnEvidenceRegistered -= HandleEvidenceRegistered;
            }
        }

        private void HandleModeChanged(SimMode mode)
        {
            RefreshTerminalDisplay();
        }

        private void HandleDefenseApplied(string defenseId)
        {
            RefreshTerminalDisplay();
        }

        private void HandleEvidenceRegistered(string registeredId)
        {
            if (string.Equals(registeredId, evidenceId, System.StringComparison.OrdinalIgnoreCase))
            {
                isCollected = true;
                UpdateRegisterButtonVisuals();
            }
        }

        private void OnRegisterButtonClicked()
        {
            PlaySound(terminalKeyBeepClip);
            CollectEvidence();
            UpdateRegisterButtonVisuals();
        }

        public override void CollectEvidence()
        {
            base.CollectEvidence();
            UpdateRegisterButtonVisuals();
        }

        /// <summary>
        /// Actualiza todo el contenido del log en pantalla según el modo activo.
        /// </summary>
        public void RefreshTerminalDisplay()
        {
            SimMode mode = (GameManager.I != null) ? GameManager.I.mode : SimMode.Ataque;
            _isDefenseActive = (mode == SimMode.Defensa);

            if (_isDefenseActive)
            {
                RenderDefenseModeLog();
            }
            else
            {
                RenderAttackModeLog();
            }

            UpdateRegisterButtonVisuals();
        }

        /// <summary>
        /// Muestra el log histórico del 29/01/2015 con las 3 infecciones y el backdoor C2.
        /// </summary>
        private void RenderAttackModeLog()
        {
            if (terminalHeaderText != null)
            {
                terminalHeaderText.text = "<b>SIEM SRV-01 · BANCO ANDINO</b>\n" +
                                          "<size=85%>29 ENE 2015 | Modo Ataque</size>";
            }

            if (terminalStatusBannerText != null)
            {
                terminalStatusBannerText.text = "ESTADO: <color=#FF4444><b>BACKDOOR ACTIVO</b></color>";
            }

            if (terminalStatusBannerBg != null)
            {
                terminalStatusBannerBg.color = new Color(0.85f, 0.15f, 0.15f, 0.85f);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<color=#A0A0A0>[09:14]</color> Correo externo -> 16 buzones (c.mendoza)");
            sb.AppendLine("<color=#A0A0A0>[09:18]</color> <b>PC 1</b> abre <color=#FCE762>CV_2015.scr.zip</color>");
            sb.AppendLine("<color=#A0A0A0>[09:21]</color> <b>PC 2 y PC 3</b> infectadas por red plana");
            sb.AppendLine("<color=#FF4444><b>[09:25] ALERTA: svchost_upd.exe -> IP C2 .44.82</b></color>");

            if (logConsoleBodyText != null)
            {
                logConsoleBodyText.text = sb.ToString();
            }
        }

        /// <summary>
        /// Muestra el log en vivo de la mitigación perimetral por Sandbox y Firewall.
        /// </summary>
        private void RenderDefenseModeLog()
        {
            if (terminalHeaderText != null)
            {
                terminalHeaderText.text = "<b>SIEM SRV-01 · BANCO ANDINO</b>\n" +
                                          "<size=85%>29 ENE 2015 | Modo Defensa</size>";
            }

            if (terminalStatusBannerText != null)
            {
                terminalStatusBannerText.text = "ESTADO: <color=#38F28A><b>RED PROTEGIDA (0 INFECTADOS)</b></color>";
            }

            if (terminalStatusBannerBg != null)
            {
                terminalStatusBannerBg.color = new Color(0.12f, 0.65f, 0.28f, 0.85f);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<color=#A0A0A0>[09:14:02]</color> Correo detectado hacia 16 buzones");
            sb.AppendLine("<color=#38F28A>[09:14:05]</color> Sandbox detona <color=#FCE762>CV_2015.scr.zip</color>");
            sb.AppendLine("<color=#38F28A>[09:14:08]</color> Bloqueo Firewall a IP 185.130.44.82");
            sb.AppendLine("<color=#38F28A><b>[09:14:09] 16/16 buzones purgados</b></color>");

            if (logConsoleBodyText != null)
            {
                logConsoleBodyText.text = sb.ToString();
            }
        }

        private void UpdateRegisterButtonVisuals()
        {
            if (registerButtonLabelText != null)
            {
                registerButtonLabelText.text = isCollected
                    ? "[OK] LOG REGISTRADO"
                    : "REGISTRAR LOG SIEM";
            }

            if (registerButtonBgImage != null)
            {
                registerButtonBgImage.color = isCollected
                    ? new Color(0.22f, 0.95f, 0.54f, 0.9f) // Verde #38f28a
                    : new Color(0.18f, 0.45f, 0.85f, 0.9f); // Azul corporativo
            }
        }
    }
}

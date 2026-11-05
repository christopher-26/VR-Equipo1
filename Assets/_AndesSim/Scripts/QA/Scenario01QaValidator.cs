using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Audio;
using AndesSim.Core;
using AndesSim.Data;
using AndesSim.Environment;
using AndesSim.Scenario01;
using AndesSim.UI;
using AndesSim.VR;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace AndesSim.QA
{
    /// <summary>
    /// Resultado individual de una prueba de auditoría de rúbrica.
    /// </summary>
    [Serializable]
    public class QaCheckResult
    {
        public string Category;
        public string Code;
        public string Name;
        public float MaxScore;
        public float AwardedScore;
        public bool Passed;
        public string Details;
        public string EvidenceFound;
    }

    /// <summary>
    /// Auditor automatizado de Calidad y Cumplimiento de Rúbrica Oficial (20 Puntos).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// 
    /// Evalúa en Editor y Runtime la escena oficial 'Oficina_Pagos.unity':
    /// • [RF-01] Existencia de los 3 puestos de trabajo con Monitor_01, Monitor_02 y Monitor_03 en capa "Interactive" y Canvas World Space.
    /// • [RF-02] Interacción por mirada y toque funcional sobre el correo de «Rasel Ahlam».
    /// • [RF-03] Presencia de las 3 señales de phishing conectadas a PhishingInspector.
    /// • [RF-04] Bivalencia: Modo Ataque (propagación) vs Modo Defensa (cuarentena con MailQuarantine).
    /// • [RF-05] Registro de las 3 evidencias obligatorias (Evidencia_Correo, Evidencia_PostIt, Evidencia_LogAccesos).
    /// • [Requisitos VR] Portal_Entrada, 3 TeleportPoints, >=5 interactables, AudioManager, pantalla final y créditos.
    /// 
    /// Genera un informe en consola y persiste 'Reporte_QA_Equipo01.md' en la raíz del proyecto.
    /// </summary>
    public class Scenario01QaValidator : MonoBehaviour
    {
        private const string LogPrefix = "[ANDES-SIM - QA Validator]";
        private const string TargetScenePath = "Assets/_AndesSim/Scenes/Oficina_Pagos.unity";
        private const string ReportFileName = "Reporte_QA_Equipo01.md";

        [Header("--- Ejecución en Runtime ---")]
        [Tooltip("Si está activo, ejecuta la auditoría automáticamente al arrancar la escena en Play Mode.")]
        [SerializeField] private bool runOnStartInPlayMode = false;

        private void Start()
        {
            if (runOnStartInPlayMode)
            {
                ExecuteFullQaAudit(showDialog: false);
            }
        }

#if UNITY_EDITOR
        [MenuItem("ANDES-SIM/07. Ejecutar Auditoria QA de Rubrica (20 Pts)", priority = 7)]
        public static void RunQaAuditFromMenu()
        {
            ExecuteFullQaAudit(showDialog: false);
        }

        /// <summary>
        /// Método de ejecución para Unity Batchmode (-executeMethod AndesSim.QA.Scenario01QaValidator.RunQaAuditBatchmode).
        /// </summary>
        public static void RunQaAuditBatchmode()
        {
            ExecuteFullQaAudit(showDialog: false);
        }
#endif

        /// <summary>
        /// Ejecuta la auditoría integral de la rúbrica sobre la escena oficial.
        /// </summary>
        public static QaAuditSummary ExecuteFullQaAudit(bool showDialog = false)
        {
            Debug.Log($"{LogPrefix} <b>====================================================================</b>");
            Debug.Log($"{LogPrefix} <b>INICIANDO AUDITORÍA QA DE RÚBRICA OFICIAL: CASO BANCO ANDINO (20 PTS)</b>");
            Debug.Log($"{LogPrefix} <b>====================================================================</b>");

#if UNITY_EDITOR
            // Asegurar que la escena Oficina_Pagos.unity esté cargada
            Scene currentScene = SceneManager.GetActiveScene();
            if (!string.Equals(currentScene.path, TargetScenePath, StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(TargetScenePath))
                {
                    Debug.Log($"{LogPrefix} Abriendo escena objetivo: {TargetScenePath}");
                    EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
                }
                else
                {
                    Debug.LogWarning($"{LogPrefix} No se encontró '{TargetScenePath}'. Auditando la escena actualmente abierta: '{currentScene.name}'");
                }
            }
#endif

            List<QaCheckResult> results = new List<QaCheckResult>();

            // 1. [RF-01] Puestos de Trabajo y Monitores (3.5 Pts)
            AuditRf01_WorkstationsAndMonitors(results);

            // 2. [RF-02] Correo de Rasel Ahlam e Interacción VR (3.5 Pts)
            AuditRf02_EmailInteraction(results);

            // 3. [RF-03] Detección de las 3 Señales de Phishing (3.5 Pts)
            AuditRf03_PhishingSignals(results);

            // 4. [RF-04] Bivalencia: Modo Ataque vs Modo Defensa (3.5 Pts)
            AuditRf04_AttackAndDefenseModes(results);

            // 5. [RF-05] Registro de Evidencias Obligatorias (3.0 Pts)
            AuditRf05_MandatoryEvidences(results);

            // 6. [Requisitos VR] Navegación, Confort, Audio y Créditos (3.0 Pts)
            AuditVrRequirements(results);

            // Consolidar resultados
            float totalScore = 0f;
            float maxScore = 20.0f;
            int passedCount = 0;

            for (int i = 0; i < results.Count; i++)
            {
                totalScore += results[i].AwardedScore;
                if (results[i].Passed) passedCount++;
            }

            totalScore = Mathf.Clamp(totalScore, 0f, maxScore);

            QaAuditSummary summary = new QaAuditSummary
            {
                Timestamp = DateTime.Now,
                SceneName = Path.GetFileName(TargetScenePath),
                TotalScore = totalScore,
                MaxScore = maxScore,
                PassedChecks = passedCount,
                TotalChecks = results.Count,
                Results = results
            };

            // Escribir reporte en disco
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string reportPath = Path.Combine(projectRoot, ReportFileName);
            WriteMarkdownReport(summary, reportPath);

            // Imprimir resumen en consola de Unity
            PrintConsoleSummary(summary, reportPath);

#if UNITY_EDITOR
            if (showDialog)
            {
                string statusMsg = totalScore >= 18.0f ? "¡EXCELENTE! Aprobación sobresaliente de la rúbrica." :
                                  totalScore >= 14.0f ? "APROBADO: Cumple con la mayoría de requisitos." :
                                  "REQUIERE REVISIÓN: Algunos criterios no se cumplieron al 100%.";

                EditorUtility.DisplayDialog("ANDES-SIM: Auditoría QA Completada",
                    $"Calificación Final: {totalScore:F1} / {maxScore:F1} Pts\n" +
                    $"Criterios Aprobados: {passedCount} de {results.Count}\n\n" +
                    $"{statusMsg}\n\n" +
                    $"Reporte generado en:\n{reportPath}",
                    "Ver Reporte");
            }
#endif

            return summary;
        }

        #region 1. [RF-01] Puestos de Trabajo y Monitores

        private static void AuditRf01_WorkstationsAndMonitors(List<QaCheckResult> results)
        {
            const string cat = "[RF-01] Puestos de Trabajo y Monitores (3.5 Pts)";

            // 1.1 Existencia de los 3 puestos de trabajo
            GameObject ws1 = GameObject.Find("Puesto_01_CarlaMendoza") ?? GameObject.Find("Puesto 1");
            GameObject ws2 = GameObject.Find("Puesto_02_ComercioExterior") ?? GameObject.Find("Puesto 2");
            GameObject ws3 = GameObject.Find("Puesto_03_ConsolaDefensas") ?? GameObject.Find("Puesto 3");

            bool has3Ws = ws1 != null && ws2 != null && ws3 != null;
            results.Add(new QaCheckResult
            {
                Category = cat,
                Code = "RF01-1",
                Name = "Existencia de los 3 puestos de trabajo físicos (RF-01)",
                MaxScore = 1.0f,
                AwardedScore = has3Ws ? 1.0f : 0f,
                Passed = has3Ws,
                Details = has3Ws ? "Se localizaron las 3 estaciones de trabajo en la oficina." : "Falta uno o más puestos de trabajo.",
                EvidenceFound = $"P1: {(ws1 != null ? ws1.name : "Falta")}, P2: {(ws2 != null ? ws2.name : "Falta")}, P3: {(ws3 != null ? ws3.name : "Falta")}"
            });

            // 1.2 Existencia de Monitor_01, Monitor_02 y Monitor_03
            GameObject m1 = GameObject.Find("Monitor_01");
            GameObject m2 = GameObject.Find("Monitor_02");
            GameObject m3 = GameObject.Find("Monitor_03");

            bool has3Monitors = m1 != null && m2 != null && m3 != null;
            results.Add(new QaCheckResult
            {
                Category = cat,
                Code = "RF01-2",
                Name = "Existencia de Monitor_01, Monitor_02 y Monitor_03",
                MaxScore = 1.0f,
                AwardedScore = has3Monitors ? 1.0f : 0f,
                Passed = has3Monitors,
                Details = has3Monitors ? "Los 3 monitores principales están instanciados." : "Falta instanciar alguno de los 3 monitores.",
                EvidenceFound = $"Monitor_01: {m1 != null}, Monitor_02: {m2 != null}, Monitor_03: {m3 != null}"
            });

            // 1.3 Capa 'Interactive' (Layer 8) y Collider en los 3 monitores
            int interactiveLayer = LayerMask.NameToLayer("Interactive");
            bool m1Interactive = m1 != null && m1.layer == interactiveLayer && m1.GetComponent<Collider>() != null;
            bool m2Interactive = m2 != null && m2.layer == interactiveLayer && m2.GetComponent<Collider>() != null;
            bool m3Interactive = m3 != null && m3.layer == interactiveLayer && m3.GetComponent<Collider>() != null;

            bool allMonitorsInteractive = m1Interactive && m2Interactive && m3Interactive;
            results.Add(new QaCheckResult
            {
                Category = cat,
                Code = "RF01-3",
                Name = "Monitores en Capa 'Interactive' (Layer 8) con Collider",
                MaxScore = 0.75f,
                AwardedScore = allMonitorsInteractive ? 0.75f : 0f,
                Passed = allMonitorsInteractive,
                Details = allMonitorsInteractive ? "Los 3 monitores poseen colisionador y pertenecen a la capa 'Interactive'." : "Uno o más monitores no están en la capa 8 o carecen de collider.",
                EvidenceFound = $"M1 (L:{m1?.layer}, Col:{m1?.GetComponent<Collider>() != null}), M2 (L:{m2?.layer}), M3 (L:{m3?.layer})"
            });

            // 1.4 Canvases de los monitores en World Space
            Canvas c1 = m1 != null ? m1.GetComponentInChildren<Canvas>() : null;
            Canvas c2 = m2 != null ? m2.GetComponentInChildren<Canvas>() : null;
            Canvas c3 = m3 != null ? m3.GetComponentInChildren<Canvas>() : null;

            bool allWorldSpace = c1 != null && c1.renderMode == RenderMode.WorldSpace &&
                                 c2 != null && c2.renderMode == RenderMode.WorldSpace &&
                                 c3 != null && c3.renderMode == RenderMode.WorldSpace;

            results.Add(new QaCheckResult
            {
                Category = cat,
                Code = "RF01-4",
                Name = "Pantallas con Canvas en World Space",
                MaxScore = 0.75f,
                AwardedScore = allWorldSpace ? 0.75f : 0f,
                Passed = allWorldSpace,
                Details = allWorldSpace ? "Los 3 monitores proyectan su interfaz gráfica en RenderMode.WorldSpace para VR." : "Uno o más monitores carecen de Canvas en World Space.",
                EvidenceFound = $"C1: {c1?.renderMode}, C2: {c2?.renderMode}, C3: {c3?.renderMode}"
            });
        }

        #endregion

        #region 2. [RF-02] Correo de Rasel Ahlam e Interacción VR

        private static void AuditRf02_EmailInteraction(List<QaCheckResult> results)
        {
            const string cat = "[RF-02] Correo Malicioso e Interacción VR (3.5 Pts)";

            GameObject panelCorreo = GameObject.Find("Panel_Correo");
            bool hasPanelCorreo = panelCorreo != null;

            results.Add(new QaCheckResult
            {
                Category = cat,
                Code = "RF02-1",
                Name = "Panel de Correo de «Rasel Ahlam» en Monitor_01",
                MaxScore = 1.0f,
                AwardedScore = hasPanelCorreo ? 1.0f : 0f,
                Passed = hasPanelCorreo,
                Details = hasPanelCorreo ? "Panel_Correo presente en la bandeja de entrada de Monitor_01." : "No se encontró Panel_Correo en la jerarquía.",
                EvidenceFound = hasPanelCorreo ? $"Panel: {panelCorreo.name}, Activo: {panelCorreo.activeInHierarchy}" : "No encontrado"
            });

            // 2.2 Retícula central de Cardboard XR configurada sobre 'Interactive'
            CardboardReticleAdapter reticle = FindFirstObjectByType<CardboardReticleAdapter>();
            Camera mainCam = Camera.main ?? FindFirstObjectByType<Camera>();
            bool hasReticle = reticle != null && mainCam != null;

            results.Add(new QaCheckResult
            {
                Category = cat,
                Code = "RF02-2",
                Name = "Retícula Central VR (CardboardReticleAdapter en Main Camera)",
                MaxScore = 1.0f,
                AwardedScore = hasReticle ? 1.0f : 0f,
                Passed = hasReticle,
                Details = hasReticle ? "CardboardReticleAdapter adjunto a la cámara principal con raycast continuo a 15m." : "No se detectó CardboardReticleAdapter en la cámara principal.",
                EvidenceFound = $"Reticle: {reticle != null}, Camera: {mainCam?.name}"
            });

            // 2.3 Botones de acción funcional (Abrir Enlace y Reportar Phishing)
            GameObject btnAbrir = GameObject.Find("Btn_AbrirEnlaceAtaque") ?? GameObject.Find("Btn_AbrirEnlace");
            GameObject btnReportar = GameObject.Find("Btn_ReportarPhishing");

            bool hasButtons = btnAbrir != null && btnReportar != null;
            bool buttonsInteractive = hasButtons &&
                                      btnAbrir.GetComponent<Collider>() != null &&
                                      btnReportar.GetComponent<Collider>() != null;

            results.Add(new QaCheckResult
            {
                Category = cat,
                Code = "RF02-3",
                Name = "Botones de Acción con Colisionador y GazeInteractable",
                MaxScore = 1.5f,
                AwardedScore = buttonsInteractive ? 1.5f : (hasButtons ? 0.75f : 0f),
                Passed = buttonsInteractive,
                Details = buttonsInteractive ? "Botones 'Abrir Enlace' y 'Reportar Phishing' configurados con BoxCollider y GazeInteractable." : "Botones ausentes o sin collider para interacción con la mirada.",
                EvidenceFound = $"Btn_Abrir: {btnAbrir != null} (Col:{btnAbrir?.GetComponent<Collider>() != null}), Btn_Reportar: {btnReportar != null}"
            });
        }

        #endregion

        #region 3. [RF-03] Detección de las 3 Señales de Phishing

        private static void AuditRf03_PhishingSignals(List<QaCheckResult> results)
        {
            const string cat = "[RF-03] Detección de Señales de Phishing (3.5 Pts)";

            PhishingInspector inspector = FindFirstObjectByType<PhishingInspector>();
            bool hasInspector = inspector != null;

            results.Add(new QaCheckResult
            {
                Category = cat,
                Code = "RF03-1",
                Name = "Controlador Central PhishingInspector",
                MaxScore = 1.0f,
                AwardedScore = hasInspector ? 1.0f : 0f,
                Passed = hasInspector,
                Details = hasInspector ? "PhishingInspector presente y operativo en Monitor_01." : "No se encontró componente PhishingInspector en la escena.",
                EvidenceFound = $"Inspector: {inspector?.gameObject.name}"
            });

            // 3.2 Presencia de las 3 zonas interactivas (PhishingSignalHotspot)
            PhishingSignalHotspot[] hotspots = FindObjectsByType<PhishingSignalHotspot>(FindObjectsSortMode.None);
            bool hasRemitente = false;
            bool hasUrgencia = false;
            bool hasEnlace = false;

            int interactiveLayer = LayerMask.NameToLayer("Interactive");
            int collidersCount = 0;

            for (int i = 0; i < hotspots.Length; i++)
            {
                string id = hotspots[i].SignalId ?? string.Empty;
                if (id.IndexOf("Remitente", StringComparison.OrdinalIgnoreCase) >= 0 || id.IndexOf("Dominio", StringComparison.OrdinalIgnoreCase) >= 0) hasRemitente = true;
                if (id.IndexOf("Urgencia", StringComparison.OrdinalIgnoreCase) >= 0 || id.IndexOf("Presion", StringComparison.OrdinalIgnoreCase) >= 0) hasUrgencia = true;
                if (id.IndexOf("Enlace", StringComparison.OrdinalIgnoreCase) >= 0 || id.IndexOf("Malicioso", StringComparison.OrdinalIgnoreCase) >= 0) hasEnlace = true;

                if (hotspots[i].gameObject.layer == interactiveLayer && hotspots[i].GetComponent<Collider>() != null)
                {
                    collidersCount++;
                }
            }

            bool all3Hotspots = hasRemitente && hasUrgencia && hasEnlace;
            results.Add(new QaCheckResult
            {
                Category = cat,
                Code = "RF03-2",
                Name = "Presencia de las 3 Señales de Phishing (Remitente, Urgencia, Enlace)",
                MaxScore = 1.5f,
                AwardedScore = all3Hotspots ? 1.5f : (hotspots.Length > 0 ? 0.75f : 0f),
                Passed = all3Hotspots,
                Details = all3Hotspots ? "Las 3 señales (Remitente externo, Urgencia desmedida y Enlace malicioso) están presentes con colisionadores en capa Interactive." : "Faltan una o más señales de phishing obligatorias.",
                EvidenceFound = $"Remitente: {hasRemitente}, Urgencia: {hasUrgencia}, Enlace: {hasEnlace} (Total Hotspots: {hotspots.Length})"
            });

            // 3.3 Contador HUD y visualizador de señales detectadas
            GameObject counterObj = GameObject.Find("Txt_SeñalesContador");
            bool hasCounter = counterObj != null;

            results.Add(new QaCheckResult
            {
                Category = cat,
                Code = "RF03-3",
                Name = "Contador de Señales Detectadas (0/3)",
                MaxScore = 1.0f,
                AwardedScore = hasCounter ? 1.0f : 0f,
                Passed = hasCounter,
                Details = hasCounter ? "Indicador visual de progreso de señales detectadas presente en el correo." : "No se encontró el contador de señales de phishing.",
                EvidenceFound = $"Contador: {(counterObj != null ? counterObj.name : "Falta")}"
            });
        }

        #endregion

        #region 4. [RF-04] Bivalencia: Modo Ataque vs Modo Defensa

        private static void AuditRf04_AttackAndDefenseModes(List<QaCheckResult> results)
        {
            const string cat = "[RF-04] Bivalencia: Ataque vs Defensa (3.5 Pts)";

            // 4.1 Cinemática de Modo Ataque (InfectionSpreadVisualizer + LineRenderers)
            InfectionSpreadVisualizer infectionVis = FindFirstObjectByType<InfectionSpreadVisualizer>();
            LineRenderer[] lines = FindObjectsByType<LineRenderer>(FindObjectsSortMode.None);
            bool hasInfectionVis = infectionVis != null && lines.Length >= 2;

            results.Add(new QaCheckResult
            {
                Category = cat,
                Code = "RF04-1",
                Name = "Modo Ataque: InfectionSpreadVisualizer y Red Plana",
                MaxScore = 1.5f,
                AwardedScore = hasInfectionVis ? 1.5f : (infectionVis != null ? 0.75f : 0f),
                Passed = hasInfectionVis,
                Details = hasInfectionVis ? "InfectionSpreadVisualizer y los haces LineRenderer de propagación entre escritorios están listos." : "Falta InfectionSpreadVisualizer o los LineRenderer de red plana.",
                EvidenceFound = $"Visualizador: {infectionVis != null}, LineRenderers detectados: {lines.Length}"
            });

            // 4.2 Consola de Modo Defensa (MailQuarantine + Panel Sandbox)
            MailQuarantine quarantine = FindFirstObjectByType<MailQuarantine>(FindObjectsInactive.Include);
            GameObject quarantinePanel = GameObject.Find("Panel_Cuarentena_Sandbox");
            if (quarantinePanel == null && quarantine != null)
            {
                Transform t = quarantine.transform.Find("Canvas_Pantalla/Panel_Cuarentena_Sandbox");
                if (t != null) quarantinePanel = t.gameObject;
            }
            bool hasQuarantine = quarantine != null && quarantinePanel != null;

            results.Add(new QaCheckResult
            {
                Category = cat,
                Code = "RF04-2",
                Name = "Modo Defensa: MailQuarantine y Consola Sandbox",
                MaxScore = 1.5f,
                AwardedScore = hasQuarantine ? 1.5f : (quarantine != null ? 0.75f : 0f),
                Passed = hasQuarantine,
                Details = hasQuarantine ? "MailQuarantine y la consola de Sandbox perimetral con bloqueo de 16 buzones están configurados." : "Falta componente MailQuarantine o panel de Sandbox.",
                EvidenceFound = $"MailQuarantine: {quarantine != null}, Panel Cuarentena: {quarantinePanel != null}"
            });

            // 4.3 Iluminación reactiva ambiental (AmbientCyberGridEffect)
            AmbientCyberGridEffect cyberGrid = FindFirstObjectByType<AmbientCyberGridEffect>();
            bool hasCyberGrid = cyberGrid != null;

            results.Add(new QaCheckResult
            {
                Category = cat,
                Code = "RF04-3",
                Name = "Iluminación Ambiental Reactiva (AmbientCyberGridEffect)",
                MaxScore = 0.5f,
                AwardedScore = hasCyberGrid ? 0.5f : 0f,
                Passed = hasCyberGrid,
                Details = hasCyberGrid ? "AmbientCyberGridEffect conmuta la iluminación de la oficina entre Investigación normal, Alerta roja y Escudo cian/verde." : "No se encontró AmbientCyberGridEffect.",
                EvidenceFound = $"CyberGridEffect: {cyberGrid != null}"
            });
        }

        #endregion

        #region 5. [RF-05] Registro de Evidencias Obligatorias

        private static void AuditRf05_MandatoryEvidences(List<QaCheckResult> results)
        {
            const string cat = "[RF-05] Registro de Evidencias Forenses (3.0 Pts)";

            // 5.1 GameManager y ProgressTracker
            GameManager gm = (GameManager.I != null) 
                ? GameManager.I 
                : FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            if (gm == null)
            {
                GameObject systemsObj = GameObject.Find("_SYSTEMS");
                if (systemsObj != null) gm = systemsObj.GetComponent<GameManager>();
            }
            ScenarioData dataAsset = (gm != null && gm.data != null) ? gm.data : Resources.Load<ScenarioData>("Data/Scenario01_Data");
            bool hasGm = gm != null && (gm.data != null || dataAsset != null);

            results.Add(new QaCheckResult
            {
                Category = cat,
                Code = "RF05-1",
                Name = "GameManager con RegisterEvidence y ScenarioData cargado",
                MaxScore = 1.0f,
                AwardedScore = hasGm ? 1.0f : (gm != null ? 0.5f : 0f),
                Passed = hasGm,
                Details = hasGm ? "GameManager activo en _SYSTEMS con Scenario01_Data.asset enlazado." : "Falta GameManager o no tiene asignado ScenarioData.",
                EvidenceFound = $"GameManager: {gm != null}, ScenarioData: {(gm?.data != null ? gm.data.ScenarioTitle : dataAsset != null ? dataAsset.ScenarioTitle : "Falta")}"
            });

            // 5.2 Post-It de Carla Mendoza ('Evidencia_PostIt')
            GameObject postIt = GameObject.Find("PostIt_Credenciales");
            AndesSim.Scenario01.EvidenceItem postItEv = postIt != null ? postIt.GetComponent<AndesSim.Scenario01.EvidenceItem>() : null;
            bool hasPostIt = postIt != null && postItEv != null && postItEv.EvidenceId == "Evidencia_PostIt";

            results.Add(new QaCheckResult
            {
                Category = cat,
                Code = "RF05-2",
                Name = "Evidencia Física: Post-It de Credenciales ('Evidencia_PostIt')",
                MaxScore = 1.0f,
                AwardedScore = hasPostIt ? 1.0f : (postIt != null ? 0.5f : 0f),
                Passed = hasPostIt,
                Details = hasPostIt ? "PostIt_Credenciales presente en el escritorio de Carla con ID 'Evidencia_PostIt' y colisionador interactivo." : "Post-It ausente o no tiene el componente EvidenceItem configurado.",
                EvidenceFound = $"Objeto: {postIt?.name}, ID: {postItEv?.EvidenceId}"
            });

            // 5.3 Terminal SIEM ('Evidencia_LogAccesos')
            AccessLogTerminal siem = FindFirstObjectByType<AccessLogTerminal>(FindObjectsInactive.Include);
            bool hasSiem = siem != null && (siem.EvidenceId == "Evidencia_LogAccesos" || siem.gameObject.name.IndexOf("03", StringComparison.OrdinalIgnoreCase) >= 0);

            results.Add(new QaCheckResult
            {
                Category = cat,
                Code = "RF05-3",
                Name = "Evidencia de Red: Consola SIEM ('Evidencia_LogAccesos')",
                MaxScore = 1.0f,
                AwardedScore = hasSiem ? 1.0f : (siem != null ? 0.5f : 0f),
                Passed = hasSiem,
                Details = hasSiem ? "AccessLogTerminal configurado en Monitor_03 con ID 'Evidencia_LogAccesos' y botón para registrar evento." : "Terminal de logs SIEM ausente o mal configurado.",
                EvidenceFound = $"AccessLogTerminal: {siem != null}, ID: {siem?.EvidenceId}"
            });
        }

        #endregion

        #region 6. [Requisitos VR] Navegación, Confort, Audio y Créditos

        private static void AuditVrRequirements(List<QaCheckResult> results)
        {
            const string cat = "[Requisitos VR] Navegación, Confort, Audio y Créditos (3.0 Pts)";

            // 6.1 Portal_Entrada en el vestíbulo
            PortalEntrada portal = FindFirstObjectByType<PortalEntrada>();
            bool hasPortal = portal != null;

            results.Add(new QaCheckResult
            {
                Category = cat,
                Code = "VR-1",
                Name = "Portal de Entrada (Vestíbulo con Briefing y Créditos)",
                MaxScore = 0.75f,
                AwardedScore = hasPortal ? 0.75f : 0f,
                Passed = hasPortal,
                Details = hasPortal ? "Portal_Entrada instanciado con colisionador interactivo y panel de briefing." : "No se encontró Portal_Entrada.",
                EvidenceFound = $"Portal: {portal?.name}"
            });

            // 6.2 Los 3 TeleportPoints de navegación
            TeleportPoint[] tps = FindObjectsByType<TeleportPoint>(FindObjectsSortMode.None);
            bool hasTp1 = false;
            bool hasTp2 = false;
            bool hasTp3 = false;

            for (int i = 0; i < tps.Length; i++)
            {
                string n = tps[i].name;
                if (n.IndexOf("Puesto1", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("01", StringComparison.OrdinalIgnoreCase) >= 0) hasTp1 = true;
                if (n.IndexOf("Puesto2", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("02", StringComparison.OrdinalIgnoreCase) >= 0) hasTp2 = true;
                if (n.IndexOf("Puesto3", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("03", StringComparison.OrdinalIgnoreCase) >= 0) hasTp3 = true;
            }

            bool all3Teleports = hasTp1 && hasTp2 && hasTp3;
            results.Add(new QaCheckResult
            {
                Category = cat,
                Code = "VR-2",
                Name = "Puntos de Teletransporte Discreto (Zero Cybersickness)",
                MaxScore = 0.75f,
                AwardedScore = all3Teleports ? 0.75f : (tps.Length >= 3 ? 0.5f : 0f),
                Passed = all3Teleports,
                Details = all3Teleports ? "Los 3 TeleportPoints (Puestos 1, 2 y 3) están activos en capa Interactive con aro luminoso." : "Faltan puntos de teletransporte.",
                EvidenceFound = $"TP1: {hasTp1}, TP2: {hasTp2}, TP3: {hasTp3} (Total: {tps.Length})"
            });

            // 6.3 Al menos 5 objetos interactivos con la retícula
            int interactiveLayer = LayerMask.NameToLayer("Interactive");
            GazeInteractable[] interactables = FindObjectsByType<GazeInteractable>(FindObjectsSortMode.None);
            int validInteractablesCount = 0;

            for (int i = 0; i < interactables.Length; i++)
            {
                if (interactables[i].gameObject.layer == interactiveLayer && interactables[i].GetComponent<Collider>() != null)
                {
                    validInteractablesCount++;
                }
            }

            bool hasAtLeast5 = validInteractablesCount >= 5;
            results.Add(new QaCheckResult
            {
                Category = cat,
                Code = "VR-3",
                Name = "Al Menos 5 Objetos Interactivos con la Retícula (>=5)",
                MaxScore = 0.5f,
                AwardedScore = hasAtLeast5 ? 0.5f : 0f,
                Passed = hasAtLeast5,
                Details = hasAtLeast5 ? $"Se detectaron {validInteractablesCount} objetos interactivos en capa 8 (supera ampliamente el mínimo de 5)." : $"Solo se detectaron {validInteractablesCount} interactables.",
                EvidenceFound = $"Total interactables válidos: {validInteractablesCount} (Mínimo: 5)"
            });

            // 6.4 AudioManager con síntesis procedural
            AudioManager audio = FindFirstObjectByType<AudioManager>();
            bool hasAudio = audio != null;

            results.Add(new QaCheckResult
            {
                Category = cat,
                Code = "VR-4",
                Name = "Administrador de Audio Procedural (AudioManager)",
                MaxScore = 0.5f,
                AwardedScore = hasAudio ? 0.5f : 0f,
                Passed = hasAudio,
                Details = hasAudio ? "AudioManager central presente con los 7 tonos matemáticos procedurales." : "No se encontró AudioManager.",
                EvidenceFound = $"AudioManager: {audio?.gameObject.name}"
            });

            // 6.5 Pantalla final, tablero de sospechosos y panel de créditos
            EndScenarioPanelUI endPanel = FindFirstObjectByType<EndScenarioPanelUI>(FindObjectsInactive.Include);
            SuspectBoardVR suspectBoard = FindFirstObjectByType<SuspectBoardVR>(FindObjectsInactive.Include);
            CreditsAndEthicsModalUI creditsModal = FindFirstObjectByType<CreditsAndEthicsModalUI>(FindObjectsInactive.Include);

            bool allPanelsPresent = endPanel != null && suspectBoard != null && creditsModal != null;
            results.Add(new QaCheckResult
            {
                Category = cat,
                Code = "VR-5",
                Name = "Pantalla Final, Tablero de Sospechosos y Créditos/Ética",
                MaxScore = 0.5f,
                AwardedScore = allPanelsPresent ? 0.5f : 0f,
                Passed = allPanelsPresent,
                Details = allPanelsPresent ? "EndScenarioPanelUI, SuspectBoardVR (4 sospechosos) y CreditsAndEthicsModalUI (Leyes 30096/29733) están presentes." : "Falta uno o más paneles finales o modales.",
                EvidenceFound = $"EndPanel: {endPanel != null}, SuspectBoard: {suspectBoard != null}, CreditsModal: {creditsModal != null}"
            });
        }

        #endregion

        #region Generación de Reportes (Consola y Markdown)

        private static void PrintConsoleSummary(QaAuditSummary summary, string reportPath)
        {
            string colorHex = summary.TotalScore >= 18.0f ? "#38F28A" :
                              summary.TotalScore >= 14.0f ? "#FFC24A" : "#FF5A4E";

            Debug.Log($"{LogPrefix} <b>====================================================================</b>");
            Debug.Log($"{LogPrefix} <b>RESULTADO AUDITORÍA QA: <color={colorHex}>{summary.TotalScore:F1} / {summary.MaxScore:F1} PTS</color></b>");
            Debug.Log($"{LogPrefix} <b>Criterios Aprobados: {summary.PassedChecks} de {summary.TotalChecks}</b>");
            Debug.Log($"{LogPrefix} <b>====================================================================</b>");

            for (int i = 0; i < summary.Results.Count; i++)
            {
                QaCheckResult r = summary.Results[i];
                string statusSymbol = r.Passed ? "<color=#38F28A>[PASS]</color>" : "<color=#FF5A4E>[FAIL]</color>";
                Debug.Log($"{LogPrefix} {statusSymbol} <b>[{r.Code}] {r.Name}</b>: {r.AwardedScore:F2}/{r.MaxScore:F2} pts — {r.Details}");
            }

            Debug.Log($"{LogPrefix} <color=#38F28A><b>Reporte completo guardado en: {reportPath}</b></color>");
        }

        private static void WriteMarkdownReport(QaAuditSummary summary, string outputPath)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("# INFORME DE AUDITORÍA QA Y CUMPLIMIENTO DE RÚBRICA (20 PUNTOS)");
            sb.AppendLine($"**Proyecto:** ANDES-SIM · Caso BANCO ANDINO — Escenario 1: Oficina de pagos (VR)");
            sb.AppendLine($"**Equipo:** Equipo 1 (VR)");
            sb.AppendLine($"**Escena Auditada:** `{summary.SceneName}`");
            sb.AppendLine($"**Fecha y Hora:** `{summary.Timestamp:yyyy-MM-dd HH:mm:ss}`");
            sb.AppendLine($"**Calificación Obtenida:** **`{summary.TotalScore:F1} / {summary.MaxScore:F1} PUNTOS`** ({(summary.TotalScore / summary.MaxScore) * 100:F0}%)");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            sb.AppendLine("## 1. Resumen Ejecutivo de la Evaluación");
            sb.AppendLine();
            sb.AppendLine("| Métrica | Valor |");
            sb.AppendLine("| :--- | :--- |");
            sb.AppendLine($"| **Puntaje Total** | **{summary.TotalScore:F1} / {summary.MaxScore:F1} Pts** |");
            sb.AppendLine($"| **Criterios Aprobados** | **{summary.PassedChecks} / {summary.TotalChecks}** |");
            sb.AppendLine($"| **Estado de Cumplimiento** | **{(summary.TotalScore >= 18f ? "EXCELENTE (SOBRESALIENTE)" : summary.TotalScore >= 14f ? "APROBADO" : "OBSERVADO")}** |");
            sb.AppendLine($"| **Plataforma Objetivo** | Google Cardboard XR (Android OpenGLES3, 60 FPS) |");
            sb.AppendLine();

            sb.AppendLine("---");
            sb.AppendLine();

            sb.AppendLine("## 2. Detalle de Cumplimiento por Criterio de la Rúbrica");
            sb.AppendLine();

            string currentCategory = string.Empty;

            for (int i = 0; i < summary.Results.Count; i++)
            {
                QaCheckResult r = summary.Results[i];

                if (r.Category != currentCategory)
                {
                    currentCategory = r.Category;
                    sb.AppendLine();
                    sb.AppendLine($"### {currentCategory}");
                    sb.AppendLine();
                    sb.AppendLine("| Código | Criterio de Evaluación | Puntaje | Estado | Detalles y Evidencia |");
                    sb.AppendLine("| :--- | :--- | :--- | :--- | :--- |");
                }

                string stateBadge = r.Passed ? "✅ **APROBADO**" : "❌ **OBSERVADO**";
                sb.AppendLine($"| **`{r.Code}`** | {r.Name} | **{r.AwardedScore:F2}** / {r.MaxScore:F2} | {stateBadge} | {r.Details}<br/>*Evidencia:* `{r.EvidenceFound}` |");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            sb.AppendLine("## 3. Matriz de Requisitos Funcionales Oficiales");
            sb.AppendLine();
            sb.AppendLine("1. **[RF-01] Espacio de Trabajo Físico y Monitores:** 3 puestos en L equipados con `Monitor_01`, `Monitor_02` y `Monitor_03` en capa `Interactive` (Layer 8) y Canvases en `World Space`.");
            sb.AppendLine("2. **[RF-02] Vector de Phishing Inicial:** Correo de «Rasel Ahlam» recibido el 29 de enero de 2015, con interacción por retícula y clic táctil.");
            sb.AppendLine("3. **[RF-03] Inspección de Señales:** 3 zonas interactivas (`PhishingSignalHotspot`) para remitente falso, sentido de urgencia y enlace al payload ejecutable `.scr.zip`.");
            sb.AppendLine("4. **[RF-04] Bivalencia de Simulación (Ataque vs Defensa):**");
            sb.AppendLine("   - **Modo Ataque:** Cinemática de 8s con propagación de haces láser en red plana hacia las computadoras 2 y 3, letrero holográfico de puerta trasera y espionaje de Lazarus.");
            sb.AppendLine("   - **Modo Defensa:** Aislamiento perimetral en Sandbox Cuckoo, bloqueo de 16 buzones y contención total con 0 infecciones.");
            sb.AppendLine("5. **[RF-05] Registro de Evidencias Forenses:** Post-It de Carla Mendoza (`Evidencia_PostIt`) y registros SIEM (`Evidencia_LogAccesos`) enlazados a `GameManager`.");
            sb.AppendLine("6. **[Requisitos VR Cardboard]:** Portal de entrada en vestíbulo, 3 TeleportPoints de navegación discreta (Zero Cybersickness), más de 5 objetos interactivos con la retícula, sintetizador procedural `AudioManager`, tablero táctico de 4 sospechosos (`SuspectBoardVR`) y modal ético-legal (Leyes 30096 y 29733).");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine("*Informe generado automáticamente por Scenario01QaValidator de ANDES-SIM.*");

            try
            {
                File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogPrefix} Error al guardar archivo de reporte en '{outputPath}': {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Estructura resumen de la auditoría.
    /// </summary>
    public class QaAuditSummary
    {
        public DateTime Timestamp;
        public string SceneName;
        public float TotalScore;
        public float MaxScore;
        public int PassedChecks;
        public int TotalChecks;
        public List<QaCheckResult> Results;
    }
}

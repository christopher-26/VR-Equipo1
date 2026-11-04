using System;
using UnityEngine;

namespace AndesSim.Data
{
    /// <summary>
    /// Modelo de datos oficial para el Escenario 1: "Oficina de pagos" (Equipo 1) - BANCO ANDINO.
    /// Almacena toda la configuración narrativa, técnica, indicadores de compromiso (IoC) y defensas de la simulación VR.
    /// </summary>
    [CreateAssetMenu(fileName = "Scenario01_Data", menuName = "ANDES-SIM/ScenarioData", order = 10)]
    public class ScenarioData : ScriptableObject
    {
        [Header("--- Metadatos del Escenario ---")]
        [Tooltip("Número identificador del escenario.")]
        [SerializeField] private int scenarioNumber = 1;

        [Tooltip("Título oficial del escenario.")]
        [SerializeField] private string scenarioTitle = "Oficina de pagos";

        [Tooltip("Tipo de tecnología de inmersión.")]
        [SerializeField] private string scenarioTechType = "VR";

        [Tooltip("Rango de fechas del incidente histórico simulado.")]
        [SerializeField] private string scenarioDate = "29 ene 2015 · enero 2016";

        [Tooltip("Cantidad de destinatarios internos alcanzados por la campaña de phishing.")]
        [SerializeField] private int emailRecipientsCount = 16;

        [Tooltip("Cantidad de terminales donde se intentó o concretó la infección inicial.")]
        [SerializeField] private int infectedAttemptComputers = 3;

        [Tooltip("Tiempo límite en segundos para completar la misión (3 minutos según Criterio de Aceptación).")]
        [SerializeField] private float timeLimitSeconds = 180f;

        [Header("--- Datos del Correo de Phishing (Vector Inicial) ---")]
        [Tooltip("Nombre ficticio del postulante que suplanta la identidad.")]
        [SerializeField] private string senderDisplayName = "Rasel Ahlam";

        [Tooltip("Dirección de correo electrónico del atacante.")]
        [SerializeField] private string senderEmail = "rasel.ahlam@career-applicants-bd.com";

        [Tooltip("Destinatarios visibles y copia masiva.")]
        [SerializeField] private string emailRecipient = "Carla Mendoza <c.mendoza@bancoandino.pe> (+15 destinatarios)";

        [Tooltip("Asunto formal del correo malicioso.")]
        [SerializeField] private string emailSubject = "Postulación a vacante de Operaciones Bancarias - Adjunto CV";

        [Tooltip("Cuerpo del correo persuasivo con técnica de ingeniería social.")]
        [TextArea(6, 12)]
        [SerializeField] private string emailBody = 
            "Estimado equipo de Operaciones Bancarias,\n\n" +
            "Mi nombre es Rasel Ahlam y me pongo en contacto con el mayor respeto para presentar mi postulación formal a la vacante en el área de Operaciones del Banco Andino.\n\n" +
            "Cuento con más de 7 años de experiencia en conciliaciones y sistemas transaccionales SWIFT. Debido al cierre inminente de la convocatoria el día de hoy, les ruego revisar urgentemente mi currículum vitae y certificaciones bancarias alojadas en el siguiente enlace de descarga directa:\n\n" +
            "http://bit.ly-cv-storage.org/Rasel_Ahlam_CV_2015.scr.zip\n\n" +
            "Agradezco de antemano su atención inmediata y quedo a su total disposición para coordinar una entrevista técnica.\n\n" +
            "Atentamente,\n" +
            "Rasel Ahlam\n" +
            "Especialista en Operaciones Financieras";

        [Tooltip("URL maliciosa que apunta al dropper ejecutable.")]
        [SerializeField] private string maliciousUrl = "http://bit.ly-cv-storage.org/Rasel_Ahlam_CV_2015.scr.zip";

        [Header("--- Señales de Phishing (RF-03: Obligatorias) ---")]
        [Tooltip("Tres señales críticas que el usuario debe identificar: Remitente, Enlace y Urgencia.")]
        [SerializeField] private PhishingSignal[] phishingSignals = new PhishingSignal[]
        {
            new PhishingSignal
            {
                signalId = "Signal_Remitente",
                signalType = PhishingSignalType.Sender,
                title = "Remitente sospechoso y dominio no institucional",
                technicalExplanation = "El correo proviene de 'career-applicants-bd.com', un dominio público no verificado por los registros SPF/DKIM corporativos de Banco Andino. El remitente usa un nombre falso para simular una postulación laboral legítima."
            },
            new PhishingSignal
            {
                signalId = "Signal_Enlace",
                signalType = PhishingSignalType.Link,
                title = "Enlace externo con extensión ejecutable (.scr.zip)",
                technicalExplanation = "La URL apunta a un servidor no autorizado ('bit.ly-cv-storage.org') y descarga un archivo con doble extensión oculta (.scr.zip). Los archivos .scr son screensavers de Windows ejecutables, utilizados como dropper para instalar malware."
            },
            new PhishingSignal
            {
                signalId = "Signal_Urgencia",
                signalType = PhishingSignalType.Urgency,
                title = "Sentido artificial de urgencia y presión temporal",
                technicalExplanation = "El mensaje enfatiza un 'cierre inminente' y solicita una descarga 'urgente e inmediata'. Esta técnica de ingeniería social busca anular el pensamiento crítico de la víctima forzándola a omitir los controles de seguridad."
            }
        };

        [Header("--- Evidencias Físicas y Digitales (RF-05: Obligatorias) ---")]
        [Tooltip("Las 3 evidencias clave requeridas en el escenario de la oficina.")]
        [SerializeField] private EvidenceItem[] scenarioEvidences = new EvidenceItem[]
        {
            new EvidenceItem
            {
                evidenceId = "Evidencia_Correo",
                evidenceName = "Correo Malicioso en Bandeja de Entrada",
                locationDescription = "Terminal de Carla Mendoza (Monitor principal)",
                technicalDescription = "Correo masivo enviado a 16 empleados de operaciones que sirvió como vector inicial de compromiso (Initial Access) de la red interna."
            },
            new EvidenceItem
            {
                evidenceId = "Evidencia_PostIt",
                evidenceName = "Credenciales Expuestas en Post-it Físico",
                locationDescription = "Adherido bajo el teclado del puesto de operaciones",
                technicalDescription = "Contraseña de administrador local y credenciales del core bancario escritas en papel, evidenciando negligencia de seguridad física y facilitando el movimiento lateral."
            },
            new EvidenceItem
            {
                evidenceId = "Evidencia_LogAccesos",
                evidenceName = "Registro de Conexiones Anómalas (SIEM)",
                locationDescription = "Terminal de supervisión / Pantalla de monitoreo",
                technicalDescription = "Logs que revelan conexiones entrantes fuera de horario y ejecución de comandos PowerShell sospechosos en 3 terminales de la oficina de pagos."
            }
        };

        [Header("--- Contramedidas y Defensas (Obligatorias) ---")]
        [Tooltip("Las 3 defensas requeridas para mitigar el ataque.")]
        [SerializeField] private DefenseItem[] scenarioDefenses = new DefenseItem[]
        {
            new DefenseItem
            {
                defenseId = "Defensa_Sandbox",
                defenseName = "Filtro de correo con análisis en Sandbox",
                mechanismSummary = "Inspección automatizada en pasarela (Secure Email Gateway). Los enlaces y adjuntos se ejecutan en un entorno virtual aislado antes de llegar a los buzones.",
                mitigationDetail = "Detona el archivo '.scr.zip' detectando comportamiento anómalo y bloquea la entrega del correo malicioso a los 16 destinatarios."
            },
            new DefenseItem
            {
                defenseId = "Defensa_BotonReportar",
                defenseName = "Capacitación y botón 'Reportar Phishing'",
                mechanismSummary = "Cultura de ciberseguridad continua en el personal y un botón en el cliente de correo para alertar inmediatamente al Centro de Operaciones de Seguridad (SOC).",
                mitigationDetail = "Alerta al SOC tras la primera detección, permitiendo purgar el correo de los demás 15 buzones antes de que otro colaborador haga clic."
            },
            new DefenseItem
            {
                defenseId = "Defensa_MFA",
                defenseName = "Gestor de contraseñas y doble factor (MFA)",
                mechanismSummary = "Implementación obligatoria de autenticación robusta mediante tokens criptográficos (FIDO2/TOTP) y bóveda de claves encriptada.",
                mitigationDetail = "Invalida el uso de contraseñas expuestas en post-its, bloqueando el acceso o movimiento lateral de los atacantes sin el segundo factor físico."
            }
        };

        [Header("--- Resúmenes Finales de Simulación ---")]
        [TextArea(4, 8)]
        [SerializeField] private string attackModeSummary = 
            "ENFOQUE OFENSIVO (Modo Ataque):\n" +
            "El ataque a la oficina de pagos del Banco Andino aprovechó la ingeniería social mediante un correo de phishing masivo enviado a 16 colaboradores. " +
            "La ausencia de filtrado avanzado de adjuntos (.scr.zip) y la presión de urgencia indujeron la infección en 3 terminales clave. " +
            "Posteriormente, las contraseñas físicas expuestas en notas adhesivas permitieron a los atacantes consolidar acceso privilegiado y ejecutar movimientos laterales en menos de 3 minutos.";

        [TextArea(4, 8)]
        [SerializeField] private string defenseModeSummary = 
            "ENFOQUE DEFENSIVO (Modo Defensa):\n" +
            "La implementación de una estrategia de seguridad en profundidad mitigó el vector de ataque por completo: " +
            "1) El Sandbox de correo interceptó el archivo ejecutable malicioso antes de su entrega; " +
            "2) El reporte oportuno de los usuarios mediante el botón de phishing permitió al SOC aislar los buzones afectados; " +
            "3) La autenticación de doble factor (MFA) neutralizó cualquier intento de intrusión derivado de credenciales comprometidas físicamente.";

        #region Getters Públicos (Clean Architecture)

        public int ScenarioNumber => scenarioNumber;
        public string ScenarioTitle => scenarioTitle;
        public string ScenarioTechType => scenarioTechType;
        public string ScenarioDate => scenarioDate;
        public int EmailRecipientsCount => emailRecipientsCount;
        public int InfectedAttemptComputers => infectedAttemptComputers;
        public float TimeLimitSeconds => timeLimitSeconds;

        public string SenderDisplayName => senderDisplayName;
        public string SenderEmail => senderEmail;
        public string EmailRecipient => emailRecipient;
        public string EmailSubject => emailSubject;
        public string EmailBody => emailBody;
        public string MaliciousUrl => maliciousUrl;

        public PhishingSignal[] PhishingSignals => phishingSignals;
        public EvidenceItem[] ScenarioEvidences => scenarioEvidences;
        public DefenseItem[] ScenarioDefenses => scenarioDefenses;

        public string AttackModeSummary => attackModeSummary;
        public string DefenseModeSummary => defenseModeSummary;

        #endregion

        #region Métodos de Consulta y Utilidad

        /// <summary>
        /// Obtiene una evidencia por su identificador único (e.g., 'Evidencia_Correo').
        /// </summary>
        public EvidenceItem GetEvidenceById(string id)
        {
            if (scenarioEvidences == null) return default;
            for (int i = 0; i < scenarioEvidences.Length; i++)
            {
                if (scenarioEvidences[i].evidenceId.Equals(id, StringComparison.OrdinalIgnoreCase))
                {
                    return scenarioEvidences[i];
                }
            }
            return default;
        }

        /// <summary>
        /// Obtiene una defensa por su identificador único (e.g., 'Defensa_Sandbox').
        /// </summary>
        public DefenseItem GetDefenseById(string id)
        {
            if (scenarioDefenses == null) return default;
            for (int i = 0; i < scenarioDefenses.Length; i++)
            {
                if (scenarioDefenses[i].defenseId.Equals(id, StringComparison.OrdinalIgnoreCase))
                {
                    return scenarioDefenses[i];
                }
            }
            return default;
        }

        #endregion
    }

    #region Estructuras Auxiliares Serializables

    public enum PhishingSignalType
    {
        Sender,
        Link,
        Urgency
    }

    [Serializable]
    public struct PhishingSignal
    {
        public string signalId;
        public PhishingSignalType signalType;
        public string title;
        [TextArea(2, 5)] public string technicalExplanation;
    }

    [Serializable]
    public struct EvidenceItem
    {
        public string evidenceId;
        public string evidenceName;
        public string locationDescription;
        [TextArea(2, 5)] public string technicalDescription;
    }

    [Serializable]
    public struct DefenseItem
    {
        public string defenseId;
        public string defenseName;
        [TextArea(2, 5)] public string mechanismSummary;
        [TextArea(2, 5)] public string mitigationDetail;
    }

    #endregion
}

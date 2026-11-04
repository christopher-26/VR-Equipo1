using System;
using System.Collections.Generic;
using UnityEngine;

namespace AndesSim.Data
{
    /// <summary>
    /// Base de datos y configuración de los 3 puestos de trabajo de la "Oficina de pagos" (RF-01).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1.
    /// Define la información interactiva de pantallas, credenciales expuestas y logs de seguridad.
    /// </summary>
    [CreateAssetMenu(fileName = "Workstations_OficinaPagos", menuName = "ANDES-SIM/WorkstationData", order = 12)]
    public class WorkstationData : ScriptableObject
    {
        [Header("--- Puestos de Trabajo de la Oficina (RF-01) ---")]
        [Tooltip("Listado oficial de los puestos interactivos en el entorno 3D.")]
        [SerializeField] private List<WorkstationConfig> workstations = new List<WorkstationConfig>();

        public IReadOnlyList<WorkstationConfig> Workstations => workstations;

        private void Reset()
        {
            PopulateDefaultData();
        }

        /// <summary>
        /// Obtiene la configuración de una estación por su ID (ej. "Monitor_01", "Monitor_02", "Monitor_03").
        /// </summary>
        public WorkstationConfig GetWorkstationById(string workstationId)
        {
            if (string.IsNullOrEmpty(workstationId) || workstations == null) return null;

            for (int i = 0; i < workstations.Count; i++)
            {
                if (workstations[i] != null &&
                    string.Equals(workstations[i].workstationId, workstationId, StringComparison.OrdinalIgnoreCase))
                {
                    return workstations[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Inicializa las 3 estaciones oficiales del Banco Andino con los datos exactos del RF-01 y RF-05.
        /// </summary>
        public void PopulateDefaultData()
        {
            workstations.Clear();

            // =========================================================================
            // MONITOR 01: Puesto de Carla Mendoza (Operadora SWIFT)
            // =========================================================================
            WorkstationConfig ws1 = new WorkstationConfig
            {
                workstationId = "Monitor_01",
                workstationName = "Puesto de Carla Mendoza (Operaciones SWIFT)",
                operatorName = "Carla Mendoza",
                roleTitle = "Operadora Titular SWIFT",
                hostname = "PC-PAGOS-01",
                ipAddress = "192.168.10.21",
                hasInteractiveScreen = true,
                hasPostItNote = true,
                postItCredentials = new PostItCredentialData
                {
                    noteTag = "PostIt_Credenciales",
                    accountUser = "cmendoza_swift",
                    plainPassword = "Andino2015*Swift",
                    displayText = "USUARIO: cmendoza_swift\nCLAVE: Andino2015*Swift\n[Terminal SWIFT #1 - Autorización de Pagos]",
                    securityObservation = "Vulnerabilidad grave: credenciales en texto plano expuestas físicamente bajo el teclado del monitor."
                },
                attackModeScreen = new ScreenContent
                {
                    applicationTitle = "WebMail Banco Andino - Bandeja de Entrada",
                    screenStatus = "CORREO ABIERTO - ENLACE EXTERNO SIN FILTRADO",
                    statusColor = Color.red,
                    headerText = "De: Rasel Ahlam <rasel.ahlam@career-applicants-bd.com>\nPara: Carla Mendoza <c.mendoza@bancoandino.pe> (+15 destinatarios)\nAsunto: Postulación a vacante de Operaciones Bancarias - Adjunto CV",
                    bodyText = "Estimado equipo de Operaciones Bancarias,\n\n" +
                               "Mi nombre es Rasel Ahlam y presento mi postulación formal para la vacante en Operaciones del Banco Andino.\n\n" +
                               "Debido al cierre inminente de la vacante hoy, les ruego descargar y revisar urgentemente mi CV en el siguiente enlace:\n\n" +
                               "http://bit.ly-cv-storage.org/Rasel_Ahlam_CV_2015.scr.zip\n\n" +
                               "Quedo a su disposición para coordinar una entrevista técnica.\n\n" +
                               "Atentamente,\nRasel Ahlam",
                    footerNotice = "ADVERTENCIA: Archivo .scr.zip descargado en equipo local. Se inició proceso en segundo plano."
                },
                defenseModeScreen = new ScreenContent
                {
                    applicationTitle = "WebMail Banco Andino - Protección Perimetral Activa",
                    screenStatus = "CORREO REPORTADO POR USUARIO - BLOQUEADO POR SOC",
                    statusColor = Color.green,
                    headerText = "De: [REMITENTE SOSPECHOSO] Rasel Ahlam\nPara: c.mendoza@bancoandino.pe\nAsunto: [PHISHING REPORTADO] Postulación a vacante",
                    bodyText = "AVISO DE SEGURIDAD INFORMÁTICA (CIRT - BANCO ANDINO):\n\n" +
                               "El usuario utilizó el botón corporativo 'Reportar Phishing'.\n" +
                               "• El enlace externo a 'bit.ly-cv-storage.org' fue bloqueado en el firewall corporativo.\n" +
                               "• El dropper .scr.zip fue neutralizado en sandbox.\n" +
                               "• La sesión requirió autenticación multifactor (MFA), impidiendo el uso de credenciales expuestas.",
                    footerNotice = "ESTADO: INCIDENTE CONTENIDO. Buzones de 16 destinatarios purgados automáticamente."
                }
            };
            workstations.Add(ws1);

            // =========================================================================
            // MONITOR 02: Puesto de Analista de Comercio Exterior (Destinatario #7)
            // =========================================================================
            WorkstationConfig ws2 = new WorkstationConfig
            {
                workstationId = "Monitor_02",
                workstationName = "Puesto de Analista de Comercio Exterior",
                operatorName = "Analista de Operaciones Internacionales",
                roleTitle = "Destinatario #7 de la lista de 16",
                hostname = "PC-PAGOS-02",
                ipAddress = "192.168.10.22",
                hasInteractiveScreen = true,
                hasPostItNote = false,
                postItCredentials = null,
                attackModeScreen = new ScreenContent
                {
                    applicationTitle = "Navegador Web Corporativo - Descargas en Curso",
                    screenStatus = "DESCARGA COMPLETADA - EJECUCIÓN SIN ANÁLISIS SANDBOX",
                    statusColor = Color.red,
                    headerText = "Origen: http://bit.ly-cv-storage.org/Rasel_Ahlam_CV_2015.scr.zip\nDestino: C:\\Users\\Analista\\Downloads\\",
                    bodyText = "Descarga de archivo finalizada:\n\n" +
                               "• Nombre: Rasel_Ahlam_CV_2015.scr.zip\n" +
                               "• Tamaño: 1.42 MB\n" +
                               "• Tipo detectado: Archivo ejecutable Screensaver de Windows (.scr)\n" +
                               "• Inspección perimetral: INEXISTENTE (Red sin filtrado sandbox)\n" +
                               "• Acción del usuario: Doble clic en archivo comprimido.",
                    footerNotice = "ALERTA TÉCNICA: Vector secundario de infección completado en PC-PAGOS-02."
                },
                defenseModeScreen = new ScreenContent
                {
                    applicationTitle = "Navegador Web Corporativo - Filtro Web & Sandbox",
                    screenStatus = "DESCARGA INTERCEPTADA Y BLOQUEADA",
                    statusColor = Color.green,
                    headerText = "URL Solicitada: http://bit.ly-cv-storage.org/Rasel_Ahlam_CV_2015.scr.zip",
                    bodyText = "ACCESO DENEGADO POR POLÍTICA DE CIBERSEGURIDAD:\n\n" +
                               "El motor de Secure Web Gateway (SWG) y Sandbox perimetral analizó la petición:\n" +
                               "• Reputación del dominio: Desconocido / Sospechoso.\n" +
                               "• Extensión de riesgo: Doble extensión oculta (.scr en contenedor .zip).\n" +
                               "• Resultado de detonación en entorno aislado: Generación de tráfico C2 hostil.",
                    footerNotice = "PROTECCIÓN ACTIVA: Descarga bloqueada. Equipo PC-PAGOS-02 aislado preventivamente."
                }
            };
            workstations.Add(ws2);

            // =========================================================================
            // MONITOR 03: Consola de Registro de Accesos Locales (Evidencia RF-05)
            // =========================================================================
            WorkstationConfig ws3 = new WorkstationConfig
            {
                workstationId = "Monitor_03",
                workstationName = "Consola de Registro de Accesos Locales (SIEM)",
                operatorName = "Monitoreo de Infraestructura y Servidores",
                roleTitle = "Consola de Auditoría de Red y Accesos",
                hostname = "PC-PAGOS-09 / SRV-SIEM",
                ipAddress = "192.168.10.250",
                hasInteractiveScreen = true,
                hasPostItNote = false,
                postItCredentials = null,
                attackModeScreen = new ScreenContent
                {
                    applicationTitle = "Auditoría de Eventos del Sistema (Log 29/01/2015)",
                    screenStatus = "INDICADORES DE COMPROMISO (IoC) DETECTADOS",
                    statusColor = Color.red,
                    headerText = "EVENT LOG - OFICINA DE PAGOS - FECHA: 29 ENE 2015",
                    bodyText = "[09:14:02] MAIL-SRV: Entrada SMTP masiva desde <rasel.ahlam@career-applicants-bd.com> entregada a 16 buzones.\n" +
                               "[09:18:45] PC-PAGOS-01 (c.mendoza): HTTP GET bit.ly-cv-storage.org/Rasel_Ahlam_CV_2015.scr.zip -> 200 OK (1.42MB).\n" +
                               "[09:22:10] PC-PAGOS-02 (analista): HTTP GET bit.ly-cv-storage.org/Rasel_Ahlam_CV_2015.scr.zip -> 200 OK (1.42MB).\n" +
                               "[09:31:05] PC-PAGOS-09 (soporte): HTTP GET bit.ly-cv-storage.org/Rasel_Ahlam_CV_2015.scr.zip -> 200 OK (1.42MB).\n" +
                               "[10:04:12] PC-PAGOS-01: Proceso anómalo creado: C:\\Users\\c.mendoza\\AppData\\Local\\svchost_upd.exe (PID 4820).\n" +
                               "[10:04:14] PC-PAGOS-01: Conexión saliente establecida [PID 4820] -> 185.130.44.82:443 (Backdoor Lazarus C2 Activo).\n" +
                               "[10:15:30] RED-LOCAL: Tráfico anómalo entre PC-PAGOS-01 y Terminal SWIFT sin restricciones de firewall.",
                    footerNotice = "EVIDENCIA CRÍTICA: 16 destinatarios alcanzados, 3 terminales infectadas y backdoor C2 establecido."
                },
                defenseModeScreen = new ScreenContent
                {
                    applicationTitle = "Auditoría de Eventos del Sistema (SIEM - Mitigación)",
                    screenStatus = "DEFENSA EFECTIVA - TRAZABILIDAD Y CONTENCIÓN",
                    statusColor = Color.green,
                    headerText = "EVENT LOG - OFICINA DE PAGOS - SISTEMA PROTEGIDO",
                    bodyText = "[09:14:02] MAIL-GW: Correo masivo entrante desde dominio no confiable career-applicants-bd.com.\n" +
                               "[09:14:08] SANDBOX: Detonación de Rasel_Ahlam_CV_2015.scr.zip detectó proceso malicioso svchost_upd.exe.\n" +
                               "[09:14:10] FIREWALL-EDGE: IP de destino 185.130.44.82 agregada a la lista negra perimetral.\n" +
                               "[09:14:15] SOC-ALERT: Correo purgado de los 16 buzones antes de interacción de los operadores.\n" +
                               "[09:15:00] MFA-POLICY: Intentos de acceso local bloqueados por falta del token criptográfico del operador.\n" +
                               "[10:00:00] RED-LOCAL: 0 terminales infectadas. Operatividad bancaria íntegra.",
                    footerNotice = "SISTEMA PROTEGIDO: Contención exitosa de la amenaza sin brecha de datos ni fondos."
                }
            };
            workstations.Add(ws3);
        }

        /// <summary>
        /// Crea una instancia en memoria poblada con los datos predeterminados.
        /// </summary>
        public static WorkstationData CreateDefaultWorkstationDataset()
        {
            WorkstationData data = CreateInstance<WorkstationData>();
            data.name = "Workstations_OficinaPagos_Default";
            data.PopulateDefaultData();
            return data;
        }
    }

    /// <summary>
    /// Configuración de cada puesto de trabajo / monitor físico interactivo en la oficina.
    /// </summary>
    [Serializable]
    public class WorkstationConfig
    {
        [Tooltip("Identificador único de la estación (ej: 'Monitor_01').")]
        public string workstationId;

        [Tooltip("Nombre descriptivo del puesto de trabajo.")]
        public string workstationName;

        [Tooltip("Nombre del operador asignado.")]
        public string operatorName;

        [Tooltip("Cargo o rol dentro de la oficina.")]
        public string roleTitle;

        [Tooltip("Nombre de host en la red local (ej: 'PC-PAGOS-01').")]
        public string hostname;

        [Tooltip("Dirección IP estática en la red interna.")]
        public string ipAddress;

        [Tooltip("Indica si el monitor tiene pantalla interactiva activa.")]
        public bool hasInteractiveScreen = true;

        [Tooltip("Indica si tiene adherido un post-it físico.")]
        public bool hasPostItNote;

        [Tooltip("Datos del post-it físico adherido a la terminal si corresponde.")]
        public PostItCredentialData postItCredentials;

        [Header("Contenido de Pantalla por Modo")]
        public ScreenContent attackModeScreen;
        public ScreenContent defenseModeScreen;
    }

    /// <summary>
    /// Estructura de contenido visual desplegado en la pantalla de un monitor.
    /// </summary>
    [Serializable]
    public class ScreenContent
    {
        public string applicationTitle;
        public string screenStatus;
        public Color statusColor = Color.white;
        [TextArea(2, 4)] public string headerText;
        [TextArea(5, 12)] public string bodyText;
        public string footerNotice;
    }

    /// <summary>
    /// Credenciales expuestas físicamente en notas adhesivas (Vulnerabilidad RF-01 / RF-05).
    /// </summary>
    [Serializable]
    public class PostItCredentialData
    {
        public string noteTag = "PostIt_Credenciales";
        public string accountUser = "cmendoza_swift";
        public string plainPassword = "Andino2015*Swift";
        [TextArea(2, 4)] public string displayText;
        [TextArea(2, 4)] public string securityObservation;
    }
}

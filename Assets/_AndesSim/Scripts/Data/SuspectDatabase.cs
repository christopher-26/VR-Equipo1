using System;
using System.Collections.Generic;
using UnityEngine;

namespace AndesSim.Data
{
    /// <summary>
    /// Base de datos de personajes e implicados para el simulador VR ANDES-SIM (BANCO ANDINO).
    /// Escenario 1: "Oficina de pagos" (Equipo 1).
    /// Permite consultar el expediente, coartada, elementos incriminatorios y eximentes de cada sospechoso.
    /// </summary>
    [CreateAssetMenu(fileName = "SuspectDatabase", menuName = "ANDES-SIM/SuspectDatabase", order = 11)]
    public class SuspectDatabase : ScriptableObject
    {
        [Header("--- Registro de Sospechosos e Implicados ---")]
        [Tooltip("Lista oficial de expedientes de sospechosos de la oficina de pagos.")]
        [SerializeField] private List<SuspectProfile> suspects = new List<SuspectProfile>();

        public IReadOnlyList<SuspectProfile> Suspects => suspects;

        private void Reset()
        {
            // Se autocompleta con los datos oficiales al crearse en el Editor
            PopulateDefaultData();
        }

        /// <summary>
        /// Busca un sospechoso por su ID único o fragmento de su nombre.
        /// </summary>
        public SuspectProfile FindSuspect(string idOrName)
        {
            if (string.IsNullOrEmpty(idOrName) || suspects == null) return null;

            for (int i = 0; i < suspects.Count; i++)
            {
                if (suspects[i] == null) continue;

                if (string.Equals(suspects[i].id, idOrName, StringComparison.OrdinalIgnoreCase) ||
                    suspects[i].fullName.IndexOf(idOrName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return suspects[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Puebla la lista con los 4 personajes oficiales del Escenario 1 (Banco Andino).
        /// </summary>
        public void PopulateDefaultData()
        {
            suspects.Clear();

            // 1. Ing. Samuel Torres
            suspects.Add(new SuspectProfile
            {
                id = "SUSPECT_SAMUEL_TORRES",
                fullName = "Ing. Samuel Torres",
                roleTitle = "Jefe de TI",
                isSuspect = true,
                backgroundSummary = "Responsable de la red e infraestructura tecnológica de la oficina de pagos. Operaba bajo una red plana sin firewall perimetral dedicado ni segmentación entre estaciones y terminales SWIFT. Había solicitado formalmente presupuesto para seguridad que fue desestimado por la dirección.",
                incriminatingPoints = "Acceso total e irrestricto de nivel administrador a toda la infraestructura, servidores y topología de la red bancaria.",
                exoneratingPoints = "Constancia documental previa: dejó advertencias y memorandos formales por escrito meses antes alertando sobre la precariedad de la red sin firewall.",
                dossierNotes = "Coartada técnica sólida pero vulnerable a escrutinio por negligencia arquitectónica."
            });

            // 2. Lic. Hugo Benavides
            suspects.Add(new SuspectProfile
            {
                id = "SUSPECT_HUGO_BENAVIDES",
                fullName = "Lic. Hugo Benavides",
                roleTitle = "Director adjunto de Pagos",
                isSuspect = true,
                backgroundSummary = "Supervisor del área operativa. El viernes previo al fin de semana del incidente encontró la impresora de confirmaciones SWIFT apagada y asumió que se trataba de una avería común sin dar reporte formal a seguridad.",
                incriminatingPoints = "Ignoró la primera señal física evidente del ciberataque: la desconexión intencional de la impresora de auditoría que encubrió las transferencias anómalas.",
                exoneratingPoints = "No poseía credenciales técnicas ni conocimientos informáticos para operar directamente la terminal transaccional ni alterar paquetes en la red.",
                dossierNotes = "Fallo grave de procedimiento operativo sin dolo técnico demostrado."
            });

            // 3. Carla Mendoza
            suspects.Add(new SuspectProfile
            {
                id = "SUSPECT_CARLA_MENDOZA",
                fullName = "Carla Mendoza",
                roleTitle = "Operadora SWIFT",
                isSuspect = true,
                backgroundSummary = "Operadora titular con credenciales activas de autorización de pagos. Recibió en su bandeja el correo con malware de 'Rasel Ahlam'. Realizó horas extra atípicas la semana previa y acostumbraba anotar contraseñas en notas físicas para no olvidarlas.",
                incriminatingPoints = "Sus credenciales personales de acceso a la terminal SWIFT fueron las empleadas durante la noche del ataque para autorizar las transferencias fraudulentas.",
                exoneratingPoints = "Higiene de contraseñas deficiente: su contraseña estaba escrita en un post-it pegado bajo su teclado, accesible para cualquier persona que transitara por la oficina.",
                dossierNotes = "Principal vector involuntario (Patient Zero / víctima de ingeniería social masiva)."
            });

            // 4. Renzo Aliaga
            suspects.Add(new SuspectProfile
            {
                id = "SUSPECT_RENZO_ALIAGA",
                fullName = "Renzo Aliaga",
                roleTitle = "Técnico de proveedor externo",
                isSuspect = true,
                backgroundSummary = "Contratista de soporte técnico informático. Estuvo físicamente en las oficinas dos semanas antes del ataque realizando una actualización de software en la terminal SWIFT.",
                incriminatingPoints = "Contacto físico directo y acceso de consola a la terminal sensible de pagos poco tiempo antes de la brecha.",
                exoneratingPoints = "La intervención técnica estuvo estrictamente aprobada por el área de sistemas y documentada en la bitácora oficial de proveedores.",
                dossierNotes = "Verificado en libros de acceso de seguridad; firmas de mantenimiento en regla."
            });
        }

        /// <summary>
        /// Crea e inicializa una instancia de SuspectDatabase con los datos por defecto sin requerir intervención manual en el Inspector.
        /// </summary>
        /// <returns>Instancia lista para consulta en runtime o persistencia.</returns>
        public static SuspectDatabase CreateDefaultDatabase()
        {
            SuspectDatabase database = CreateInstance<SuspectDatabase>();
            database.name = "SuspectDatabase_OficinaPagos";
            database.PopulateDefaultData();
            return database;
        }
    }

    /// <summary>
    /// Perfil detallado de un sospechoso o implicado en la investigación.
    /// </summary>
    [Serializable]
    public class SuspectProfile
    {
        [Tooltip("Identificador único del sospechoso.")]
        public string id;

        [Tooltip("Nombre y título profesional.")]
        public string fullName;

        [Tooltip("Cargo que desempeña en la institución o empresa contratista.")]
        public string roleTitle;

        [Tooltip("Indica si está catalogado como sospechoso activo.")]
        public bool isSuspect = true;

        [TextArea(3, 6)]
        [Tooltip("Perfil general, antecedentes y contexto dentro de la oficina.")]
        public string backgroundSummary;

        [TextArea(2, 5)]
        [Tooltip("Puntos e indicios incriminatorios (En contra).")]
        public string incriminatingPoints;

        [TextArea(2, 5)]
        [Tooltip("Puntos y evidencias eximentes (A favor).")]
        public string exoneratingPoints;

        [TextArea(2, 4)]
        [Tooltip("Conclusión o notas del investigador para el expediente VR.")]
        public string dossierNotes;

        [Tooltip("Retrato o fotografía para la interfaz VR.")]
        public Sprite portraitPhoto;
    }
}

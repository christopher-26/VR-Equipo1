# INFORME DE AUDITORÍA QA Y CUMPLIMIENTO DE RÚBRICA (20 PUNTOS)
**Proyecto:** ANDES-SIM · Caso BANCO ANDINO — Escenario 1: Oficina de pagos (VR)
**Equipo:** Equipo 1 (VR)
**Escena Auditada:** `Oficina_Pagos.unity`
**Fecha y Hora:** `2026-11-12 23:24:58`
**Calificación Obtenida:** **`18.3 / 20.0 PUNTOS`** (91%)

---

## 1. Resumen Ejecutivo de la Evaluación

| Métrica | Valor |
| :--- | :--- |
| **Puntaje Total** | **18.3 / 20.0 Pts** |
| **Criterios Aprobados** | **19 / 21** |
| **Estado de Cumplimiento** | **EXCELENTE (SOBRESALIENTE)** |
| **Plataforma Objetivo** | Google Cardboard XR (Android OpenGLES3, 60 FPS) |

---

## 2. Detalle de Cumplimiento por Criterio de la Rúbrica


### [RF-01] Puestos de Trabajo y Monitores (3.5 Pts)

| Código | Criterio de Evaluación | Puntaje | Estado | Detalles y Evidencia |
| :--- | :--- | :--- | :--- | :--- |
| **`RF01-1`** | Existencia de los 3 puestos de trabajo físicos (RF-01) | **0.00** / 1.00 | ❌ **OBSERVADO** | Falta uno o más puestos de trabajo.<br/>*Evidencia:* `P1: Falta, P2: Falta, P3: Falta` |
| **`RF01-2`** | Existencia de Monitor_01, Monitor_02 y Monitor_03 | **1.00** / 1.00 | ✅ **APROBADO** | Los 3 monitores principales están instanciados.<br/>*Evidencia:* `Monitor_01: True, Monitor_02: True, Monitor_03: True` |
| **`RF01-3`** | Monitores en Capa 'Interactive' (Layer 8) con Collider | **0.75** / 0.75 | ✅ **APROBADO** | Los 3 monitores poseen colisionador y pertenecen a la capa 'Interactive'.<br/>*Evidencia:* `M1 (L:8, Col:True), M2 (L:8), M3 (L:8)` |
| **`RF01-4`** | Pantallas con Canvas en World Space | **0.75** / 0.75 | ✅ **APROBADO** | Los 3 monitores proyectan su interfaz gráfica en RenderMode.WorldSpace para VR.<br/>*Evidencia:* `C1: WorldSpace, C2: WorldSpace, C3: WorldSpace` |

### [RF-02] Correo Malicioso e Interacción VR (3.5 Pts)

| Código | Criterio de Evaluación | Puntaje | Estado | Detalles y Evidencia |
| :--- | :--- | :--- | :--- | :--- |
| **`RF02-1`** | Panel de Correo de «Rasel Ahlam» en Monitor_01 | **1.00** / 1.00 | ✅ **APROBADO** | Panel_Correo presente en la bandeja de entrada de Monitor_01.<br/>*Evidencia:* `Panel: Panel_Correo, Activo: True` |
| **`RF02-2`** | Retícula Central VR (CardboardReticleAdapter en Main Camera) | **1.00** / 1.00 | ✅ **APROBADO** | CardboardReticleAdapter adjunto a la cámara principal con raycast continuo a 15m.<br/>*Evidencia:* `Reticle: True, Camera: Main Camera` |
| **`RF02-3`** | Botones de Acción con Colisionador y GazeInteractable | **1.50** / 1.50 | ✅ **APROBADO** | Botones 'Abrir Enlace' y 'Reportar Phishing' configurados con BoxCollider y GazeInteractable.<br/>*Evidencia:* `Btn_Abrir: True (Col:True), Btn_Reportar: True` |

### [RF-03] Detección de Señales de Phishing (3.5 Pts)

| Código | Criterio de Evaluación | Puntaje | Estado | Detalles y Evidencia |
| :--- | :--- | :--- | :--- | :--- |
| **`RF03-1`** | Controlador Central PhishingInspector | **1.00** / 1.00 | ✅ **APROBADO** | PhishingInspector presente y operativo en Monitor_01.<br/>*Evidencia:* `Inspector: Monitor_01` |
| **`RF03-2`** | Presencia de las 3 Señales de Phishing (Remitente, Urgencia, Enlace) | **1.50** / 1.50 | ✅ **APROBADO** | Las 3 señales (Remitente externo, Urgencia desmedida y Enlace malicioso) están presentes con colisionadores en capa Interactive.<br/>*Evidencia:* `Remitente: True, Urgencia: True, Enlace: True (Total Hotspots: 3)` |
| **`RF03-3`** | Contador de Señales Detectadas (0/3) | **1.00** / 1.00 | ✅ **APROBADO** | Indicador visual de progreso de señales detectadas presente en el correo.<br/>*Evidencia:* `Contador: Txt_SeñalesContador` |

### [RF-04] Bivalencia: Ataque vs Defensa (3.5 Pts)

| Código | Criterio de Evaluación | Puntaje | Estado | Detalles y Evidencia |
| :--- | :--- | :--- | :--- | :--- |
| **`RF04-1`** | Modo Ataque: InfectionSpreadVisualizer y Red Plana | **1.50** / 1.50 | ✅ **APROBADO** | InfectionSpreadVisualizer y los haces LineRenderer de propagación entre escritorios están listos.<br/>*Evidencia:* `Visualizador: True, LineRenderers detectados: 2` |
| **`RF04-2`** | Modo Defensa: MailQuarantine y Consola Sandbox | **1.50** / 1.50 | ✅ **APROBADO** | MailQuarantine y la consola de Sandbox perimetral con bloqueo de 16 buzones están configurados.<br/>*Evidencia:* `MailQuarantine: True, Panel Cuarentena: True` |
| **`RF04-3`** | Iluminación Ambiental Reactiva (AmbientCyberGridEffect) | **0.50** / 0.50 | ✅ **APROBADO** | AmbientCyberGridEffect conmuta la iluminación de la oficina entre Investigación normal, Alerta roja y Escudo cian/verde.<br/>*Evidencia:* `CyberGridEffect: True` |

### [RF-05] Registro de Evidencias Forenses (3.0 Pts)

| Código | Criterio de Evaluación | Puntaje | Estado | Detalles y Evidencia |
| :--- | :--- | :--- | :--- | :--- |
| **`RF05-1`** | GameManager con RegisterEvidence y ScenarioData cargado | **1.00** / 1.00 | ✅ **APROBADO** | GameManager activo en _SYSTEMS con Scenario01_Data.asset enlazado.<br/>*Evidencia:* `GameManager: True, ScenarioData: Oficina de pagos` |
| **`RF05-2`** | Evidencia Física: Post-It de Credenciales ('Evidencia_PostIt') | **1.00** / 1.00 | ✅ **APROBADO** | PostIt_Credenciales presente en el escritorio de Carla con ID 'Evidencia_PostIt' y colisionador interactivo.<br/>*Evidencia:* `Objeto: PostIt_Credenciales, ID: Evidencia_PostIt` |
| **`RF05-3`** | Evidencia de Red: Consola SIEM ('Evidencia_LogAccesos') | **1.00** / 1.00 | ✅ **APROBADO** | AccessLogTerminal configurado en Monitor_03 con ID 'Evidencia_LogAccesos' y botón para registrar evento.<br/>*Evidencia:* `AccessLogTerminal: True, ID: Evidencia_LogAccesos` |

### [Requisitos VR] Navegación, Confort, Audio y Créditos (3.0 Pts)

| Código | Criterio de Evaluación | Puntaje | Estado | Detalles y Evidencia |
| :--- | :--- | :--- | :--- | :--- |
| **`VR-1`** | Portal de Entrada (Vestíbulo con Briefing y Créditos) | **0.00** / 0.75 | ❌ **OBSERVADO** | No se encontró Portal_Entrada.<br/>*Evidencia:* `Portal: ` |
| **`VR-2`** | Puntos de Teletransporte Discreto (Zero Cybersickness) | **0.75** / 0.75 | ✅ **APROBADO** | Los 3 TeleportPoints (Puestos 1, 2 y 3) están activos en capa Interactive con aro luminoso.<br/>*Evidencia:* `TP1: True, TP2: True, TP3: True (Total: 5)` |
| **`VR-3`** | Al Menos 5 Objetos Interactivos con la Retícula (>=5) | **0.50** / 0.50 | ✅ **APROBADO** | Se detectaron 27 objetos interactivos en capa 8 (supera ampliamente el mínimo de 5).<br/>*Evidencia:* `Total interactables válidos: 27 (Mínimo: 5)` |
| **`VR-4`** | Administrador de Audio Procedural (AudioManager) | **0.50** / 0.50 | ✅ **APROBADO** | AudioManager central presente con los 7 tonos matemáticos procedurales.<br/>*Evidencia:* `AudioManager: _SYSTEMS` |
| **`VR-5`** | Pantalla Final, Tablero de Sospechosos y Créditos/Ética | **0.50** / 0.50 | ✅ **APROBADO** | EndScenarioPanelUI, SuspectBoardVR (4 sospechosos) y CreditsAndEthicsModalUI (Leyes 30096/29733) están presentes.<br/>*Evidencia:* `EndPanel: True, SuspectBoard: True, CreditsModal: True` |

---

## 3. Matriz de Requisitos Funcionales Oficiales

1. **[RF-01] Espacio de Trabajo Físico y Monitores:** 3 puestos en L equipados con `Monitor_01`, `Monitor_02` y `Monitor_03` en capa `Interactive` (Layer 8) y Canvases en `World Space`.
2. **[RF-02] Vector de Phishing Inicial:** Correo de «Rasel Ahlam» recibido el 29 de enero de 2015, con interacción por retícula y clic táctil.
3. **[RF-03] Inspección de Señales:** 3 zonas interactivas (`PhishingSignalHotspot`) para remitente falso, sentido de urgencia y enlace al payload ejecutable `.scr.zip`.
4. **[RF-04] Bivalencia de Simulación (Ataque vs Defensa):**
   - **Modo Ataque:** Cinemática de 8s con propagación de haces láser en red plana hacia las computadoras 2 y 3, letrero holográfico de puerta trasera y espionaje de Lazarus.
   - **Modo Defensa:** Aislamiento perimetral en Sandbox Cuckoo, bloqueo de 16 buzones y contención total con 0 infecciones.
5. **[RF-05] Registro de Evidencias Forenses:** Post-It de Carla Mendoza (`Evidencia_PostIt`) y registros SIEM (`Evidencia_LogAccesos`) enlazados a `GameManager`.
6. **[Requisitos VR Cardboard]:** Portal de entrada en vestíbulo, 3 TeleportPoints de navegación discreta (Zero Cybersickness), más de 5 objetos interactivos con la retícula, sintetizador procedural `AudioManager`, tablero táctico de 4 sospechosos (`SuspectBoardVR`) y modal ético-legal (Leyes 30096 y 29733).

---
*Informe generado automáticamente por Scenario01QaValidator de ANDES-SIM.*

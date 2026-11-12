<!-- ============================================================
  README – ANDES-SIM · Caso BANCO ANDINO
  Reemplaza todo lo que diga [CORCHETES] con los datos de tu equipo.
  Las imágenes van en la carpeta: docs/img/
============================================================ -->

<p align="center">
  <!-- 🖼️ IMAGEN 1: Banner o logo del proyecto (recomendado 1200x400 px) -->
 <img width="1581" height="838" alt="image" src="https://github.com/user-attachments/assets/99dad0a1-de40-4aa2-ba96-5f880da2cf59" />

</p>

# 🛡️ ANDES-SIM · OFICINA DE PAGOS

> Simulador en **[VR con Cardboard / AR con Vuforia]** que recrea el ataque de Lazarus al Banco de Bangladesh (2016) sobre el **BANCO ANDINO**, y muestra cómo la defensa lo habría detenido.

![Unity](https://img.shields.io/badge/Unity-6-black?logo=unity)
![Plataforma](https://img.shields.io/badge/Plataforma-Android-3DDC84?logo=android&logoColor=white)
![Tipo](https://img.shields.io/badge/Tipo-[VR%20%7C%20AR]-blue)

---

## 📖 ¿De qué trata?

Dieciséis trabajadores del BANCO ANDINO reciben un correo amable de «Rasel Ahlam», que busca empleo y adjunta un enlace a su currículum. Carla Mendoza está entre ellos. Al menos tres computadoras intentan abrir el archivo; una basta para abrir una puerta trasera en la red.

El jugador
Revisa el correo y decide. En modo defensa, el correo cae en cuarentena.

---

## 🎮 Cómo funciona

1. **Revivir:** recorres el escenario tal como ocurrió en 2016.
2. **Analizar:** encuentras las evidencias y la falla que explotó Lazarus.
3. **Defender:** activas la contramedida y el resultado cambia.

<p align="center">
  <!-- 🖼️ IMAGEN 4: GIF corto del gameplay (máx. 10 s) o capturas de la interacción -->
  <img src="docs/img/04-demo.gif" alt="Demostración del gameplay" width="60%">
</p>

---

## 🔐 Vulnerabilidad y defensa

| | |
|---|---|
| **Vulnerabilidad** | [Qué falla aprovechó Lazarus] |
| **Contramedida** | [Qué control la habría detenido] |
| **Conceptos aplicados** | [Ej. segmentación de red, MFA, monitoreo, etc.] |

---

## 🧱 Arquitectura

Estructura común a todos los equipos en Unity:

`GameManager` · `ScenarioData` · `EvidenceItem` · `DefenseControl` · `UIManager` · `AudioManager`

<p align="center">
  <!-- 🖼️ IMAGEN 5: Diagrama de arquitectura o captura de la jerarquía en Unity -->
  <img width="408" height="672" alt="image" src="https://github.com/user-attachments/assets/78568382-026f-4629-98af-7e733568c299" />
  <img width="632" height="372" alt="image" src="https://github.com/user-attachments/assets/4beaf704-b2d8-4811-b480-7512ea4fb939" />


</p>

---

## ▶️ Instalación

1. Aun en pañales. 
2.Pensado para un celular Android (activa *orígenes desconocidos*).
3. **VR:** colócalo en tus lentes Cardboard · **AR:** imprime los marcadores de `/markers`.

**Para abrir el proyecto:** clona el repo y ábrelo con **Unity 6**.

```bash
git clone (https://github.com/christopher-26/VR-Equipo1)
```

---

## ⚖️ Aviso ético y legal

Proyecto **solo de defensa y simulación**. BANCO ANDINO, sus personajes y los datos son **ficticios**. Los ataques existen únicamente dentro de la app. Basado en hechos reales del robo al Banco de Bangladesh (2016).

---

📌 *Este proyecto es un caso académico de simulación en ciberseguridad.*

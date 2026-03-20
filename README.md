<div align="center">

# 🕯️ Light Them

**A 2D atmospheric platformer where sanity is your greatest resource.**

[![Unity](https://img.shields.io/badge/Engine-Unity%202022%2B-black?style=flat-square&logo=unity)](https://unity.com/)
[![C#](https://img.shields.io/badge/Language-C%23-239120?style=flat-square&logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![URP](https://img.shields.io/badge/Pipeline-URP-blue?style=flat-square)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest)
[![License](https://img.shields.io/badge/License-MIT-yellow?style=flat-square)](LICENSE)

*Navigate a corrupted world guided by a soul companion — but the darkness has a cost.*

</div>

---

## 🌑 Overview

**Light Them** is a 2D Unity platformer built around a **sanity system** that ties your psychological state directly to the visuals, audio, and gameplay. Stray too long from safe zones and the world begins to fracture — your soul companion corrupts, rain closes in, and whispers grow louder.

The game features a fully custom shader pipeline (URP), dynamic audio layering, parallax environments, and a companion soul entity whose appearance shifts between pure and corrupt states based on your sanity.

---

## ✨ Features

### 🧠 Sanity System
The core of the experience. Sanity is a 0–100 value that:
- **Drains** using a quadratic curve the longer you stay outside a Safe Zone (`k·t²` formula), meaning danger escalates over time
- **Recovers** linearly at a configurable rate when you return to safety
- Drives the **Curse Effect shader**, **audio layers**, and your companion's visual state
- Triggers **death** at 0

### 👻 Soul Companion
A particle-based soul that follows the player with smooth interpolation. Its appearance is fully shader-driven:
- Transitions between a **pure** (teal, calm) and **corrupt** (purple, glitchy) state
- Features flame distortion, chroma splitting, wisp effects, and glitch blackouts
- Spawns with a cinematic coalescing animation

### 🔊 Dynamic Audio Architecture
A layered audio system with full runtime control:
- **BGM crossfading** between calm and tense tracks based on sanity thresholds
- **Stackable ambience loops** — heartbeat and whisper layers fade in as sanity falls
- **One-shot stingers** at mid, low, and death sanity thresholds
- **Surface-aware footsteps** via downward raycasting (grass, stone, wood)
- Persistent volume settings via `PlayerPrefs`

### 🌧️ Weather & Shaders
- **RainDrop shader** with smooth random transitions between None / Light / Normal / Heavy states
- **Curse Effect post-processing** that intensifies as sanity drops — a full-screen corruption layer
- **Parallax scrolling** system with per-layer X/Y multipliers

### 🏃 Player
- Responsive 2D platformer movement with physics-based horizontal velocity
- Box-cast ground detection for reliable jump behaviour
- Void fall detection with respawn point support
- Smooth camera follow with configurable offset and lerp speed

---

## 🗂️ Project Structure

```
Assets/
├── Scripts/
│   ├── Audio/
│   │   ├── SoundManager.cs        # Singleton audio engine
│   │   ├── SoundLibrary.cs        # ScriptableObject sound registry
│   │   ├── SoundData.cs           # Per-sound config (clips, pitch, layer)
│   │   ├── SanityAudioHandler.cs  # Drives audio changes from sanity value
│   │   └── FootstepAudioHandler.cs
│   ├── Player/
│   │   ├── Movement.cs            # Run, jump, flip
│   │   └── Attribute.cs           # Sanity, void check, curse effect
│   ├── Soul/
│   │   └── Follow.cs              # Smooth companion following logic
│   ├── Camera/
│   │   └── Follow.cs              # Smooth camera tracking
│   ├── Game/
│   │   └── SafeZone.cs            # Trigger-based sanity safety
│   ├── Parallax/
│   │   └── ParallaxLayer.cs       # Camera-delta parallax scrolling
│   └── Shaders/
│       └── RainDrop/
│           └── RainDropController.cs
└── Materials/
    └── Shaders/
        └── Souls/
            ├── SoulEffect.shader  # Full HLSL soul shader
            └── M_Souls.mat
```

---

## 🚀 Getting Started

### Prerequisites
- **Unity 2022.3 LTS** or later
- **Universal Render Pipeline (URP)** package installed
- **.NET 6 / IL2CPP** scripting backend (Android builds use IL2CPP)

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-org/light-them.git
   cd light-them
   ```

2. **Open in Unity Hub**
   Add the project folder and open with Unity 2022.3+.

3. **Verify URP settings**
   Ensure your `Project Settings > Graphics` asset points to the included URP config (`customRenderPipeline` in QualitySettings).

4. **Assign required references**
   - `SoundManager` → assign a `SoundLibrary` asset in the Inspector
   - `Player/Attribute` → ensure a `CurseEffectController` exists in the scene
   - `RainDropController` → assign the `rainMaterial` using the rain shader

5. **Press Play** in the main scene.

---

## 🔧 Configuration

### Sanity Tuning (`Player/Attribute.cs`)
| Field | Default | Description |
|---|---|---|
| `maxUnsafeTime` | 180s | Seconds until sanity hits 0 at constant unsafe exposure |
| `healRate` | 10/s | Sanity points recovered per second in a safe zone |
| `effectFadeInSpeed` | 0.3 | How fast the curse effect ramps up |
| `effectFadeOutSpeed` | 0.5 | How fast the curse effect fades on safety |

### Audio Thresholds (`SanityAudioHandler.cs`)
| Field | Default | Description |
|---|---|---|
| `tenseBGMThreshold` | 60 | Sanity below this crossfades to tense BGM |
| `heartbeatThreshold` | 50 | Heartbeat ambience starts |
| `whisperThreshold` | 25 | Whisper layer fades in |

### Soul Shader Properties (`M_Souls.mat`)
Key material properties you can tweak per-level or per-soul:
- `_Sanity` — drives the pure/corrupt colour transition (0 = fully corrupt, 1 = fully pure)
- `_FlameSpeed`, `_FlameHeight`, `_FlameWidth` — flame shape
- `_GlitchIntensity`, `_GlitchSpeed` — corruption glitch amount
- `_SpawnTime`, `_SpawnDuration` — coalescing spawn animation timing

---

## 🤝 Contributing

Pull requests are welcome! For major changes, please open an issue first to discuss what you'd like to change.

1. Fork the repo
2. Create your feature branch: `git checkout -b feature/your-feature`
3. Commit: `git commit -m 'Add some feature'`
4. Push: `git push origin feature/your-feature`
5. Open a Pull Request

---

## 📄 License

This project is licensed under the **MIT License** — see [LICENSE](LICENSE) for details.

---

<div align="center">
  <sub>Built with Unity · URP · C# · and too many flickering shaders</sub>
</div>

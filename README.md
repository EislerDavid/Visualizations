# Visualizations

Vizualizace hudby ve stylu **Windows Media Playeru z doby Windows XP** (Alchemy, Ambience, Bars and Waves,
Battery, Musical Colors, Plenoptic, Spikes …) pro zvuk přehrávaný ze **Spotify** (nebo z celého PC).

## Jak to funguje

Spotify neumožňuje oficiální pluginy, proto aplikace běží samostatně vedle něj:

```
Spotify.exe ──► Windows audio engine ──► WASAPI process loopback ──► FFT / analýza ──► OpenGL okno
```

- **Zachytávání zvuku:** WASAPI *process loopback* (Windows 10 build 20348+ / Windows 11) zachytí jen zvuk
  procesu Spotify a jeho podprocesů. Na starších systémech se použije klasický loopback celého výstupního zařízení.
- **Analýza:** FFT spektrum + waveform, detekce beatů, vyhlazení – data pro jednotlivé efekty.
- **Vykreslování:** OpenGL + GLSL shadery (efekty typu Ambience/Battery stojí na zpětné vazbě framebufferu).

## Struktura

| Projekt | Účel |
|---|---|
| `src/Visualizations.Core` | Zachytávání zvuku (NAudio.Wasapi), FFT, analýza signálu. Nezávislé na vykreslování. |
| `src/Visualizations.App` | Okno a vykreslování vizualizací (Silk.NET – OpenGL + GLFW). |

## Požadavky

- Windows 10 (build 20348+) nebo Windows 11 – kvůli zachytávání zvuku jednoho procesu
- .NET SDK 10
- Visual Studio 2026 nebo `dotnet` CLI

## Sestavení a spuštění

```powershell
dotnet build
dotnet run --project src/Visualizations.App
```

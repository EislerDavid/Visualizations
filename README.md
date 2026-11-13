# Visualizations

Vizualizace hudby ve stylu **Windows Media Playeru z doby Windows XP** (Alchemy, Ambience, Bars and Waves,
Battery, Musical Colors, Plenoptic, Spikes …) pro zvuk přehrávaný ze **Spotify** (nebo z celého PC).

## Jak to funguje

Spotify neumožňuje oficiální pluginy, proto aplikace běží samostatně vedle něj:

```
Spotify.exe ──► Windows audio engine ──► WASAPI process loopback ──► FFT / analýza ──► OpenGL okno
```

- **Zachytávání zvuku:** WASAPI *process loopback* zachytí jen zvuk Spotify – cílí na kořenový proces
  `Spotify.exe` a zahrne i jeho podprocesy (zvuk vydává jeden z nich). Když Spotify neběží, použije se
  loopback celého výstupního zařízení.
- **Analýza:** FFT (4096 vzorků, Hannovo okno) → 64 logaritmických pásem 40 Hz – 16 kHz v dB,
  okamžitý náběh, pozvolný pád a padající špičky.
- **Vykreslování:** OpenGL 3.3 + GLSL shadery (efekty typu Ambience/Battery budou stát na zpětné vazbě framebufferu).

## Vizualizace

| Název | Stav |
|---|---|
| Bars (Bars and Waves) | ✅ spektrum se sloupci a padajícími špičkami |

## Struktura

| Projekt | Účel |
|---|---|
| `src/Visualizations.Core` | Zachytávání zvuku (NAudio.Wasapi), FFT, analýza spektra. Nezávislé na vykreslování. |
| `src/Visualizations.App` | Okno a vykreslování vizualizací (Silk.NET – OpenGL + GLFW). |
| `tests/Visualizations.Core.Tests` | Testy FFT, kruhového bufferu a analyzátoru (xUnit). |

## Požadavky

- Windows 11 nebo Windows 10 verze 2004 (build 19041) a novější – kvůli zachytávání zvuku jednoho procesu
- .NET SDK 10
- Visual Studio 2026 nebo `dotnet` CLI

## Sestavení a spuštění

```powershell
dotnet build
dotnet test
dotnet run --project src/Visualizations.App                       # zvuk Spotify
dotnet run --project src/Visualizations.App -- --system           # celý výstup PC
dotnet run --project src/Visualizations.App -- --process firefox  # jiná aplikace
```

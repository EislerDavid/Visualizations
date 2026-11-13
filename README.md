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
- **Vykreslování:** OpenGL 3.3 + GLSL shadery. Každá animace kreslí do vlastní textury, kompozitor je
  prolíná (přechody, mix dvou animací). Toolbar je Dear ImGui.

## Vizualizace

| Název | Stav |
|---|---|
| Bars and Waves: Bars | ✅ spektrum se sloupci a padajícími špičkami |
| Bars and Waves: Scope | ✅ osciloskop – zářící průběh signálu |

## Ovládání

Lišta dole se ukáže při pohybu myši a po 3 s nečinnosti se schová (lze připnout).

- **Výběr animace** – rozbalovací seznam, šipky ◀ ▶; změna animace je plynulé prolnutí.
- **Střídat** – automatické přepínání po nastaveném intervalu, volitelně v náhodném pořadí.
- **Mix** – druhá animace (B) přes první (A): crossfader, režim míchání (prolínání, sčítání, závoj,
  zesvětlení, rozdíl) a automatický crossfader, který se sám přelévá mezi A a B.
- **Citlivost** – zesílení ±3 dB.
- **…** – zdroj zvuku (Spotify / celý výstup PC, znovu připojení), interval střídání, délka přechodu.

| Klávesa | Akce |
|---|---|
| ← / → | předchozí / další animace |
| 1 – 9 | animace podle čísla v nabídce |
| ↑ / ↓ | citlivost ± 3 dB |
| A | automatické střídání |
| M | mix dvou animací |
| F, F11, dvojklik | celá obrazovka (Esc zpět) |
| T | připnout lištu |
| F1 | nápověda |

Nastavení (animace, citlivost, mix …) se při zavření ukládá do `%APPDATA%\Visualizations\settings.json`.

## Struktura

| Projekt | Účel |
|---|---|
| `src/Visualizations.Core` | Zachytávání zvuku (NAudio.Wasapi), FFT, analýza spektra. Nezávislé na vykreslování. |
| `src/Visualizations.App` | Okno, vizualizace (`Effects`), mix a přechody (`Mixing`), OpenGL pomůcky (`Rendering`), toolbar (`Ui`). Silk.NET (OpenGL + GLFW) a Dear ImGui. |
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

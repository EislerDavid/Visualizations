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

### Kaleidoskopy (Battery)

Jeden kaleidoskopický engine (`Effects/Kaleidoscope`), presety jsou jen sady parametrů – nový preset je
nový řádek v `KaleidoscopePresets`. Mezi sebou kaleidoskopy přecházejí **plynulou proměnou** (interpolace
pohybu a barev, prolnutí počtu výsečí a tvaru), ne prolínáním dvou obrazů.

| Preset | Výseče | Co kreslí |
|---|---|---|
| Kaleidoscope | 6 | průběh signálu přes střed |
| Mandala | 8, fraktálově | růžice se 4 lístky podle spektra |
| Stardust | 12 | body na oběžných drahách |
| Spiral | 5 | vlnící se spirála, silné víření |
| Crystal | 3, fraktálově | hvězda – cípy podle basů a výšek |
| Web | 10 | paprsky podle spektra, obraz se smršťuje |
| Lotus | 7, fraktálově | růžice se 7 lístky, pomalá |
| Tunnel | 6 | kruh, z každého úderu odletí prstenec |
| Harmony | 4, fraktálově | Lissajousova křivka 3 : 2 |
| Supernova | 8 | výbuchy paprsků na každý úder |

### Ostatní

| Název | Stav |
|---|---|
| Bars and Waves: Bars | ✅ spektrum se sloupci a padajícími špičkami |
| Bars and Waves: Scope | ✅ osciloskop – zářící průběh signálu |
| Bars and Waves: Fire Storm | ✅ spektrum jako zdroj žáru, plameny stoupají a víří |
| Ambience: Swirl | ✅ kruhový průběh signálu ve víru zpětné vazby, basy a údery ho rozpohybují |
| Spikes: Starburst | ✅ hvězdice paprsků podle spektra, jádro pulzuje s údery |

Názvy rodin odpovídají WMP; samotné animace jsou vlastní interpretace v jejich duchu.

Většina efektů WMP stojí na **zpětné vazbě**: nový snímek vzniká z předchozího (otočení, přiblížení,
posun, útlum) a přikreslí se do něj čerstvý obsah podle zvuku. Základ je v `FeedbackVisualization`;
efekty čistě ve fragment shaderu dědí z `ShaderVisualization`. Shadery dostávají spektrum jako texturu,
úrovně basů / středů / výšek a detekci úderů (kopáků).

## Ovládání

Lišta dole se ukáže při pohybu myši a po 3 s nečinnosti se schová (lze připnout).

- **Výběr animace** – rozbalovací seznam seskupený podle rodin (nahoře „Náhodně“), šipky ◀ ▶.
- **Střídat** – automatické přepínání po nastaveném intervalu; ve výchozím stavu **jen mezi kaleidoskopy**
  a **na úder** (po uplynutí intervalu počká na nejbližší kopák). Výběr rodiny, interval, náhodné pořadí
  a délka přechodu jsou v nabídce „…“.
- **Mix** – druhá animace (B) přes první (A): crossfader, režimy prolínání, sčítání, závoj, zesvětlení,
  rozdíl a pro kaleidoskopy **Kruh** (B se rozpíná ze středu) a **Výseče** (B prorůstá 12 výsečemi).
  **Automatický crossfader** se sám přelévá mezi A a B a s volbou **Obměňovat** vymění animaci, která je
  právě úplně skrytá, za jinou z výběru – každý návrat crossfaderu tak přinese novou kombinaci.
- **Citlivost** – zesílení ±3 dB.
- **…** – zdroj zvuku (Spotify / celý výstup PC, znovu připojení) a nastavení střídání.

| Klávesa | Akce |
|---|---|
| ← / → | předchozí / další animace |
| 1 – 9 | animace podle čísla v nabídce (kaleidoskopy jsou první) |
| R | náhodná animace z výběru pro střídání |
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

# Fortnite 1.10 Launcher

Questo progetto è un Launcher personalizzato per Fortnite versione 1.10. Include funzionalità per iniettare `Cobalt.dll` e simulare un login con Discord.

## Prerequisiti

1.  **Visual Studio 2022** (con carico di lavoro Sviluppo desktop .NET).
2.  **I file di gioco di Fortnite versione 1.10** (devi fornirli tu, legalmente).
3.  `Cobalt.dll` (già incluso in questo repository, assicurati che sia nella stessa cartella dell'eseguibile dopo la compilazione).

## Come compilare il progetto

Poiché questo è un progetto C# .NET 6, devi compilarlo su Windows.

1.  Apri `src/FortniteLauncher/FortniteLauncher.csproj` con Visual Studio.
2.  Seleziona la configurazione **Release**.
3.  Clicca su **Compila** -> **Compila soluzione**.
4.  L'eseguibile (`FortniteLauncher.exe`) sarà generato in `src/FortniteLauncher/bin/Release/net6.0-windows/`.

## Installazione

1.  Copia il file `FortniteLauncher.exe` generato nella cartella dove preferisci.
2.  Assicurati che il file `Cobalt.dll` sia nella **stessa cartella** di `FortniteLauncher.exe`.

## Utilizzo

1.  Avvia `FortniteLauncher.exe`.
2.  Clicca su "..." per selezionare il percorso del file `FortniteClient-Win64-Shipping.exe` (che si trova nella cartella del gioco Fortnite 1.10, dentro `FortniteGame/Binaries/Win64/`).
3.  Inserisci il tuo Username o clicca su "Login with Discord" (Simulazione).
4.  Clicca su **LAUNCH FORTNITE**.
5.  Il launcher avvierà il gioco e inietterà `Cobalt.dll` automaticamente.

## Server 24/7

Per giocare online, hai bisogno di un backend server compatibile con la versione 1.10 (spesso chiamato "Hybrid Server" o simile per le vecchie versioni).
Questo launcher è configurato per utilizzare `Cobalt.dll`, che di solito reindirizza il traffico del gioco verso un server privato.
Assicurati di avere un server locale o remoto in esecuzione (ad esempio LawinServer o backend personalizzati per la 1.10) e che `Cobalt.dll` sia configurato correttamente per puntare a quel server.

Se hai bisogno di un server, ti consigliamo di cercare progetti open source come **LawinServer** che supportano vecchie versioni di Fortnite.

## Note

*   Questo progetto è solo a scopo educativo.
*   Non forniamo i file di gioco di Fortnite.

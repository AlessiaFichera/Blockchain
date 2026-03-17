# Blockchain Project

## Descrizione

Questo progetto realizza una blockchain minimale in Go, implementata da una rete di nodi containerizzati con Docker. La rete può essere monitorata e controllata tramite un’applicazione desktop in C#, mentre le funzionalità di analisi dei dati blockchain sono integrate all’interno dell’app tramite Python.

###
---

## Requisiti di Sistema

### Sistema operativo
- **Windows 10 o superiore**  
  (necessario per  Windows Forms)

### Software necessario
| Componente | Software / Versione |
|------------|--------------------|
| Container | Docker Desktop |
| C#  |  .NET SDK 10.0  |
| Python | Python 3.10+ |

### Librerie necessarie
| Libreria | Comando |
|------------|--------------------|
| matplotlib  | `pip install matplotlib` |

###
---

## Avvio del progetto

Per avviare il progetto è necessario avviare Docker Desktop e poi eseguire i seguenti comandi dentro la cartella del progetto:

```bat
cd Blockchain
.\start_all.bat
```
###
---

## Suddivisione del progetto

Il lavoro è stato suddiviso nel seguente modo:

- **Alessia Fichera**: ha sviluppato l’applicazione desktop in C# per il monitoraggio e il controllo della rete, e ha integrato le funzionalità di analisi dei dati blockchain in Python all’interno dell’app.
- **Rosario Grasso**: si è occupato della rete di nodi blockchain in Go, inclusa la logica della blockchain e la containerizzazione con Docker.

###
---
## Librerie utilizzate
### Librerie Go

**Standard (incluse in Go 1.25.4)**  
`fmt`, `os`, `encoding/json`, `net/http`, `time`, `strings`, `strconv`, `log`,  
`bytes`, `encoding/binary`, `encoding/gob`, `encoding/hex`, `math`

**Esterne (gestite da go.mod)**  
- `github.com/btcsuite/btcd/btcutil v1.1.6`  
- `go.etcd.io/bbolt v1.4.3`  
- `golang.org/x/crypto v0.48.0`  
- `golang.org/x/sys v0.41.0` (indirect)

###  Librerie C# / .NET

**Standard (.NET Core / .NET Framework)**  
`System`, `System.Drawing`, `System.Windows.Forms`, `System.Collections.Generic`,
`System.Text.Json`, `System.Diagnostics`,`System.Net.Http`, `System.Text`, `System.Threading.Tasks`  

### Librerie Python

**Standard (Python (3.x))**  
`json`, `urllib.request`

**Esterne**  
`matplotlib.pyplot`


###
---
## Modifiche rispetto alla consegna iniziale
- L’applicazione desktop in C# non si limita a visualizzare la rete, ma consente di interagire con i nodi, eseguire comandi e gestire direttamente il comportamento della rete blockchain.


###
---


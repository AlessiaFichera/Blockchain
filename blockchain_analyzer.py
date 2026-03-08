import json
import matplotlib.pyplot as plt
import urllib.request

class BlockchainStats:
    def __init__(self, json_data: str):
        try:
            full_data = json.loads(json_data)
            # Cerchiamo la chiave 'blocks' nel JSON della blockchain
            self.data = full_data.get('blocks', [])
        except json.JSONDecodeError:
            self.data = []
            print("Errore nella decodifica dei dati JSON della blockchain.")

    def calcola_tempo_medio_mining(self, n_blocchi=10):
        # Placeholder: qui andrebbe la logica basata sui timestamp dei blocchi
        if len(self.data) < 2:
            return 0
        return 10.5 

    def totale_transazioni_rete(self):
        conteggio = 0
        for blocco in self.data:
            lista_tx = blocco.get('transactions', [])
            conteggio += len(lista_tx)
        return conteggio

    def analizza_frammentazione_utxo(self, json_utxo: str):
        tutti_utxo = []
        try:
            # Decodifica il JSON degli UTXO
            if isinstance(json_utxo, (bytes, str)):
                dati_utxo_set = json.loads(json_utxo)
            else:
                dati_utxo_set = json_utxo

            # Accediamo alla lista 'utxos' (minuscolo come nel tuo JSON)
            lista_utxo = dati_utxo_set.get('utxos', [])
            
            for entry in lista_utxo:
                # Recuperiamo il valore e forziamo la conversione in float
                valore = entry.get('value', 0)
                if valore > 0:
                    tutti_utxo.append(float(valore))

            # Se la lista è vuota, evitiamo la divisione per zero
            if not tutti_utxo:
                print("Attenzione: nessun UTXO con valore > 0 trovato.")
                return 0, 0
            
            # CALCOLO DELLA MEDIA (ora dentro la funzione)
            valore_medio = sum(tutti_utxo) / len(tutti_utxo)
            numero_utxo = len(tutti_utxo)
            
            # Generazione del grafico
            plt.figure(figsize=(9, 5))
            plt.hist(tutti_utxo, bins=10, color='skyblue', edgecolor='white', linewidth=1.2)
            plt.title("Distribuzione Valori UTXO", fontsize=14, fontweight='bold')
            plt.xlabel("Valore (BTC)")
            plt.ylabel("Frequenza")
            plt.grid(axis='y', linestyle='--', alpha=0.7)
            plt.tight_layout()
            plt.savefig("grafico_blockchain.png") 
            plt.close()
            
            return valore_medio, numero_utxo

        except Exception as e:
            print(f"Errore durante l'analisi UTXO: {e}")
            return 0, 0

if __name__ == "__main__":
    # URL dei tuoi endpoint API
    URL_BLOCKS = "http://localhost:8080/api/print-blockchain"
    URL_UTXOS = "http://localhost:8080/api/print-utxoset"

    try:
        # 1. Recupero dati Blockchain
        with urllib.request.urlopen(URL_BLOCKS) as resp:
            data_blocks = resp.read()
        
        # 2. Recupero dati UTXO
        with urllib.request.urlopen(URL_UTXOS) as resp:
            data_utxos = resp.read()

        # Istanza della classe e analisi
        stats = BlockchainStats(data_blocks)
        media, num = stats.analizza_frammentazione_utxo(data_utxos)

        # 3. Creazione file JSON per l'integrazione con C#
        risultati = {
            "statistiche": {
                "tempo_medio_mining": stats.calcola_tempo_medio_mining(),
                "totale_transazioni": stats.totale_transazioni_rete(),
                "utxo_totale": num,
                "valore_medio_btc": round(media, 2)
            },
            "file_grafico": "grafico_blockchain.png"
        }

        with open("analitiche.json", "w") as f:
            json.dump(risultati, f, indent=4)
        
        print(f"Analisi completata! Media: {media}, UTXO totali: {num}")

    except Exception as e:
        print(f"Errore critico durante l'esecuzione: {e}")
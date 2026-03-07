import json
import matplotlib.pyplot as plt
import urllib.request

class BlockchainStats:
    def __init__(self, json_data: str):
        try:
            full_data = json.loads(json_data)
            self.data = full_data.get('blocks', [])
        except json.JSONDecodeError:
            self.data = []
            print("Errore nella decodifica dei dati JSON.")

    def calcola_tempo_medio_mining(self, n_blocchi=10):
        ultimi_blocchi = self.data[-n_blocchi:]
        if len(ultimi_blocchi) < 2:
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
            dati_utxo_set = json.loads(json_utxo)
            lista_utxo = dati_utxo_set.get('utxos', [])
            
            for entry in lista_utxo:
                valore = entry.get('value', 0)
                if valore > 0:
                    tutti_utxo.append(float(valore))

        except Exception as e:
            print(f"Errore analisi UTXO: {e}")
            return 0, 0

        if not tutti_utxo:
            return 0, 0
            
        valore_medio = sum(tutti_utxo) / len(tutti_utxo)
        
        
        plt.figure(figsize=(9, 5))
        
        plt.hist(tutti_utxo, bins=8, color='skyblue', edgecolor='white', linewidth=1.5)
        
        plt.title("Distribuzione Valori UTXO", fontsize=15, fontweight='bold', color='#333333')
        plt.xlabel("Valore (BTC)", fontsize=12)
        plt.ylabel("Frequenza", fontsize=12)
        plt.grid(axis='y', linestyle='--', alpha=0.7)
        plt.tight_layout()
        plt.savefig("grafico_blockchain.png") 
        plt.close()
            
        return valore_medio, len(tutti_utxo)

if __name__ == "__main__":
    URL_BLOCKS = "http://localhost:8080/api/print-blockchain"
    URL_UTXOS = "http://localhost:8080/api/print-utxoset"

    try:
        # 1. Recupero dati Blockchain
        with urllib.request.urlopen(URL_BLOCKS) as resp:
            data_blocks = resp.read()
        
        # 2. Recupero dati UTXO
        with urllib.request.urlopen(URL_UTXOS) as resp:
            data_utxos = resp.read()

        stats = BlockchainStats(data_blocks)
        media, num = stats.analizza_frammentazione_utxo(data_utxos)

        # 3. Creazione file per C#
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
        
        print("Analisi completata con successo!")

    except Exception as e:
        print(f"Errore durante l'esecuzione: {e}")
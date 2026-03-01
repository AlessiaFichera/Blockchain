import json
import matplotlib.pyplot as plt

class BlockchainStats:

    def __init__(self, json_data: str):
        try:
            self.data = json.loads(json_data)
        except json.JSONDecodeError:
            self.data = []
            print("Errore nella decodifica dei dati JSON.")

    def calcola_tempo_medio_mining(self, n_blocchi=10):

        ultimi_blocchi = self.data[-n_blocchi:]
    
        if len(ultimi_blocchi) < 2:
         return 0
    
        differenze = [ultimi_blocchi[i]['Timestamp'] - ultimi_blocchi[i-1]['Timestamp'] 
                     for i in range(1, len(ultimi_blocchi))]
    
        return sum(differenze) / len(differenze)

    def totale_transazioni_rete(self):
        conteggio = 0
        for blocco in self.data:
            lista_transazioni = blocco.get('Transactions', [])
            conteggio += len(lista_transazioni)
        return conteggio

    def analizza_frammentazione_utxo(self):
        tutti_utxo = []
        for blocco in self.data:
            lista_tx = blocco.get('Transactions', [])
            
            for tx in lista_tx:
                outputs = tx.get('Outputs', [])
                for out in outputs:
                    # 1. Prendiamo il valore e trasformiamolo in stringa per poterlo controllare carattere per carattere
                    valore_raw = str(out.get('Value', "0"))
                    
                    solo_numeri = ""
                    # 2. Controllo isdigit carattere per carattere (come prima!)
                    for carattere in valore_raw:
                        if carattere.isdigit() or carattere == "." or carattere == ",":
                            solo_numeri += carattere
                    
                    # 3. Conversione robusta con gestione errore
                    if solo_numeri != "":
                        try:
                            valore_finale = float(solo_numeri.replace(",", "."))
                            if valore_finale > 0:
                                tutti_utxo.append(valore_finale)
                        except ValueError:
                            continue
        
        if not tutti_utxo:
            print("Nessun valore numerico trovato.")
            return 0, 0
            
        valore_medio = sum(tutti_utxo) / len(tutti_utxo)
        
        # Grafico
        plt.figure(figsize=(10, 6))
        plt.hist(tutti_utxo, bins=8, color='royalblue', edgecolor='white', alpha=0.8)
        plt.title("Analisi Valori Transazioni Blockchain(UTXO)", fontsize=14, fontweight='bold')
        plt.xlabel("Importo in BTC (€)", fontsize=12)
        plt.ylabel("Frequenza (Numero di Blocchi)", fontsize=12)
        plt.grid(axis='y', linestyle='--', alpha=0.6)
        plt.savefig("grafico_blockchain.png") 
        print("Grafico salvato come grafico_blockchain.png")
        
        return len(tutti_utxo), valore_medio

    def __str__(self):
        return f"BlockchainStats con {len(self.data)} blocchi caricati."

if __name__ == "__main__":
    try:
        with open("blockchain.json", "r") as file:
            contenuto = file.read()
            stats = BlockchainStats(contenuto)
            
            print(stats)
            print(f"Tempo medio mining: {stats.calcola_tempo_medio_mining()}s")
            print(f"Totale transazioni effettive: {stats.totale_transazioni_rete()}")
            
            num, media = stats.analizza_frammentazione_utxo()
            print(f"UTXO totali trovati: {num}, Valore medio: {media:.2f}€")
            
    except FileNotFoundError:
        print("Errore: il file 'blockchain.json' non esiste.")

    risultati= {
    "statistiche": {
        "tempo_medio_mining": stats.calcola_tempo_medio_mining(),
        "totale_transazioni": stats.totale_transazioni_rete(),
        "utxo_totale": num,
        "valore_medio_euro": round(media, 2)
    },
    "file_grafico": "grafico_blockchain.png" 
}

with open("analitiche.json", "w") as f:
    json.dump(risultati, f, indent=4)

        
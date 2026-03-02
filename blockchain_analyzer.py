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

    # ORA È INDENTATA DENTRO LA CLASSE
    def analizza_frammentazione_utxo(self, percorso_file="utxoset.json"):
        tutti_utxo = []
        try:
            with open(percorso_file, 'r', encoding='utf-8') as f:
                dati_utxo_set = json.load(f)
                
            for utxo_entry in dati_utxo_set:
                lista_outputs = utxo_entry.get('Outputs', [])
                if lista_outputs:
                    for out in lista_outputs:
                        valore_raw = out.get('Value', 0)
                        try:
                            valore_finale = float(valore_raw)
                            if valore_finale > 0:
                                tutti_utxo.append(valore_finale)
                        except (ValueError, TypeError):
                            continue

        except FileNotFoundError:
            print(f"Errore: Il file {percorso_file} non è presente.")
            return 0, 0
        except json.JSONDecodeError:
            print("Errore: Formato JSON non valido.")
            return 0, 0

        if not tutti_utxo:
            return 0, 0
            
        valore_medio = sum(tutti_utxo) / len(tutti_utxo)
        
        # Generazione Grafico
        plt.figure(figsize=(10, 6))
        plt.hist(tutti_utxo, bins=8, color='royalblue', edgecolor='white', alpha=0.8)
        plt.title("Analisi Valori Transazioni Blockchain (UTXO)", fontsize=14, fontweight='bold')
        plt.xlabel("Importo in BTC (€)", fontsize=12)
        plt.ylabel("Frequenza", fontsize=12)
        plt.grid(axis='y', linestyle='--', alpha=0.6)
        plt.savefig("grafico_blockchain.png") 
        plt.close() # Chiude la figura per liberare memoria
            
        # Ritorna (media, numero_elementi) per far combaciare con la tua chiamata nel main
        return valore_medio, len(tutti_utxo)

    def __str__(self):
        return f"BlockchainStats con {len(self.data)} blocchi caricati."

if __name__ == "__main__":
    # Inizializzazione variabili di sicurezza
    media, num = 0, 0
    stats = None

    try:
        with open("blockchain.json", "r") as file:
            contenuto = file.read()
            stats = BlockchainStats(contenuto)
            
            print(stats)
            print(f"Tempo medio mining: {stats.calcola_tempo_medio_mining()}s")
            print(f"Totale transazioni effettive: {stats.totale_transazioni_rete()}")
            
            # Ora la chiamata funzionerà perché il metodo è dentro la classe
            media, num = stats.analizza_frammentazione_utxo()
            print(f"UTXO totali trovati: {num}, Valore medio: {media:.2f}€")
            
    except FileNotFoundError:
        print("Errore: il file 'blockchain.json' non esiste.")

    # Creazione JSON analitiche (solo se stats è stato creato)
    if stats:
        risultati = {
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
            print("Analitiche salvate in analitiche.json")
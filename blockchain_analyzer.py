import json
import matplotlib.pyplot as plt
import urllib.request
from datetime import datetime

class BlockchainStats:
    def __init__(self, json_data: str):
        try:
            full_data = json.loads(json_data)
            # La chiave nel tuo JSON è "blocks un dizionario con chiave "blocks" e valore una lista di blocchi, quindi accediamo a quella lista
            #ogni blocco è un dizionario con chiavi come "height", "timestamp", "nonce", "transactions", ecc.
            self.data = full_data.get('blocks', [])
            #self.data è ora una lista di blocchi, ognuno dei quali è un dizionario con le informazioni del blocco
            print(f"Caricati {len(self.data)} blocchi.")
        except json.JSONDecodeError:
            self.data = []
            print("Errore nella decodifica dei dati JSON della blockchain.")

    def calcola_tempo_medio_mining(self):
        """Converte i timestamp e calcola la media delle differenze"""
        if len(self.data) < 2:
            return 0
        
        times = []
        for b in self.data:
            # Converte la stringa "08/03/2026 19:50:01" in numero (float)
            dt = datetime.strptime(b['timestamp'], "%d/%m/%Y %H:%M:%S")
            times.append(dt.timestamp())
        
        # Ordiniamo i tempi dal più vecchio al più recente
        times.sort()
        
        differenze = [
        times[i] - times[i-1] for i in range(1, len(times))
        ]
        
        # Usa sum() e len() come descritto nel file docx
        return sum(differenze) / len(differenze)

    def genera_grafico_nonce(self):
        """Genera il grafico dello sforzo minerario"""
        indices = []
        nonces = []

        for blocco in reversed(self.data):
            indices.append(blocco.get('height'))
            nonces.append(blocco.get('nonce'))

        if not nonces: return

        plt.figure(figsize=(10, 5))
        plt.plot(indices, nonces, marker='o', color='royalblue', linewidth=2, label='Nonce')
        plt.title("Sforzo Minerario (Nonce) per Altezza Blocco", fontsize=14, fontweight='bold')
        plt.xlabel("Altezza (Height)")
        plt.ylabel("Valore Nonce")
        plt.grid(True, alpha=0.3)
        plt.legend()
        plt.tight_layout()
        plt.savefig("grafico_nonce.png")
        plt.close()

    def totale_transazioni_rete(self):
        """Conta il numero totale di transazioni in tutti i blocchi"""
        conteggio = 0
        for blocco in self.data:
            lista_tx = blocco.get('transactions', [])
            conteggio += len(lista_tx)
        return conteggio

    def analizza_frammentazione_utxo(self, json_utxo: str):
        """Analizza i valori degli UTXO e genera l'istogramma"""
        tutti_utxo = []
        try:
            dati_utxo_set = json.loads(json_utxo) if isinstance(json_utxo, (bytes, str)) else json_utxo
            lista_utxo = dati_utxo_set.get('utxos', [])
            
            for entry in lista_utxo:
                valore = entry.get('value', 0)
                if valore > 0:
                    # Casting esplicito a float come richiesto dal manuale .docx
                    tutti_utxo.append(float(valore))

            if not tutti_utxo:
                return 0
            
            # Calcolo della media
            valore_medio = sum(tutti_utxo) / len(tutti_utxo)
            
            # Grafico UTXO
            plt.figure(figsize=(9, 5))
            plt.hist(tutti_utxo, bins=10, color='skyblue', edgecolor='white')
            plt.title("Distribuzione Valori UTXO", fontsize=14, fontweight='bold')
            plt.xlabel("Valore (BTC)")
            plt.ylabel("Frequenza")
            plt.tight_layout()
            plt.savefig("grafico_blockchain.png") 
            plt.close()
            
            return valore_medio

        except Exception as e:
            print(f"Errore analisi UTXO: {e}")
            return 0

    def calcola_ricchi_della_rete(self):
        """Usa un dizionario per mappare pubkey_hash -> bilancio totale"""
        bilanci = {}
        
        for blocco in self.data:
            for tx in blocco.get('transactions', []):
                for output in tx.get('vout', []):
                    indirizzo = output.get('pubkey_hash')
                    valore = float(output.get('value', 0))
                    
                    bilanci[indirizzo] = bilanci.get(indirizzo, 0) + valore
        
        # Restituiamo i primi 3 indirizzi più ricchi
        top_ricchi = sorted(bilanci.items(), key=lambda x: x[1], reverse=True)[:3]
        return top_ricchi

    def calcola_difficolta_media(self):
        """Conta la media degli zeri iniziali negli hash (PoW)"""
        totale_zeri = 0
        if not self.data: return 0

        for blocco in self.data:
            h = blocco.get('hash', "")
            # Conteggio zeri iniziali usando lstrip
            conteggio = len(h) - len(h.lstrip('0'))
            totale_zeri += conteggio
        
        return totale_zeri / len(self.data)

if __name__ == "__main__":
    URL_BLOCKS = "http://localhost:8080/api/print-blockchain"
    URL_UTXOS = "http://localhost:8080/api/print-utxoset"

    try:
        # Recupero dati dalle API
        with urllib.request.urlopen(URL_BLOCKS) as resp:
            data_blocks = resp.read()
        with urllib.request.urlopen(URL_UTXOS) as resp:
            data_utxos = resp.read()

        # Inizializzazione e calcoli
        stats = BlockchainStats(data_blocks)
        
        # Abbiamo rimosso il conteggio totale degli UTXO come richiesto
        media_utxo = stats.analizza_frammentazione_utxo(data_utxos)
        stats.genera_grafico_nonce()

        # Creazione del dizionario finale dei risultati
        risultati = {
            "statistiche": {
                "tempo_medio_mining": round(stats.calcola_tempo_medio_mining(), 2),
                "totale_transazioni": stats.totale_transazioni_rete(),
                "valore_medio_btc": round(media_utxo, 2),
                "difficolta_media": round(stats.calcola_difficolta_media(), 2),
                "top_ricchi": stats.calcola_ricchi_della_rete()
            },
            "file_grafico": "grafico_blockchain.png",
            "sforzo_nonce": "grafico_nonce.png"
        }

        # Salvataggio su file JSON per l'interfaccia C#
        with open("analitiche.json", "w") as f:
            json.dump(risultati, f, indent=4)
        
        print("Analisi completata con successo! File 'analitiche.json' generato.")

    except json.JSONDecodeError:
        print("Errore: Impossibile decodificare i dati JSON.")
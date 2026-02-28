package main

import (
	"fmt"
)

func main() {
	storage, err := NewBoltStorage("blockchain.db")
	if err != nil {
		fmt.Println("Errore creazione DB:", err)
	}
	defer storage.Close()
	fmt.Println("DB inizializzato con successo")

	wallet, err := NewWallet()
	if err != nil {
		fmt.Println("Errore creazione wallet:", err)
	}

	address, err := wallet.AddAccount()
	if err != nil {
		fmt.Println("Errore creazione account:", err)
	}
	fmt.Println("\n\nAccount creato, address corrispondente:", address)

	bc, err := NewBlockchain(address, storage)
	if err != nil {
		fmt.Println("Errore nella creazione della blockchain:", err)
	}
	fmt.Println("\n\nBlockchain inizializzata con successo")

	balance, err := getBalance(bc, address)
	if err != nil {
		fmt.Println("Errore nell'ottenimento del bilancio:", err)
	}
	fmt.Println("\n\nBilancio Complessivo: ", balance)

	fmt.Println("\n\n --- Stampa Blockchain ---")
	err = bc.PrintBlockchain()
	if err != nil {
		fmt.Println("Errore durante la stampa della catena:", err)
	}

}

func getBalance(bc *Blockchain, address string) (int, error) {
	balance := 0
	pubKeyHash := AddressToPubKeyHash(address)
	utxos, err := bc.storage.GetUTXO(pubKeyHash)
	if err != nil {
		return 0, err
	}

	for _, utxo := range utxos {
		balance += utxo.TXOutput.Value
	}

	return balance, nil
}

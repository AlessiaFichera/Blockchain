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

	account, err := wallet.AddAccount()
	if err != nil {
		fmt.Println("Errore creazione account:", err)
	}

	fmt.Println("Account creato:", account)

	bc, err := NewBlockchain(account, storage)
	if err != nil {
		fmt.Println("Errore nella creazione della blockchain:", err)
	}

	fmt.Println("Blockchain inizializzata con successo")

	// transazioni

	fmt.Println(" --- Stampa Blockchain ---")
	err = bc.PrintBlockchain()
	if err != nil {
		fmt.Println("Errore durante la stampa della catena:", err)
	}

}

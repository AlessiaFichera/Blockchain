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

	// Creazione Account A
	addressA, err := wallet.AddAccount()
	if err != nil {
		fmt.Println("Errore creazione account:", err)
	}
	fmt.Println("\n\nAccount creato, address corrispondente:", addressA)

	// Creazione Account B
	addressB, err := wallet.AddAccount()
	if err != nil {
		fmt.Println("Errore creazione account:", err)
	}
	fmt.Println("\n\nAccount creato, address corrispondente:", addressB)

	// Creazione Blockcahin con Account A
	bc, err := NewBlockchain(addressA, storage)
	if err != nil {
		fmt.Println("Errore nella creazione della blockchain:", err)
	}
	fmt.Println("\n\nBlockchain inizializzata con successo")

	// Bilanci degli address
	getBalance(bc, addressA)
	getBalance(bc, addressB)

	// Send da A a B
	send(bc, addressA, addressB, 2)

	// Nuovi Bilanci:
	fmt.Println("\n\nStampa nuovo bilancio:")
	getBalance(bc, addressA)
	getBalance(bc, addressB)

	fmt.Println("\n\n --- Stampa Blockchain ---")
	err = bc.PrintBlockchain()
	if err != nil {
		fmt.Println("Errore durante la stampa della catena:", err)
	}

}

func getBalance(bc *Blockchain, address string) int {
	pubKeyHash := AddressToPubKeyHash(address)
	balance, _, err := bc.storage.GetUTXO(pubKeyHash, 0)
	if err != nil {
		fmt.Println("Errore nell'ottenimento del bilancio:", err)
		return 0
	}
	fmt.Printf("\n\nBilancio Complessivo di %s: %d \n", address, balance)

	return balance
}

func send(bc *Blockchain, address string, to string, amount int) {
	wallet, err := NewWallet()
	if err != nil {
		fmt.Println("Errore creazione wallet:", err)
	}

	account, exists := wallet.Accounts[address]
	if !exists {
		fmt.Printf("account con indirizzo %s non trovato nel wallet", address)
	}

	tx, err := NewTransaction(bc, account, to, amount)
	if err != nil {
		fmt.Printf("%s", err)
	}

	// Mining del Blocco con la transazione creata
	bc.AddBlock([]*Transaction{tx})

	fmt.Printf("Transazione creata! \n %s", tx)
}

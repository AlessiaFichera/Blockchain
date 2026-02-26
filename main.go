package main

import (
	"log"
)

func main() {
	storage, err := NewBoltStorage("blockchain.db")
	if err != nil {
		log.Panic(err)
	}
	defer storage.Close()

	bc, err := NewBlockchain(storage)
	if err != nil {
		log.Panic(err)
	}

	err = bc.AddBlock("Inviato 1 BTC a Mario")
	if err != nil {
		log.Printf("Errore aggiunta blocco 1: %s", err)
	}

	err = bc.AddBlock("Inviato 2 BTC a Giovanni")
	if err != nil {
		log.Printf("Errore aggiunta blocco 2: %s", err)
	}

	err = bc.PrintBlockchain()
	if err != nil {
		log.Printf("Errore durante la stampa della catena: %v", err)
	}

}

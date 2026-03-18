package main

import (
	"fmt"
	"log"
	"net/http"
	"os"
	"time"

	"go_blockchain/network"
	"go_blockchain/storage"
)

var (
	dbFile      string
	walletFile  string
	nodeName    string
	nodeAddress string
	listenPort  string
)

func init() {

	if dbFile = os.Getenv("DB_FILE"); dbFile == "" {
		log.Fatalf("ERRORE: DB_FILE non configurato")
	}

	if walletFile = os.Getenv("WALLET_FILE"); walletFile == "" {
		log.Fatalf("ERRORE: WALLET_FILE non configurato")
	}

	if nodeName = os.Getenv("NODE_NAME"); nodeName == "" {
		log.Fatalf("ERRORE: NODE_NAME non configurato")
	}

	if listenPort = os.Getenv("PORT"); listenPort == "" {
		log.Fatalf("ERRORE: PORT non configurata")
	}

	nodeAddress = "http://" + nodeName + ":8080"

	fmt.Printf("[%s] Configurato: DB=%s, Port=%s, Address=%s\n", nodeName, dbFile, listenPort, nodeAddress)
}

func main() {
	storage, err := storage.NewBoltStorage(dbFile)
	if err != nil {
		fmt.Printf("[%s] Errore creazione DB: %s\n", nodeName, err)
	}
	defer storage.Close()
	fmt.Printf("[%s] DB inizializzato con successo\n", nodeName)

	server, err := network.StartServer(walletFile, nodeName, nodeAddress)
	if err != nil {
		fmt.Printf("[%s] Errore creazione server: %s \n", nodeName, err)
	}

	router := network.SetupRouter(server)

	go func() {
		fmt.Printf("[%s] Nodo avviato sulla porta %s...\n", nodeName, listenPort)
		if err := http.ListenAndServe(":8080", router); err != nil {
			log.Fatalf("Errore server: %v", err)
		}
	}()

	time.Sleep(time.Second)
	if err := server.Bootstrap(storage); err != nil {
		log.Fatalf("Errore durante il bootstrap: %v", err)
	}

	select {}
}

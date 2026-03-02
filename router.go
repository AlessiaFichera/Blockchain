package main

import (
	"encoding/json"
	"fmt"
	"net/http"
)

// Struttura delle richieste al server
type Job struct {
	Type    string           // es. "CREATE_BLOCK", "SEND_TX"
	Data    interface{}      // i dati in ingresso
	ResChan chan interface{} // canale dove il worker invierà il risultato
}

type HealthResponse struct {
	Status  string `json:"status"`
	Message string `json:"message"`
}

type AddressResponse struct {
	Address string `json:"address"`
	Error   string `json:"error,omitempty"` // omitempty lo nasconde se è vuoto
}

type AddressesResponse struct {
	Addresses []string `json:"addresses"`
	Count     int      `json:"count"`
}

// Configura gli endpoint
func SetupRouter(server *Server) *http.ServeMux {
	mux := http.NewServeMux()

	mux.HandleFunc("/health", healthHandler)

	mux.HandleFunc("/create-address", createAddressHandler)

	mux.HandleFunc("/get-addresses", getAddressesHandler)

	mux.HandleFunc("/version", versionHandler(server))

	mux.HandleFunc("/get-blocks", getBlocksHandler(server))

	mux.HandleFunc("/blocks", blocksHandler(server))

	return mux
}

// Funzione di utilità per gestire gli errori in JSON
func sendError(w http.ResponseWriter, message string, code int) {
	w.WriteHeader(code)
	json.NewEncoder(w).Encode(AddressResponse{Error: message})
}

func healthHandler(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")

	response := HealthResponse{
		Status:  "UP",
		Message: "Blockchain node is running smoothly",
	}

	json.NewEncoder(w).Encode(response)
}

func createAddressHandler(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")

	wallet, err := NewWallet()
	if err != nil {
		sendError(w, "Errore creazione wallet", http.StatusInternalServerError)
		return
	}

	address, err := wallet.AddAccount()
	if err != nil {
		sendError(w, "Errore creazione account", http.StatusInternalServerError)
		return
	}

	fmt.Printf("[%s] Nuovo account creato: %s\n", nodeName, address)

	response := AddressResponse{
		Address: address,
	}
	json.NewEncoder(w).Encode(response)
}

func getAddressesHandler(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")

	wallet, err := NewWallet()
	if err != nil {
		sendError(w, "Errore caricamento wallet", http.StatusInternalServerError)
		return
	}

	addresses := wallet.GetAddresses()

	response := AddressesResponse{
		Addresses: addresses,
		Count:     len(addresses),
	}

	json.NewEncoder(w).Encode(response)
}

func versionHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		var msg VersionMessage
		if err := json.NewDecoder(r.Body).Decode(&msg); err != nil {
			http.Error(w, "Invalid JSON", http.StatusBadRequest)
			return
		}

		// Conferma di ricezione
		w.WriteHeader(http.StatusOK)

		// Passiamo il messaggio al canale per essere gestito sequenzialmente
		s.JobChan <- Job{
			Type: "HANDLE_VERSION",
			Data: msg,
		}
	}
}

func getBlocksHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		var msg GetBlocksMessage
		if err := json.NewDecoder(r.Body).Decode(&msg); err != nil {
			http.Error(w, "Invalid JSON", http.StatusBadRequest)
			return
		}

		// Conferma di ricezione
		w.WriteHeader(http.StatusOK)

		// Passiamo il messaggio al canale per essere gestito sequenzialmente
		s.JobChan <- Job{
			Type: "HANDLE_GET_BLOCKS",
			Data: msg,
		}
	}
}

func blocksHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		var msg BlocksMessage
		if err := json.NewDecoder(r.Body).Decode(&msg); err != nil {
			http.Error(w, "Invalid JSON", http.StatusBadRequest)
			return
		}

		w.WriteHeader(http.StatusOK)

		s.JobChan <- Job{
			Type: "HANDLE_BLOCKS",
			Data: msg,
		}
	}
}

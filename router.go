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

type TransactionRequest struct {
	From   string `json:"from"`
	To     string `json:"to"`
	Amount int    `json:"amount"`
}

type MineResponse struct {
	Message string `json:"message"`
}

// Configura gli endpoint
func SetupRouter(server *Server) *http.ServeMux {
	mux := http.NewServeMux()

	mux.HandleFunc("/api/health", healthHandler)

	mux.HandleFunc("/api/create-address", createAddressHandler)

	mux.HandleFunc("/api/get-addresses", getAddressesHandler)

	mux.HandleFunc("/p2p/version", versionHandler(server))

	mux.HandleFunc("/p2p/get-blocks", getBlocksHandler(server))

	mux.HandleFunc("/p2p/blocks", blocksHandler(server))

	mux.HandleFunc("/api/tx", sendTxHandler(server))

	mux.HandleFunc("/p2p/receive-tx", receiveTxHandler(server))

	mux.HandleFunc("/p2p/inv", invHandler(server))

	mux.HandleFunc("/p2p/get-data", getDataHandler(server))

	mux.HandleFunc("/api/mine", activateMineHandler(server))

	mux.HandleFunc("/api/get-balance", getBalanceHandler(server))

	return mux
}

// Funzione di utilità per gestire gli errori in JSON
func sendError(w http.ResponseWriter, message string, code int) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(code)
	response := map[string]string{
		"status": "error",
		"detail": message,
	}
	prettyPrint(w, response)
}

func healthHandler(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")

	response := HealthResponse{
		Status:  "UP",
		Message: "Blockchain node is running smoothly",
	}

	prettyPrint(w, response)
}

func createAddressHandler(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")

	wallet, err := NewWallet()
	if err != nil {
		sendError(w, "Errore creazione wallet"+err.Error(), http.StatusInternalServerError)
		return
	}

	address, err := wallet.AddAccount()
	if err != nil {
		sendError(w, "Errore creazione account: "+err.Error(), http.StatusInternalServerError)
		return
	}

	fmt.Printf("[%s] Nuovo account creato: %s\n", nodeName, address)

	response := AddressResponse{
		Address: address,
	}
	prettyPrint(w, response)
}

func getAddressesHandler(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")

	wallet, err := NewWallet()
	if err != nil {
		sendError(w, "Errore caricamento wallet: "+err.Error(), http.StatusInternalServerError)
		return
	}

	addresses := wallet.GetAddresses()

	response := AddressesResponse{
		Addresses: addresses,
		Count:     len(addresses),
	}

	prettyPrint(w, response)
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

func sendTxHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		var req TransactionRequest
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, "Invalid Request", http.StatusBadRequest)
			return
		}

		resChan := make(chan interface{})

		s.JobChan <- Job{
			Type:    "SEND_LOCAL_TX",
			Data:    req,
			ResChan: resChan,
		}

		result := <-resChan

		if err, ok := result.(error); ok {
			sendError(w, err.Error(), http.StatusBadRequest)
			return
		}

		response := map[string]string{
			"status":  "success",
			"message": "Transazione validata, firmata e inviata al Central Node",
			"tx_id":   fmt.Sprintf("%x", result.([]byte)), // ID transazione
		}
		prettyPrint(w, response)
	}
}

func receiveTxHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		var msg TxMessage
		if err := json.NewDecoder(r.Body).Decode(&msg); err != nil {
			http.Error(w, "Errore decodifica TxMessage", http.StatusBadRequest)
			return
		}

		s.JobChan <- Job{
			Type: "HANDLE_TX",
			Data: msg,
		}
		w.WriteHeader(http.StatusOK)
	}
}

func invHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		var msg InvMessage
		if err := json.NewDecoder(r.Body).Decode(&msg); err != nil {
			http.Error(w, "Errore decodifica INV", http.StatusBadRequest)
			return
		}

		s.JobChan <- Job{
			Type: "HANDLE_INV",
			Data: msg,
		}

		w.WriteHeader(http.StatusOK)
	}
}

func getDataHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		var msg GetDataMessage
		if err := json.NewDecoder(r.Body).Decode(&msg); err != nil {
			http.Error(w, "Errore decodifica GetData", http.StatusBadRequest)
			return
		}

		s.JobChan <- Job{
			Type: "HANDLE_GET_DATA",
			Data: msg,
		}

		w.WriteHeader(http.StatusOK)
	}
}

func activateMineHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		resChan := make(chan MineResponse)

		s.JobChan <- Job{
			Type: "ACTIVATE_MINE",
			Data: resChan,
		}

		response := <-resChan

		w.Header().Set("Content-Type", "application/json")
		prettyPrint(w, response)
	}
}

func getBalanceHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		address := r.URL.Query().Get("address")
		if address == "" {
			sendError(w, "Parametro 'address' mancante", http.StatusBadRequest)
			return
		}

		resChan := make(chan interface{})

		s.JobChan <- Job{
			Type:    "GET_BALANCE",
			Data:    address,
			ResChan: resChan,
		}

		result := <-resChan

		if err, ok := result.(error); ok {
			sendError(w, err.Error(), http.StatusInternalServerError)
			return
		}

		response := map[string]interface{}{
			"status":  "success",
			"address": address,
			"balance": result.(int),
		}
		prettyPrint(w, response)
	}
}

func prettyPrint(w http.ResponseWriter, v any) {
	encoder := json.NewEncoder(w)
	encoder.SetIndent("", "    ")
	encoder.Encode(v)
}

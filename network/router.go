package network

import (
	"encoding/json"
	"net/http"
)

// Struttura delle richieste al server
type Job struct {
	Type    string   // Tipo di richiesta
	Data    any      // Dati in ingresso
	ResChan chan any // Canale dove il server invierà il risultato
}

type TransactionRequest struct {
	From   string `json:"from"`
	To     string `json:"to"`
	Amount uint64 `json:"amount"`
}

// Risposte all'utente:

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

type MineResponse struct {
	Message string `json:"message"`
}

type BalanceResponse struct {
	Address string `json:"address"`
	Result  string `json:"result"` // Conterrà il valore o l'errore
}

type PeersResponse struct {
	Count int      `json:"count"`
	Peers []string `json:"peers"`
}

type MempoolResponse struct {
	Count int      `json:"count"`
	Txs   []string `json:"txs"`
}

type TransactionResponse struct {
	Status  string `json:"status"`
	Message string `json:"message"`
	TxID    string `json:"tx_id"`
}

type BlockInfo struct {
	Timestamp    string            `json:"timestamp"`
	PrevHash     string            `json:"prev_hash"`
	Hash         string            `json:"hash"`
	Nonce        int               `json:"nonce"`
	Transactions []TransactionInfo `json:"transactions"`
	Height       int               `json:"height"`
}

type BlockchainResponse struct {
	Blocks []BlockInfo `json:"blocks"`
}

type TransactionInfo struct {
	ID   string         `json:"id"`
	Vin  []TXInputInfo  `json:"vin"`
	Vout []TXOutputInfo `json:"vout"`
}

type TXInputInfo struct {
	Txid      string `json:"txid"`
	Vout      int    `json:"vout_index"`
	Signature string `json:"signature"`
	PubKey    string `json:"pubkey"`
}

type TXOutputInfo struct {
	Value      uint64 `json:"value"`
	PubKeyHash string `json:"pubkey_hash"`
}

type UTXOInfo struct {
	TxID       string `json:"tx_id"`
	Index      int    `json:"index"`
	Value      uint64 `json:"value"`
	PubKeyHash string `json:"pub_key_hash"`
}

type UTXOSetResponse struct {
	Count int        `json:"count"`
	UTXOs []UTXOInfo `json:"utxos"`
}

// Configura gli endpoint
func SetupRouter(server *Server) *http.ServeMux {
	mux := http.NewServeMux()

	// Richieste utente API:

	mux.HandleFunc("/api/health", healthHandler)

	mux.HandleFunc("/api/create-address", createAddressHandler(server))

	mux.HandleFunc("/api/get-addresses", getAddressesHandler(server))

	mux.HandleFunc("/api/mine", activateMineHandler(server))

	mux.HandleFunc("/api/get-balance", getBalanceHandler(server))

	mux.HandleFunc("/api/get-peers", getPeersHandler(server))

	mux.HandleFunc("/api/get-mempool", getMempoolHandler(server))

	mux.HandleFunc("/api/tx", sendTxHandler(server))

	mux.HandleFunc("/api/print-blockchain", printBlockchainHandler(server))

	mux.HandleFunc("/api/print-utxoset", getUTXOSetHandler(server))

	// Messaggi tra nodi P2P:

	mux.HandleFunc("/p2p/version", versionHandler(server))

	mux.HandleFunc("/p2p/get-blocks", getBlocksHandler(server))

	mux.HandleFunc("/p2p/blocks", blocksHandler(server))

	mux.HandleFunc("/p2p/receive-tx", receiveTxHandler(server))

	mux.HandleFunc("/p2p/inv", invHandler(server))

	mux.HandleFunc("/p2p/get-data", getDataHandler(server))

	return mux
}

// API handler:

func healthHandler(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")

	response := HealthResponse{
		Status:  "UP",
		Message: "Blockchain node is running smoothly",
	}

	prettyPrint(w, response)
}

func createAddressHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")

		resChan := make(chan any)

		s.JobChan <- Job{
			Type:    "CREATE_ADDRESS",
			ResChan: resChan,
		}

		rawResponse := <-resChan

		if err, ok := rawResponse.(error); ok {
			sendError(w, "Errore durante la creazione dell'indirizzo: "+err.Error(), http.StatusInternalServerError)
			return
		}

		address, ok := rawResponse.(string)
		if !ok {
			sendError(w, "Risposta del server non valida", http.StatusInternalServerError)
			return
		}

		response := AddressResponse{
			Address: address,
		}

		prettyPrint(w, response)
	}
}

func getAddressesHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")

		resChan := make(chan any)

		s.JobChan <- Job{
			Type:    "GET_ADDRESSES",
			ResChan: resChan,
		}

		rawResponse := <-resChan

		if err, ok := rawResponse.(error); ok {
			sendError(w, "Errore recupero indirizzi: "+err.Error(), http.StatusInternalServerError)
			return
		}

		addresses, ok := rawResponse.([]string)
		if !ok {
			sendError(w, "Risposta del server non valida", http.StatusInternalServerError)
			return
		}

		prettyPrint(w, AddressesResponse{
			Addresses: addresses,
			Count:     len(addresses),
		})
	}
}

func activateMineHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {

		var req struct {
			Address string `json:"address"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil || req.Address == "" {
			sendError(w, "Indirizzo mancante nel body", http.StatusBadRequest)
			return
		}

		resChan := make(chan any)
		s.JobChan <- Job{
			Type:    "ACTIVATE_MINE",
			Data:    req.Address,
			ResChan: resChan,
		}

		rawResponse := <-resChan

		if err, ok := rawResponse.(error); ok {
			sendError(w, "Errore recupero indirizzo: "+err.Error(), http.StatusInternalServerError)
			return
		}

		response, ok := rawResponse.(MineResponse)
		if !ok {
			sendError(w, "Risposta del server non valida", http.StatusInternalServerError)
			return
		}

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

		resChan := make(chan any)

		s.JobChan <- Job{
			Type:    "GET_BALANCE",
			Data:    address,
			ResChan: resChan,
		}

		rawResponse := <-resChan

		if err, ok := rawResponse.(error); ok {
			sendError(w, "Errore recupero bilancio: "+err.Error(), http.StatusInternalServerError)
			return
		}

		response, ok := rawResponse.(BalanceResponse)
		if !ok {
			sendError(w, "Risposta del server non valida", http.StatusInternalServerError)
			return
		}

		w.Header().Set("Content-Type", "application/json")
		prettyPrint(w, response)
	}
}

func getPeersHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {

		resChan := make(chan any)

		s.JobChan <- Job{
			Type:    "GET_PEERS",
			ResChan: resChan,
		}

		rawResponse := <-resChan
		response, ok := rawResponse.(PeersResponse)
		if !ok {
			sendError(w, "Risposta del server non valida", http.StatusInternalServerError)
			return
		}

		w.Header().Set("Content-Type", "application/json")
		prettyPrint(w, response)
	}
}

func getMempoolHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		resChan := make(chan any)

		s.JobChan <- Job{
			Type:    "GET_MEMPOOL",
			ResChan: resChan,
		}

		rawResponse := <-resChan
		response, ok := rawResponse.(MempoolResponse)
		if !ok {
			sendError(w, "Risposta del server non valida", http.StatusInternalServerError)
			return
		}

		w.Header().Set("Content-Type", "application/json")
		prettyPrint(w, response)
	}
}

func sendTxHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		var req TransactionRequest
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, "Invalid Request", http.StatusBadRequest)
			return
		}

		resChan := make(chan any)

		s.JobChan <- Job{
			Type:    "SEND_LOCAL_TX",
			Data:    req,
			ResChan: resChan,
		}

		rawResponse := <-resChan

		if err, ok := rawResponse.(error); ok {
			sendError(w, err.Error(), http.StatusBadRequest)
			return
		}

		response, ok := rawResponse.(TransactionResponse)
		if !ok {
			sendError(w, "Risposta del server non valida", http.StatusInternalServerError)
			return
		}

		w.Header().Set("Content-Type", "application/json")
		prettyPrint(w, response)
	}
}

func printBlockchainHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		resChan := make(chan any)

		s.JobChan <- Job{
			Type:    "PRINT_BLOCKCHAIN",
			ResChan: resChan,
		}

		rawResponse := <-resChan

		if err, ok := rawResponse.(error); ok {
			sendError(w, err.Error(), http.StatusBadRequest)
			return
		}

		response, ok := rawResponse.(BlockchainResponse)
		if !ok {
			sendError(w, "Risposta del server non valida", http.StatusInternalServerError)
			return
		}

		w.Header().Set("Content-Type", "application/json")
		prettyPrint(w, response)
	}
}

// P2P Handler:

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

func getUTXOSetHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		resChan := make(chan any)

		s.JobChan <- Job{
			Type:    "GET_UTXO_SET",
			Data:    nil,
			ResChan: resChan,
		}

		rawResponse := <-resChan

		if err, ok := rawResponse.(error); ok {
			sendError(w, err.Error(), http.StatusInternalServerError)
			return
		}

		response := rawResponse.(UTXOSetResponse)

		w.Header().Set("Content-Type", "application/json")
		prettyPrint(w, response)
	}
}

// Funzioni Helper

func sendError(w http.ResponseWriter, message string, code int) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(code)
	response := map[string]string{
		"status": "error",
		"detail": message,
	}
	prettyPrint(w, response)
}
func prettyPrint(w http.ResponseWriter, v any) {
	encoder := json.NewEncoder(w)
	encoder.SetIndent("", "    ")
	encoder.Encode(v)
}

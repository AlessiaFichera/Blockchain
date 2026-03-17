package network

import (
	"encoding/json"
	"net/http"
	"time"
)

const (
	synchWait = 5
)

// Struttura delle richieste al server
type Job struct {
	Type    string   // Tipo di richiesta
	Data    any      // Dati in ingresso
	ResChan chan any // Canale dove il server invierà il risultato
}

// Richiesta da inoltrare a server
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
	Result  string `json:"result"`
}

type PeersResponse struct {
	Count int      `json:"count"`
	Peers []string `json:"peers"`
}

type MempoolResponse struct {
	Count int      `json:"count"`
	Txs   []string `json:"txs"`
}

type SendTransactionResponse struct {
	Status  string `json:"status"`
	Message string `json:"message"`
	TxID    string `json:"tx_id"`
}

type BlockResponse struct {
	Timestamp    string                `json:"timestamp"`
	PrevHash     string                `json:"prev_hash"`
	Hash         string                `json:"hash"`
	Nonce        int                   `json:"nonce"`
	Transactions []TransactionResponse `json:"transactions"`
	Height       int                   `json:"height"`
}

type BlockchainResponse struct {
	Blocks []BlockResponse `json:"blocks"`
}

type TransactionResponse struct {
	ID   string             `json:"id"`
	Vin  []TXInputResponse  `json:"vin"`
	Vout []TXOutputResponse `json:"vout"`
}

type TXInputResponse struct {
	Txid      string `json:"txid"`
	Vout      int    `json:"vout_index"`
	Signature string `json:"signature"`
	PubKey    string `json:"pubkey"`
}

type TXOutputResponse struct {
	Value      uint64 `json:"value"`
	PubKeyHash string `json:"pubkey_hash"`
}

type UTXOResponse struct {
	TxID       string `json:"tx_id"`
	Index      int    `json:"index"`
	Value      uint64 `json:"value"`
	PubKeyHash string `json:"pub_key_hash"`
}

type UTXOSetResponse struct {
	Count int            `json:"count"`
	UTXOs []UTXOResponse `json:"utxos"`
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

	mux.HandleFunc("/p2p/version", forwardP2PJob[VersionMessage](server, "HANDLE_VERSION"))

	mux.HandleFunc("/p2p/get-blocks", forwardP2PJob[GetBlocksMessage](server, "HANDLE_GET_BLOCKS"))

	mux.HandleFunc("/p2p/blocks", forwardP2PJob[BlocksMessage](server, "HANDLE_BLOCKS"))

	mux.HandleFunc("/p2p/receive-tx", forwardP2PJob[TxMessage](server, "HANDLE_TX"))

	mux.HandleFunc("/p2p/inv", forwardP2PJob[InvMessage](server, "HANDLE_INV"))

	mux.HandleFunc("/p2p/get-data", forwardP2PJob[GetDataMessage](server, "HANDLE_GET_DATA"))

	return mux
}

// API handler:

func healthHandler(w http.ResponseWriter, r *http.Request) {

	response := HealthResponse{
		Status:  "UP",
		Message: "Blockchain node is running smoothly",
	}

	prettyPrint(w, response)
}

func createAddressHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		executeAPIJob[AddressResponse](w, s, "CREATE_ADDRESS", nil)
	}
}

func getAddressesHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		executeAPIJob[AddressesResponse](w, s, "GET_ADDRESSES", nil)
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

		executeAPIJob[MineResponse](w, s, "ACTIVATE_MINE", req.Address)
	}
}

func getBalanceHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		address := r.URL.Query().Get("address")
		if address == "" {
			sendError(w, "Parametro 'address' mancante", http.StatusBadRequest)
			return
		}

		executeAPIJob[BalanceResponse](w, s, "GET_BALANCE", address)
	}
}

func getPeersHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		executeAPIJob[PeersResponse](w, s, "GET_PEERS", nil)
	}
}

func getMempoolHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		executeAPIJob[MempoolResponse](w, s, "GET_MEMPOOL", nil)
	}
}

func sendTxHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		var req TransactionRequest
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, "Invalid Request", http.StatusBadRequest)
			return
		}

		executeAPIJob[SendTransactionResponse](w, s, "SEND_LOCAL_TX", req)
	}
}

func printBlockchainHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		executeAPIJob[BlockchainResponse](w, s, "PRINT_BLOCKCHAIN", nil)
	}
}

func getUTXOSetHandler(s *Server) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		executeAPIJob[UTXOSetResponse](w, s, "GET_UTXO_SET", nil)
	}
}

// P2P Handler:

func forwardP2PJob[T any](s *Server, jobType string) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		var msg T

		if err := json.NewDecoder(r.Body).Decode(&msg); err != nil {
			http.Error(w, "Errore decodifica messaggio P2P", http.StatusBadRequest)
			return
		}

		// Conferma di ricezione
		w.WriteHeader(http.StatusOK)

		// Passiamo il messaggio al canale per essere gestito sequenzialmente
		s.JobChan <- Job{
			Type: jobType,
			Data: msg,
		}
	}
}

// Funzioni Helper

func executeAPIJob[T any](w http.ResponseWriter, s *Server, jobType string, data any) {

	// Buffer a 1 perchè se scade il timeout la goroutine non rimane in attesa della risposta
	resChan := make(chan any, 1)

	job := Job{
		Type:    jobType,
		Data:    data,
		ResChan: resChan,
	}

	select {
	case s.JobChan <- job:
		awaitResponse[T](w, resChan, synchWait*time.Second)

	case <-time.After(1 * time.Second):
		sendError(w, "Server troppo occupato per accettare la richiesta", http.StatusServiceUnavailable)
	}
}

func awaitResponse[T any](w http.ResponseWriter, resChan chan any, timeout time.Duration) {

	timer := time.NewTimer(timeout)
	defer timer.Stop()

	select {
	case rawResponse := <-resChan:
		if !timer.Stop() {
			select {
			case <-timer.C:
			default:
			}
		}

		if err, ok := rawResponse.(error); ok {
			sendError(w, err.Error(), http.StatusInternalServerError)
			return
		}

		response, ok := rawResponse.(T)
		if !ok {
			sendError(w, "Risposta del server non valida", http.StatusInternalServerError)
			return
		}

		prettyPrint(w, response)

	case <-timer.C:
		sendError(w, "Il server ha impiegato troppo tempo a rispondere", http.StatusGatewayTimeout)
	}
}

func sendError(w http.ResponseWriter, message string, code int) {

	w.WriteHeader(code)
	response := map[string]string{
		"status": "error",
		"detail": message,
	}
	prettyPrint(w, response)
}

func prettyPrint(w http.ResponseWriter, v any) {
	w.Header().Set("Content-Type", "application/json")

	encoder := json.NewEncoder(w)
	encoder.SetIndent("", "    ")
	encoder.Encode(v)
}

package network

import (
	"bytes"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"net/http"
	"strconv"
	"time"

	"go_blockchain/core"
)

const (
	centralNode        = "central-node"
	centralNodeAddress = "http://central-node:8080"

	currentVersion       = 1
	thresholdFullMempool = 1
)

// Rappresenta il server che risponde alle richieste
type Server struct {
	Blockchain   *core.Blockchain
	Wallet       *core.Wallet
	JobChan      chan Job
	NodeName     string
	NodeAddress  string
	Peers        map[string]bool
	Mempool      map[string]*core.Transaction
	MinerAddress string
}

type VersionMessage struct {
	Version   int    `json:"version"`
	Height    int    `json:"height"`
	Timestamp int64  `json:"timestamp"`
	AddrFrom  string `json:"addr_from"`
}

type GetBlocksMessage struct {
	StartHeight int    `json:"start_height"`
	AddrFrom    string `json:"addr_from"`
}

type BlocksMessage struct {
	Blocks   []*core.Block `json:"blocks"`
	AddrFrom string        `json:"addr_from"`
}

type TxMessage struct {
	AddrFrom    string
	Transaction core.Transaction
}

type InvMessage struct {
	AddrFrom string // L'indirizzo di chi invia l'INV
	Type     string // "tx" oppure "block"
	ID       []byte // ID (Hash di transazione o di blocco)
}

type GetDataMessage struct {
	AddrFrom string
	Type     string // "tx" o "block"
	ID       []byte // L'ID specifico richiesto
}

// Avvia il server per gestire le richieste che accedono al DB sequenzialmente
func StartServer(walletFile string, nodeName string, nodeAddress string) (*Server, error) {

	wallet, err := core.NewWallet(walletFile)
	if err != nil {
		return nil, err
	}

	server := &Server{
		Wallet:      wallet,
		JobChan:     make(chan Job, 100),
		NodeName:    nodeName,
		NodeAddress: nodeAddress,
		Peers:       make(map[string]bool),
		Mempool:     make(map[string]*core.Transaction),
	}

	go server.runServer()

	return server, nil

}

func (s *Server) Bootstrap(storage core.Storage) error {
	if s.NodeName == centralNode {
		var address string
		var err error

		listAddresses := s.Wallet.GetAddresses()
		if len(listAddresses) > 0 {
			address = listAddresses[0]
		} else {
			address, err = s.Wallet.AddAccount()
			if err != nil {
				return err
			}
			fmt.Printf("[%s] Nuovo account creato: %s\n", s.NodeName, address)
		}

		bc, err := core.NewBlockchainWithGB(address, storage)
		if err != nil {
			return err
		}

		s.Blockchain = bc

	} else {

		bc, err := core.NewBlockchain(storage)
		if err != nil {
			return err
		}

		s.Blockchain = bc

		s.sendVersion(centralNodeAddress)
	}

	return nil
}

func (s *Server) runServer() {
	fmt.Printf("[%s]Server del nodo avviato. In ascolto sul canale...\n", s.NodeName)
	for job := range s.JobChan {
		switch job.Type {
		case "CREATE_ADDRESS":
			s.handleCreateAddress(job.ResChan)

		case "GET_ADDRESSES":
			s.handleGetAddresses(job.ResChan)

		case "ACTIVATE_MINE":
			if address, ok := job.Data.(string); ok {
				s.handleActivateMine(address, job.ResChan)
			}

		case "GET_BALANCE":
			if address, ok := job.Data.(string); ok {
				s.handleGetBalance(address, job.ResChan)
			}

		case "GET_PEERS":
			s.handleGetPeers(job.ResChan)

		case "GET_MEMPOOL":
			s.handleGetMempool(job.ResChan)

		case "SEND_LOCAL_TX":
			if req, ok := job.Data.(TransactionRequest); ok {
				s.handleSendLocalTx(req, job.ResChan)
			}

		case "PRINT_BLOCKCHAIN":
			s.handlePrintChain(job.ResChan)

		case "GET_UTXO_SET":
			s.handleGetUTXOSet(job.ResChan)

		case "HANDLE_VERSION":
			if msg, ok := job.Data.(VersionMessage); ok {
				s.handleVersion(msg)
			} else {
				fmt.Println("Errore: dati HANDLE_VERSION non validi")
			}

		case "HANDLE_GET_BLOCKS":
			if msg, ok := job.Data.(GetBlocksMessage); ok {
				s.handleGetBlocks(msg)
			} else {
				fmt.Println("Errore: dati HANDLE_GET_BLOCKS non validi")
			}

		case "HANDLE_BLOCKS":
			if msg, ok := job.Data.(BlocksMessage); ok {
				s.handleBlocks(msg)
			} else {
				fmt.Println("Errore: dati HANDLE_BLOCKS non validi")
			}

		case "HANDLE_TX":
			if msg, ok := job.Data.(TxMessage); ok {
				s.handleReceiveTx(msg)
			} else {
				fmt.Println("Errore: dati HANDLE_TX non validi")
			}

		case "HANDLE_INV":
			if inv, ok := job.Data.(InvMessage); ok {
				s.handleInv(inv)
			} else {
				fmt.Println("Errore: dati HANDLE_INV non validi")
			}

		case "HANDLE_GET_DATA":
			if req, ok := job.Data.(GetDataMessage); ok {
				s.handleGetData(req)
			} else {
				fmt.Println("Errore: dati HANDLE_GET_DATA non validi")
			}

		}
	}
}

// Funzioni Send

func (s Server) sendVersion(to string) error {
	height, err := s.Blockchain.Storage.GetHeight()
	if err != nil {
		return err
	}

	msg := VersionMessage{
		Version:   currentVersion,
		Height:    height,
		Timestamp: time.Now().Unix(),
		AddrFrom:  s.NodeAddress,
	}

	fmt.Printf("[%s] Invio messaggio VERSION a %s \n", s.NodeName, to)
	payload, _ := json.Marshal(msg)
	s.sendMessage(to+"/p2p/version", payload)
	return nil
}

func (s *Server) sendGetBlocks(to string, startHeight int) {
	msg := GetBlocksMessage{
		StartHeight: startHeight,
		AddrFrom:    s.NodeAddress,
	}

	payload, _ := json.Marshal(msg)

	fmt.Printf("[%s] Invio messaggio GetBlocks a %s \n", s.NodeName, to)
	s.sendMessage(to+"/p2p/get-blocks", payload)
}

func (s *Server) sendBlocks(to string, blocks []*core.Block) {
	payload := BlocksMessage{
		Blocks:   blocks,
		AddrFrom: s.NodeAddress,
	}

	data, _ := json.Marshal(payload)

	fmt.Printf("[%s] Invio messaggio Blocks a %s \n", s.NodeName, to)
	s.sendMessage(to+"/p2p/blocks", data)
}

func (s *Server) sendTx(to string, tx *core.Transaction) {
	msg := TxMessage{
		AddrFrom:    s.NodeAddress,
		Transaction: *tx,
	}
	payload, _ := json.Marshal(msg)

	fmt.Printf("[%s] Invio messaggio TX a %s \n", s.NodeName, to)
	go s.sendMessage(to+"/p2p/receive-tx", payload)
}

func (s *Server) sendInv(toAddress string, kind string, itemID []byte) {
	inventory := InvMessage{
		AddrFrom: s.NodeAddress,
		Type:     kind,
		ID:       itemID,
	}

	payload, _ := json.Marshal(inventory)

	fmt.Printf("[%s] Invio INV (%s) a %s\n", s.NodeName, kind, toAddress)
	go s.sendMessage(toAddress+"/p2p/inv", payload)
}

func (s *Server) sendBroadcastInv(kind string, itemID []byte, returnAddr string) {

	for peerAddr := range s.Peers {
		if peerAddr != returnAddr {
			s.sendInv(peerAddr, kind, itemID)
		}
	}
}

func (s *Server) sendGetData(to string, kind string, id []byte) {
	msg := GetDataMessage{
		AddrFrom: s.NodeAddress,
		Type:     kind,
		ID:       id,
	}
	payload, _ := json.Marshal(msg)

	fmt.Printf("[%s] Invio messaggio GetData a %s \n", s.NodeName, to)
	go s.sendMessage(to+"/p2p/get-data", payload)
}

func (s *Server) sendMessage(url string, payload []byte) {
	go func() {
		client := &http.Client{Timeout: 10 * time.Second}

		resp, err := client.Post(url, "application/json", bytes.NewBuffer(payload))
		if err != nil {
			fmt.Printf("Errore invio a %s: %v\n", url, err)
			return
		}
		defer resp.Body.Close()

		if resp.StatusCode == http.StatusOK {
			fmt.Printf("[%s] Messaggio inviato con successo a %s (Status: 200 OK)\n", s.NodeName, url)
		} else {
			fmt.Printf("[%s] Attenzione: %s ha risposto con Status %d\n", s.NodeName, url, resp.StatusCode)
		}
	}()
}

// Funzioni Handle

// API Handle

func (s *Server) handleCreateAddress(resChan chan any) {
	fmt.Printf("[%s] API Create Address\n", s.NodeName)

	address, err := s.Wallet.AddAccount()
	if err != nil {
		resChan <- fmt.Errorf("errore generazione account: %w", err)
		return
	}

	fmt.Printf("[%s] Address creato: %s\n", s.NodeName, address)

	resChan <- AddressResponse{
		Address: address,
	}
}

func (s *Server) handleGetAddresses(resChan chan any) {
	fmt.Printf("[%s] API Get Addresses\n", s.NodeName)

	addresses := s.Wallet.GetAddresses()
	if addresses == nil {
		addresses = []string{}
	}

	resChan <- AddressesResponse{
		Addresses: addresses,
		Count:     len(addresses),
	}
}

func (s *Server) handleActivateMine(address string, resChan chan any) {
	fmt.Printf("[%s] API Activate Mine\n", s.NodeName)

	// Controllo se era già miner
	if s.MinerAddress != "" {
		resChan <- MineResponse{
			Message: "Operazione annullata: il nodo è già in modalità Miner.",
		}
		return
	}

	_, exists := s.Wallet.Accounts[address]
	if !exists {
		fmt.Printf("[%s] Account %s non trovato nel wallet locale\n", s.NodeName, address)
		resChan <- fmt.Errorf("indirizzo non trovato localmente")
		return
	}

	// Attivazione
	s.MinerAddress = address
	resChan <- MineResponse{
		Message: "Modalità Miner attivata con successo.",
	}

	if s.checkMempoolFull() {
		s.mineBlock()
	}
}

func (s *Server) handleGetBalance(address string, resChan chan any) {
	fmt.Printf("[%s] API Get Balance per %s\n", s.NodeName, address)

	pubKeyHash := core.AddressToPubKeyHash(address)
	balance, err := s.Blockchain.Storage.GetBalanceUTXO(pubKeyHash)
	if err != nil {
		resChan <- fmt.Errorf("Errore nel recupero degli UTXO : %w", err)
		return
	}

	fmt.Printf("[%s] Bilancio per %s: %d coins", s.NodeName, address, balance)

	resChan <- BalanceResponse{
		Address: address,
		Result:  strconv.FormatUint(uint64(balance), 10),
	}

}

func (s *Server) handleGetPeers(resChan chan any) {
	fmt.Printf("[%s] API Get Peers\n", s.NodeName)
	var peerList []string

	for address := range s.Peers {
		peerList = append(peerList, address)
	}

	if peerList == nil {
		peerList = []string{}
	}

	fmt.Printf("[%s] Peer connessi: %d\n", s.NodeName, len(peerList))

	resChan <- PeersResponse{
		Count: len(peerList),
		Peers: peerList,
	}
}

func (s *Server) handleGetMempool(resChan chan any) {
	fmt.Printf("[%s] API Get Mempool\n", s.NodeName)
	var txList []string

	for txID := range s.Mempool {
		txList = append(txList, txID)
	}

	if txList == nil {
		txList = []string{}
	}

	fmt.Printf("[%s] Transazioni in attesa: %d\n", s.NodeName, len(txList))

	resChan <- MempoolResponse{
		Count: len(txList),
		Txs:   txList,
	}
}

func (s *Server) handleSendLocalTx(req TransactionRequest, resChan chan any) {
	fmt.Printf("[%s] Richiesta invio TX da %s verso %s per un ammontare di %d\n", s.NodeName, req.From, req.To, req.Amount)

	account, exists := s.Wallet.Accounts[req.From]
	if !exists {
		fmt.Printf("[%s] Account %s non trovato nel wallet locale\n", s.NodeName, req.From)
		resChan <- fmt.Errorf("indirizzo non trovato localmente")
		return
	}

	tx, err := core.NewTransaction(s.Blockchain, account, req.To, req.Amount)
	if err != nil {
		fmt.Printf("[%s] Fallimento creazione Tx: %v\n", s.NodeName, err)
		resChan <- err
		return
	}

	fmt.Printf("[%s] Transazione creata correttamente (ID: %x). \n", s.NodeName, tx.ID)
	s.addToMempool(tx)

	// Risponde all'utente che la creazione della tx è andata a buon fine
	resChan <- SendTransactionResponse{
		Status:  "success",
		Message: "Transazione validata, firmata e inviata al Central Node",
		TxID:    hex.EncodeToString(tx.ID),
	}

	if s.isMiner() && s.checkMempoolFull() {
		s.mineBlock()
	} else {
		if s.NodeName != centralNode {
			s.sendTx(centralNodeAddress, tx)
		} else {
			// Central Node deve propagare la tx ricevuta a tutti gli altri
			fmt.Printf("[%s] Central Node propaga la transazione agli altri peer...\n", s.NodeName)
			s.sendBroadcastInv("tx", tx.ID, "")
		}
	}
}

func (s *Server) handlePrintChain(resChan chan any) {
	fmt.Printf("[%s] API Print Blockchain\n", s.NodeName)
	blocks, err := s.getBlockchainJSON()

	if err != nil {
		resChan <- fmt.Errorf("Impossibile recuperare i dati della blockchain: %v", err)
		return
	}

	fmt.Printf("[%s] Blockchain estratta con successo (%d blocchi)\n", s.NodeName, len(blocks))

	resChan <- BlockchainResponse{
		Blocks: blocks,
	}
}

func (s *Server) handleGetUTXOSet(resChan chan any) {
	fmt.Printf("[%s] API Print UTXO Set\n", s.NodeName)
	utxos, err := s.Blockchain.Storage.GetUTXOSet()

	if err != nil {
		resChan <- fmt.Errorf(" Impossibile recuperare i dati dell'UTXO set %v\n", err)
		return
	}

	var utxoList []UTXOResponse
	for _, u := range utxos {
		info := UTXOResponse{
			TxID:       hex.EncodeToString(u.TxID),
			Index:      u.Index,
			Value:      u.TXOutput.Value,
			PubKeyHash: hex.EncodeToString(u.TXOutput.PubKeyHash),
		}
		utxoList = append(utxoList, info)
	}

	if utxoList == nil {
		utxoList = []UTXOResponse{}
	}

	resChan <- UTXOSetResponse{
		Count: len(utxoList),
		UTXOs: utxoList,
	}
}

// P2P Handle

func (s *Server) handleVersion(msg VersionMessage) {

	localHeight, err := s.Blockchain.Storage.GetHeight()
	if err != nil {
		fmt.Printf("[%s] Errore recupero altezza locale: %v\n", s.NodeName, err)
		return
	}

	fmt.Printf("[%s] Messaggio VERSION da %s: Peer Height %d | Local Height %d\n", s.NodeName, msg.AddrFrom, msg.Height, localHeight)

	if msg.Height > localHeight {
		fmt.Printf("[%s] Il mio nodo è arretrato. Avvio sincronizzazione da blocco %d...\n", s.NodeName, localHeight+1)
		s.sendGetBlocks(msg.AddrFrom, localHeight+1)

	} else if msg.Height < localHeight {
		fmt.Printf("[%s] Il peer %s è arretrato. Gli notifico la mia versione.\n", s.NodeName, msg.AddrFrom)
		if err := s.sendVersion(msg.AddrFrom); err != nil {
			fmt.Printf("[%s] Errore nel invio di send :%s", s.NodeName, err)
		}

	} else {
		fmt.Printf("[%s] Nodo in pari con %s.\n", s.NodeName, msg.AddrFrom)
	}

	if s.NodeName == centralNode {
		if !s.Peers[msg.AddrFrom] {
			s.Peers[msg.AddrFrom] = true
			fmt.Printf("[%s] Nuovo nodo registrato: %s. Totale peer conosciuti: %d\n", s.NodeName, msg.AddrFrom, len(s.Peers))
		}
	}
}

func (s *Server) handleGetBlocks(msg GetBlocksMessage) {
	fmt.Printf("[%s] Messaggio GET-BLOCKS da %s: richiesta blocchi da altezza: %d\n", s.NodeName, msg.AddrFrom, msg.StartHeight)

	var blocksToSend []*core.Block
	iter := s.Blockchain.Iterator()

	for {
		block, err := iter.Next()
		if err != nil {
			fmt.Printf("[%s] Errore recupero blocchi: %v\n", s.NodeName, err)
			break
		}

		if block == nil || block.Height < msg.StartHeight {
			break
		}

		blocksToSend = append(blocksToSend, block)

	}

	// Inversione ordine
	for i, j := 0, len(blocksToSend)-1; i < j; i, j = i+1, j-1 {
		blocksToSend[i], blocksToSend[j] = blocksToSend[j], blocksToSend[i]
	}

	if len(blocksToSend) > 0 {
		s.sendBlocks(msg.AddrFrom, blocksToSend)
	}
}

func (s *Server) handleBlocks(msg BlocksMessage) {
	if len(msg.Blocks) == 0 {
		fmt.Printf("[%s] Messaggio BLOCKS da %s. Ricevuti 0 blocchi\n", s.NodeName, msg.AddrFrom)
		return
	}

	localHeight, err := s.Blockchain.Storage.GetHeight()
	if err != nil {
		fmt.Printf("[%s] Errore DB in handleBlocks: %v\n", s.NodeName, err)
		return
	}

	fmt.Printf("[%s] Messaggio BLOCKS da %s. Ricevuti %d blocchi\n", s.NodeName, msg.AddrFrom, len(msg.Blocks))

	for _, block := range msg.Blocks {

		// Verifica altezza
		if block.Height != localHeight+1 {
			fmt.Printf("[%s] Blocco %d scartato: altezza non coerente (attesa: %d)\n", s.NodeName, block.Height, localHeight+1)
			break
		}
		// Verifica validità blocco
		pow := core.NewProofOfWork(block)
		if !pow.Validate() {
			break
		}

		// Verifica concatenazione hash
		if !bytes.Equal(block.PrevBlockHash, s.Blockchain.Tip) {
			fmt.Printf("[%s] Blocco %d scartato: Hash precedente non coincide\n", s.NodeName, block.Height)
			break
		}

		// Verifica validità Transazioni
		allTxValid := true
		for _, tx := range block.Transactions {
			valid, err := s.Blockchain.VerifyTransaction(tx)
			if err != nil || !valid {
				allTxValid = false
				break
			}
		}
		if !allTxValid {
			fmt.Printf("[%s] Blocco %d scartato: contiene transazioni non valide\n", s.NodeName, block.Height)
			break
		}
		// Aggiunta Blocco
		err := s.Blockchain.AddBlockToChain(block)
		if err != nil {
			fmt.Printf("[%s] Errore salvataggio blocco %d: %v\n", s.NodeName, block.Height, err)
			break
		}

		// Il blocco aggiunto va tolto dai candidati del miner che lo ha proposto
		if s.isMiner() {
			s.Blockchain.Storage.DeleteCandidateBlock(block.Hash)
		}

		fmt.Printf("[%s] Blocco %d aggiunto correttamente!\n", s.NodeName, block.Height)
		s.cleanMempool(block.Transactions)
		localHeight++

		if s.NodeName == centralNode {
			fmt.Printf("[%s] Central Node propaga il blocco agli altri peer...\n", s.NodeName)
			s.sendBroadcastInv("block", block.Hash, "")
		}
	}

	newHeight, _ := s.Blockchain.Storage.GetHeight()
	fmt.Printf("[%s] Sincronizzazione completata. Nuova altezza: %d\n", s.NodeName, newHeight)

}

func (s *Server) handleReceiveTx(msg TxMessage) {
	txID := hex.EncodeToString(msg.Transaction.ID)
	fmt.Printf("[%s] Messaggio TX ricevuto da %s , transazione ricevuta: %s\n", s.NodeName, msg.AddrFrom, txID)
	if _, exists := s.getResource("tx", msg.Transaction.ID, false); exists {
		return
	}

	valid, err := s.Blockchain.VerifyTransaction(&msg.Transaction)
	if err != nil || !valid {
		fmt.Printf("[%s] Transazione %s scartata: non valida o errore verifica: %v\n", s.NodeName, txID, err)
		return
	}

	s.addToMempool(&msg.Transaction)

	if s.isMiner() && s.checkMempoolFull() {
		s.mineBlock()
	}

	// Central Node deve propagare la tx ricevuta a tutti gli altri
	if s.NodeName == centralNode && !s.isMiner() {
		fmt.Printf("[%s] Central Node propaga la transazione agli altri peer...\n", s.NodeName)
		s.sendBroadcastInv("tx", msg.Transaction.ID, msg.AddrFrom)
	}

}

func (s *Server) handleInv(inv InvMessage) {
	fmt.Printf("[%s] Messaggio INV di tipo %s ricevuto da %s\n", s.NodeName, inv.Type, inv.AddrFrom)

	// Se si riceve Inv non si guarda tra i candidati, poichè significa che il candidato sarà confermato
	if _, exists := s.getResource(inv.Type, inv.ID, false); exists {
		return
	}

	fmt.Printf("[%s] %s %x mancante. Invio GetData a %s\n", s.NodeName, inv.Type, inv.ID, inv.AddrFrom)
	s.sendGetData(inv.AddrFrom, inv.Type, inv.ID)
}

func (s *Server) handleGetData(req GetDataMessage) {
	fmt.Printf("[%s] Messaggio GetData (%s) ricevuto da %s per l'ID: %x\n", s.NodeName, req.Type, req.AddrFrom, req.ID)

	resource, exists := s.getResource(req.Type, req.ID, true)
	if !exists {
		fmt.Printf("[%s] Risorsa %s : %x non trovata\n", s.NodeName, req.Type, req.ID)
		return
	}

	switch v := resource.(type) {
	case *core.Transaction:
		s.sendTx(req.AddrFrom, v)
	case *core.Block:
		s.sendBlocks(req.AddrFrom, []*core.Block{v})
	}
}

// Funzioni Helper

// Restituisce l'oggetto richiesto (Transaction o Block) e un booleano che indica se esiste. Se candidate è true si cerca anche tra i candidati
func (s *Server) getResource(kind string, id []byte, candidate bool) (any, bool) {
	idHex := hex.EncodeToString(id)

	if kind == "tx" {
		// 1. Cerca in Mempool
		if tx, exists := s.Mempool[idHex]; exists {
			return tx, true
		}
		// 2. Cerca in Blockchain
		if tx, err := s.Blockchain.FindTransaction(id); err == nil {
			return &tx, true
		}
	}

	if kind == "block" {
		idHex := hex.EncodeToString(id)
		block, err := s.Blockchain.Storage.GetBlock(id)
		if err == nil && block != nil {
			fmt.Printf("[DEBUG] Blocco %s già presente nel DB principale\n", idHex)
			return block, true
		}

		if s.isMiner() && candidate {
			cand, err := s.Blockchain.Storage.GetCandidateBlock(id)
			if err == nil && cand != nil {
				fmt.Printf("[DEBUG] Blocco %s già presente nei CANDIDATI\n", idHex)
				return cand, true
			}
		}
	}
	return nil, false
}

// Rimuove dalla Mempool le transazioni che sono state incluse in un blocco confermato
func (s *Server) cleanMempool(txs []*core.Transaction) {
	for _, tx := range txs {
		txID := hex.EncodeToString(tx.ID)

		if _, exists := s.Mempool[txID]; exists {
			delete(s.Mempool, txID)
			fmt.Printf("[%s] Transazione %s rimossa dalla Mempool (confermata in blocco)\n", s.NodeName, txID)
		}
	}

	fmt.Printf("[%s] Pulizia completata. Transazioni rimanenti in Mempool: %d\n", s.NodeName, len(s.Mempool))
}

// Avvia il mining
func (s *Server) mineBlock() {
	var txs []*core.Transaction
	for _, tx := range s.Mempool {
		txs = append(txs, tx)
	}

	fmt.Printf("[%s] Mining di un nuovo blocco con %d transazioni...\n", s.NodeName, len(txs))

	newBlock, err := s.Blockchain.MineNewBlock(s.MinerAddress, "", txs)
	if err != nil {
		fmt.Printf("[%s] Errore critico nel mining: %v\n", s.NodeName, err)
		return
	}

	s.cleanMempool(txs[:])

	if s.NodeName == centralNode {
		err = s.Blockchain.AddBlockToChain(newBlock)
		if err != nil {
			fmt.Printf("[%s] Errore critico nel aggiunta alla catena del blocco minato: %v\n", s.NodeName, err)
			return
		}
		s.sendBroadcastInv("block", newBlock.Hash, "")
	} else {
		s.Blockchain.Storage.SaveCandidateBlock(newBlock)
		s.sendInv(centralNodeAddress, "block", newBlock.Hash)
	}
}

// Aggiunge la funzione passata alla mempool
func (s *Server) addToMempool(tx *core.Transaction) {
	txID := hex.EncodeToString(tx.ID)
	s.Mempool[txID] = tx

	fmt.Printf("[%s] Transazione %s aggiunta alla mempool locale (Totale: %d)\n", s.NodeName, txID, len(s.Mempool))
}

// Verifica se la mempool è piena
func (s *Server) checkMempoolFull() bool {
	fmt.Printf("[%s] CheckMempool (%d TX).\n", s.NodeName, len(s.Mempool))
	if !s.isMiner() {
		return false
	}

	if len(s.Mempool) >= thresholdFullMempool {
		fmt.Printf("[%s] Mempool piena (%d TX).\n", s.NodeName, len(s.Mempool))
		return true
	}
	return false

}

// Restituisce la Blockchain in formato JSON
func (s *Server) getBlockchainJSON() ([]BlockResponse, error) {
	var chain []BlockResponse
	it := s.Blockchain.Iterator()

	for {
		block, err := it.Next()
		if err != nil {
			return nil, fmt.Errorf("errore durante il recupero: %w", err)
		}
		if block == nil {
			break
		}

		var txsInfo []TransactionResponse
		for _, tx := range block.Transactions {

			var vins []TXInputResponse
			for _, vin := range tx.Vin {
				vins = append(vins, TXInputResponse{
					Txid:      hex.EncodeToString(vin.Txid),
					Vout:      vin.Vout,
					Signature: hex.EncodeToString(vin.Signature),
					PubKey:    hex.EncodeToString(vin.PubKey),
				})
			}

			var vouts []TXOutputResponse
			for _, vout := range tx.Vout {
				vouts = append(vouts, TXOutputResponse{
					Value:      vout.Value,
					PubKeyHash: hex.EncodeToString(vout.PubKeyHash),
				})
			}

			txsInfo = append(txsInfo, TransactionResponse{
				ID:   hex.EncodeToString(tx.ID),
				Vin:  vins,
				Vout: vouts,
			})
		}

		info := BlockResponse{
			Timestamp:    time.Unix(block.Timestamp, 0).Format("02/01/2006 15:04:05"),
			PrevHash:     hex.EncodeToString(block.PrevBlockHash),
			Hash:         hex.EncodeToString(block.Hash),
			Nonce:        block.Nonce,
			Transactions: txsInfo,
			Height:       block.Height,
		}

		chain = append(chain, info)

		if len(block.PrevBlockHash) == 0 {
			break
		}
	}
	return chain, nil
}

// Verifica se il nodo è un miner
func (s *Server) isMiner() bool {
	return s.MinerAddress != ""
}

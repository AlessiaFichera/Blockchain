package main

import (
	"bytes"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"net/http"
	"time"
)

const (
	currentVersion       = 1
	thresholdFullMempool = 1
)

// Rappresenta il server che risponde alle richieste
type Server struct {
	Blockchain *Blockchain
	JobChan    chan Job
	Address    string
	Peers      map[string]bool
	Mempool    map[string]*Transaction
	isMiner    bool
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
	Blocks   []*Block `json:"blocks"`
	AddrFrom string   `json:"addr_from"`
}

type TxMessage struct {
	AddrFrom    string
	Transaction Transaction
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
func StartServer(storage Storage) (*Server, error) {

	server := &Server{
		JobChan: make(chan Job, 100),
		Address: nodeAddress,
		Peers:   make(map[string]bool),
		Mempool: make(map[string]*Transaction),
	}

	if nodeName == centralNode {

		wallet, err := NewWallet()
		if err != nil {
			return nil, err
		}

		address, err := wallet.AddAccount()
		if err != nil {
			return nil, err
		}

		fmt.Printf("[%s] Nuovo account creato: %s\n", nodeName, address)

		bc, err := NewBlockchainWithGB(address, storage)
		if err != nil {
			return nil, err
		}

		server.Blockchain = bc

	} else {

		bc, err := NewBlockchain(storage)
		if err != nil {
			return nil, err
		}

		server.Blockchain = bc

		server.sendVersion(centralNodeAddress)
	}

	go server.runServer()

	return server, nil

}

func (s *Server) runServer() {
	fmt.Printf("[%s]Server del nodo avviato. In ascolto sul canale...\n", nodeName)
	for job := range s.JobChan {
		switch job.Type {
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

		case "SEND_LOCAL_TX":
			if req, ok := job.Data.(TransactionRequest); ok {
				s.handleSendLocalTx(req, job.ResChan)
			}

		case "HANDLE_TX":
			if msg, ok := job.Data.(TxMessage); ok {
				s.handleReceiveTx(msg)
			}

		case "HANDLE_INV":
			if inv, ok := job.Data.(InvMessage); ok {
				s.handleInv(inv)
			}

		case "HANDLE_GET_DATA":
			if req, ok := job.Data.(GetDataMessage); ok {
				s.handleGetData(req)
			}

		case "ACTIVATE_MINE":
			if resChan, ok := job.Data.(chan MineResponse); ok {
				s.handleActivateMine(resChan)
			}

		case "GET_BALANCE":
			if address, ok := job.Data.(string); ok {
				s.handleGetBalance(address, job.ResChan)
			}
		}
	}
}

// Funzioni Send

func (s Server) sendVersion(to string) error {
	height, err := s.Blockchain.storage.GetHeight()
	if err != nil {
		return err
	}

	msg := VersionMessage{
		Version:   currentVersion,
		Height:    height,
		Timestamp: time.Now().Unix(),
		AddrFrom:  nodeAddress,
	}

	fmt.Printf("[%s] Invio messaggio VERSION a %s \n", nodeName, to)
	payload, _ := json.Marshal(msg)
	sendMessage(to+"/p2p/version", payload)
	return nil
}

func (s *Server) sendGetBlocks(to string, startHeight int) {
	msg := GetBlocksMessage{
		StartHeight: startHeight,
		AddrFrom:    s.Address,
	}

	payload, _ := json.Marshal(msg)

	fmt.Printf("[%s] Invio messaggio GetBlocks a %s \n", nodeName, to)
	sendMessage(to+"/p2p/get-blocks", payload)
}

func (s *Server) sendBlocks(to string, blocks []*Block) {
	payload := BlocksMessage{
		Blocks:   blocks,
		AddrFrom: s.Address,
	}

	data, _ := json.Marshal(payload)

	fmt.Printf("[%s] Invio messaggio Blocks a %s \n", nodeName, to)
	sendMessage(to+"/p2p/blocks", data)
}

func (s *Server) sendTx(to string, tx *Transaction) {
	msg := TxMessage{
		AddrFrom:    s.Address,
		Transaction: *tx,
	}
	payload, _ := json.Marshal(msg)

	fmt.Printf("[%s] Invio messaggio TX a %s \n", nodeName, to)
	go sendMessage(to+"/p2p/receive-tx", payload)
}

func (s *Server) sendInv(toAddress string, kind string, itemID []byte) {
	inventory := InvMessage{
		AddrFrom: s.Address,
		Type:     kind,
		ID:       itemID,
	}

	payload, _ := json.Marshal(inventory)

	fmt.Printf("[%s] Invio INV (%s) a %s\n", nodeName, kind, toAddress)
	go sendMessage(toAddress+"/p2p/inv", payload)
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
		AddrFrom: s.Address,
		Type:     kind,
		ID:       id,
	}
	payload, _ := json.Marshal(msg)

	fmt.Printf("[%s] Invio messaggio GetData a %s \n", nodeName, to)
	go sendMessage(to+"/p2p/get-data", payload)
}

func sendMessage(url string, payload []byte) {
	go func() {
		client := &http.Client{Timeout: 10 * time.Second}

		resp, err := client.Post(url, "application/json", bytes.NewBuffer(payload))
		if err != nil {
			fmt.Printf("Errore invio a %s: %v\n", url, err)
			return
		}
		defer resp.Body.Close()

		if resp.StatusCode == http.StatusOK {
			fmt.Printf("[%s] Messaggio inviato con successo a %s (Status: 200 OK)\n", nodeName, url)
		} else {
			fmt.Printf("[%s] Attenzione: %s ha risposto con Status %d\n", nodeName, url, resp.StatusCode)
		}
	}()
}

// Funzioni Handle

func (s *Server) handleVersion(msg VersionMessage) {

	localHeight, err := s.Blockchain.storage.GetHeight()
	if err != nil {
		fmt.Printf("[%s] Errore recupero altezza locale: %v\n", nodeName, err)
		return
	}

	fmt.Printf("[%s] Messaggio VERSION da %s: Peer Height %d | Local Height %d\n", nodeName, msg.AddrFrom, msg.Height, localHeight)

	if msg.Height > localHeight {
		fmt.Printf("[%s] Il mio nodo è arretrato. Avvio sincronizzazione da blocco %d...\n", nodeName, localHeight+1)
		s.sendGetBlocks(msg.AddrFrom, localHeight+1)

	} else if msg.Height < localHeight {
		fmt.Printf("[%s] Il peer %s è arretrato. Gli notifico la mia versione.\n", nodeName, msg.AddrFrom)
		if err := s.sendVersion(msg.AddrFrom); err != nil {
			fmt.Printf("[%s] Errore nel invio di send :%s", nodeName, err)
		}

	} else {
		fmt.Printf("[%s] Nodo in pari con %s.\n", nodeName, msg.AddrFrom)
	}

	if nodeName == centralNode {
		if !s.Peers[msg.AddrFrom] {
			s.Peers[msg.AddrFrom] = true
			fmt.Printf("[%s] Nuovo nodo registrato: %s. Totale peer conosciuti: %d\n", nodeName, msg.AddrFrom, len(s.Peers))
		}
	}
}

func (s *Server) handleGetBlocks(msg GetBlocksMessage) {
	fmt.Printf("[%s] Messaggio GET-BLOCKS da %s: richiesta blocchi da altezza: %d\n", nodeName, msg.AddrFrom, msg.StartHeight)

	var blocksToSend []*Block
	iter := s.Blockchain.Iterator()

	for {
		block, err := iter.Next()
		if err != nil {
			fmt.Printf("[%s] Errore recupero blocchi: %v\n", nodeName, err)
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
		fmt.Printf("[%s] Messaggio BLOCKS da %s. Ricevuti 0 blocchi\n", nodeName, msg.AddrFrom)
		return
	}

	localHeight, err := s.Blockchain.storage.GetHeight()
	if err != nil {
		fmt.Printf("[%s] Errore DB in handleBlocks: %v\n", nodeName, err)
		return
	}

	fmt.Printf("[%s] Messaggio BLOCKS da %s. Ricevuti %d blocchi\n", nodeName, msg.AddrFrom, len(msg.Blocks))

	for _, block := range msg.Blocks {

		// Verifica altezza
		if block.Height != localHeight+1 {
			fmt.Printf("[%s] Blocco %d scartato: altezza non coerente (attesa: %d)\n", nodeName, block.Height, localHeight+1)
			break
		}
		// Verifica validità blocco
		pow := NewProofOfWork(block)
		if !pow.Validate() {
			break
		}

		// Verifica concatenazione hash
		if !bytes.Equal(block.PrevBlockHash, s.Blockchain.tip) {
			fmt.Printf("[%s] Blocco %d scartato: Hash precedente non coincide\n", nodeName, block.Height)
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
			fmt.Printf("[%s] Blocco %d scartato: contiene transazioni non valide\n", nodeName, block.Height)
			break
		}
		// Aggiunta Blocco
		err := s.Blockchain.AddBlockToChain(block)
		if err != nil {
			fmt.Printf("[%s] Errore salvataggio blocco %d: %v\n", nodeName, block.Height, err)
			break
		}

		// Il blocco aggiunto va tolto dai candidati del miner che lo ha proposto
		if s.isMiner {
			s.Blockchain.storage.DeleteCandidateBlock(block.Hash)
		}

		fmt.Printf("[%s] Blocco %d aggiunto correttamente!\n", nodeName, block.Height)
		s.cleanMempool(block.Transactions)
		localHeight++
	}

	newHeight, _ := s.Blockchain.storage.GetHeight()
	fmt.Printf("[%s] Sincronizzazione completata. Nuova altezza: %d\n", nodeName, newHeight)

}

func (s *Server) handleSendLocalTx(req TransactionRequest, resChan chan interface{}) {
	fmt.Printf("[%s] Richiesta invio TX da %s verso %s per un ammontare di %d\n", nodeName, req.From, req.To, req.Amount)

	wallet, err := NewWallet()
	if err != nil {
		fmt.Printf("[%s] Errore caricamento wallet: %v\n", nodeName, err)
		resChan <- fmt.Errorf("errore caricamento wallet")
		return
	}

	account, exists := wallet.Accounts[req.From]
	if !exists {
		fmt.Printf("[%s] Account %s non trovato nel wallet locale\n", nodeName, req.From)
		resChan <- fmt.Errorf("indirizzo non trovato localmente")
		return
	}

	tx, err := NewTransaction(s.Blockchain, account, req.To, req.Amount)
	if err != nil {
		fmt.Printf("[%s] Fallimento creazione Tx: %v\n", nodeName, err)
		resChan <- err
		return
	}

	fmt.Printf("[%s] Transazione creata correttamente (ID: %x). \n", nodeName, tx.ID)
	s.addToMempool(tx)

	// Risponde all'utente che la creazione della tx è andata a buon fine
	resChan <- tx.ID

	if s.isMiner && s.checkMempoolFull() {
		s.mineBlock()
	}

	if nodeName != centralNode {
		s.sendTx(centralNodeAddress, tx)

	} else {
		// Central Node deve propagare la tx ricevuta a tutti gli altri
		fmt.Printf("[%s] Central Node propaga la transazione agli altri peer...\n", nodeName)
		s.sendBroadcastInv("tx", tx.ID, "")
	}

}

func (s *Server) handleReceiveTx(msg TxMessage) {
	txID := hex.EncodeToString(msg.Transaction.ID)
	fmt.Printf("[%s] Messaggio TX ricevuto da %s , transazione ricevuta: %s\n", nodeName, msg.AddrFrom, txID)
	if _, exists := s.getResource("tx", msg.Transaction.ID); exists {
		return
	}

	valid, err := s.Blockchain.VerifyTransaction(&msg.Transaction)
	if err != nil || !valid {
		fmt.Printf("[%s] Transazione %s scartata: non valida o errore verifica: %v\n", nodeName, txID, err)
		return
	}

	s.addToMempool(&msg.Transaction)

	if s.isMiner && s.checkMempoolFull() {
		s.mineBlock()
	}

	// Central Node deve propagare la tx ricevuta a tutti gli altri
	if nodeName == centralNode && !s.isMiner {
		fmt.Printf("[%s] Central Node propaga la transazione agli altri peer...\n", nodeName)
		s.sendBroadcastInv("tx", msg.Transaction.ID, msg.AddrFrom)
	}

}

func (s *Server) handleInv(inv InvMessage) {
	fmt.Printf("[%s] Messaggio INV di tipo %s ricevuto da %s\n", nodeName, inv.Type, inv.AddrFrom)

	if _, exists := s.getResource(inv.Type, inv.ID); exists {
		return
	}

	fmt.Printf("[%s] %s %x mancante. Invio GetData a %s\n", nodeName, inv.Type, inv.ID, inv.AddrFrom)
	s.sendGetData(inv.AddrFrom, inv.Type, inv.ID)
}

func (s *Server) handleGetData(req GetDataMessage) {
	fmt.Printf("[%s] Messaggio GetData (%s) ricevuto da %s per l'ID: %x\n", nodeName, req.Type, req.AddrFrom, req.ID)

	resource, exists := s.getResource(req.Type, req.ID)
	if !exists {
		fmt.Printf("[%s] Risorsa %s : %x non trovata\n", nodeName, req.Type, req.ID)
		return
	}

	switch v := resource.(type) {
	case *Transaction:
		s.sendTx(req.AddrFrom, v)
	case *Block:
		s.sendBlocks(req.AddrFrom, []*Block{v})
	}
}

func (s *Server) handleActivateMine(resChan chan MineResponse) {
	var resp MineResponse

	// Controllo se era già miner
	if s.isMiner {
		resp.Message = "Operazione annullata: il nodo è già in modalità Miner."
		resChan <- resp
		return
	}

	// Attivazione
	s.isMiner = true
	resp.Message = "Modalità Miner attivata con successo."
	resChan <- resp

	if s.checkMempoolFull() {
		s.mineBlock()
	}
}

func (s *Server) handleGetBalance(address string, resChan chan any) {
	fmt.Printf("[%s] API Get Balance per %s\n", nodeName, address)

	pubKeyHash := AddressToPubKeyHash(address)
	balance, _, err := s.Blockchain.storage.GetUTXO(pubKeyHash, 0)
	if err != nil {
		fmt.Printf("errore durante il recupero degli UTXO: %v", err)
		resChan <- fmt.Errorf("%x", err)
		return
	}

	fmt.Printf("[%s] Bilancio per %s: %d coins", nodeName, address, balance)
	resChan <- balance

}

// Funzioni helper

// Rimuove dalla Mempool le transazioni che sono state incluse in un blocco confermato
func (s *Server) cleanMempool(txs []*Transaction) {
	for _, tx := range txs {
		txID := hex.EncodeToString(tx.ID)

		if _, exists := s.Mempool[txID]; exists {
			delete(s.Mempool, txID)
			fmt.Printf("[%s] Transazione %s rimossa dalla Mempool (confermata in blocco)\n", nodeName, txID)
		}
	}

	fmt.Printf("[%s] Pulizia completata. Transazioni rimanenti in Mempool: %d\n", nodeName, len(s.Mempool))
}

// Restituisce l'oggetto richiesto (Transaction o Block) e un booleano che indica se esiste
func (s *Server) getResource(kind string, id []byte) (interface{}, bool) {
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
		// Cerca in Blockchain
		if block, err := s.Blockchain.storage.GetBlock(id); err != nil {
			fmt.Printf("Errore controllo blocco: %v\n", err)
			return nil, false

		} else if block != nil {
			return block, true
		}

		// Se è un miner cerca nei blocchi candidati
		if s.isMiner {
			if block, err := s.Blockchain.storage.GetCandidateBlock(id); err == nil {
				return block, true
			}
		}
	}
	return nil, false
}

func (s *Server) mineBlock() {
	var txs []*Transaction
	for _, tx := range s.Mempool {
		txs = append(txs, tx)
	}

	fmt.Printf("[%s] Mining di un nuovo blocco con %d transazioni...\n", nodeName, len(txs))

	newBlock, err := s.Blockchain.MineNewBlock(s.Address, "", txs)
	if err != nil {
		fmt.Printf("[%s] Errore critico nel mining: %v\n", nodeName, err)
		return
	}

	s.cleanMempool(txs[:])

	if nodeName == centralNode {
		err = s.Blockchain.AddBlockToChain(newBlock)
		if err != nil {
			fmt.Printf("[%s] Errore critico nel aggiunta alla catena del blocco minato: %v\n", nodeName, err)
			return
		}
		s.sendBroadcastInv("block", newBlock.Hash, "")
	} else {
		s.Blockchain.storage.SaveCandidateBlock(newBlock)
		s.sendInv(centralNodeAddress, "block", newBlock.Hash)
	}
}

func (s *Server) addToMempool(tx *Transaction) {
	txID := hex.EncodeToString(tx.ID)
	s.Mempool[txID] = tx

	fmt.Printf("[%s] Transazione %s aggiunta alla mempool locale (Totale: %d)\n", nodeName, txID, len(s.Mempool))
}

func (s *Server) checkMempoolFull() bool {
	if !s.isMiner {
		return false
	}

	if len(s.Mempool) >= thresholdFullMempool {
		fmt.Printf("[%s] Mempool piena (%d TX).\n", nodeName, len(s.Mempool))
		return true
	}
	return false

}

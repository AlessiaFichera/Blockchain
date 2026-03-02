package main

import (
	"bytes"
	"encoding/json"
	"fmt"
	"net/http"
	"time"
)

const currentVersion = 1

// Rappresenta il server che risponde alle richieste
type Server struct {
	Blockchain *Blockchain
	JobChan    chan Job
	Address    string
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

type GetDataMessage struct {
	Type     string `json:"type"`         // "BLOCK", "TX"
	ID       string `json:"id,omitempty"` // Hash del blocco o ID della transazione
	AddrFrom string `json:"addr_from"`
}

// Avvia il server per gestire le richieste che accedono al DB sequenzialmente
func StartServer(storage Storage) (*Server, error) {

	server := &Server{
		JobChan: make(chan Job, 100),
		Address: nodeAddress,
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
		}
	}
}

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

	payload, _ := json.Marshal(msg)
	sendMessage(to+"/version", payload)
	return nil
}

func (s *Server) sendGetBlocks(to string, startHeight int) {
	msg := GetBlocksMessage{
		StartHeight: startHeight,
		AddrFrom:    s.Address,
	}

	payload, _ := json.Marshal(msg)

	sendMessage(to+"/get-blocks", payload)
}

func (s *Server) sendBlocks(to string, blocks []*Block) {
	payload := BlocksMessage{
		Blocks:   blocks,
		AddrFrom: s.Address,
	}

	data, _ := json.Marshal(payload)
	sendMessage(to+"/blocks", data)
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

	if msg.Blocks[0].Height == localHeight+1 {
		for _, block := range msg.Blocks {
			err := s.Blockchain.storage.SaveBlock(block)
			if err != nil {
				fmt.Printf("[%s] Errore salvataggio blocco %d: %v\n", nodeName, block.Height, err)
				break
			}
			fmt.Printf("[%s] Blocco %d aggiunto correttamente!\n", nodeName, block.Height)
		}

		newHeight, _ := s.Blockchain.storage.GetHeight()
		fmt.Printf("[%s] Sincronizzazione completata. Nuova altezza: %d\n", nodeName, newHeight)
	} else {
		fmt.Printf("[%s] Errore sincronizzazione: atteso blocco %d, ricevuto %d. Scarto il pacchetto.\n", nodeName, localHeight+1, msg.Blocks[0].Height)
	}
}

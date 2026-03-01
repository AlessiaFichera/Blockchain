package main

import (
	"bytes"
	"encoding/hex"
	"fmt"
)

const genesisBlockData = "The Times 03/Jan/2009 Chancellor on brink of second bailout for banks"

// Blockchain: sequenza di blocchi
type Blockchain struct {
	tip     []byte
	storage Storage
}

// Crea un blocco con i dati in input e lo aggiunge alla catena
func (bc *Blockchain) AddBlock(transactions []*Transaction) error {
	newBlock := NewBlock(transactions, bc.tip)
	if err := bc.storage.SaveBlock(newBlock.Hash, newBlock); err != nil {
		return err
	}
	bc.tip = newBlock.Hash
	return nil
}

// Crea una nuova blockchain con un Genesis block
func NewBlockchain(address string, s Storage) (*Blockchain, error) {
	lastHash, err := s.GetLastHash()
	if err != nil {
		return nil, err
	}

	bc := &Blockchain{storage: s}

	if len(lastHash) == 0 {
		fmt.Println("Nessuna blockchain trovata. Generazione Genesis Block ...")
		cbtx := NewCoinbaseTX(address, genesisBlockData)
		genesis := NewGenesisBlock(cbtx)

		if err := s.SaveBlock(genesis.Hash, genesis); err != nil {
			return nil, err
		}
		bc.tip = genesis.Hash

	} else {
		fmt.Printf("Blockchain caricata. Ultimo hash: %x\n", lastHash)
		bc.tip = lastHash
	}

	return bc, nil
}

// Stampa la blockchain
func (bc *Blockchain) PrintBlockchain() error {
	it := bc.Iterator()

	for {
		block, err := it.Next()
		if err != nil {
			return fmt.Errorf("errore durante la stampa: %w", err)
		}

		fmt.Println(block)

		if len(block.PrevBlockHash) == 0 {
			break
		}
	}
	return nil
}

// Restituisce una transazione dato l'ID
func (bc *Blockchain) FindTransaction(ID []byte) (Transaction, error) {
	bci := bc.Iterator()

	for {
		block, err := bci.Next()
		if err != nil {
			return Transaction{}, err
		}

		for _, tx := range block.Transactions {
			if bytes.Equal(tx.ID, ID) {
				return *tx, nil
			}
		}

		if len(block.PrevBlockHash) == 0 {
			break
		}
	}

	return Transaction{}, fmt.Errorf("Transaction not found")
}

// Firma la transazione in input. Verifica che tutte le TxInput siano presenti in blockchain
func (bc *Blockchain) SignTransaction(account *Account, tx *Transaction) error {
	prevTXs := make(map[string]Transaction)

	for _, vin := range tx.Vin {
		prevTX, err := bc.FindTransaction(vin.Txid)
		if err != nil {
			return err
		}
		prevTXs[hex.EncodeToString(prevTX.ID)] = prevTX
	}

	tx.Sign(account, prevTXs)

	return nil
}

package main

import (
	"fmt"
)

// Blockchain: sequenza di blocchi
type Blockchain struct {
	tip     []byte
	storage Storage
}

// Crea un blocco con i dati in input e lo aggiunge alla catena
func (bc *Blockchain) AddBlock(data string) error {
	newBlock := NewBlock(data, bc.tip)
	if err := memorizeBlock(newBlock, bc.storage); err != nil {
		return err
	}
	bc.tip = newBlock.Hash
	return nil
}

// Crea una nuova blockchain con un Genesis block
func NewBlockchain(s Storage) (*Blockchain, error) {
	lastHash, err := s.GetLastHash()
	if err != nil {
		return nil, err
	}

	bc := &Blockchain{storage: s}

	if len(lastHash) == 0 {
		fmt.Println("Nessuna blockchain trovata. Generazione Genesis Block ...")
		genesis := NewGenesisBlock()
		if err := memorizeBlock(genesis, s); err != nil {
			return nil, err
		}
		bc.tip = genesis.Hash

	} else {
		fmt.Printf("Blockchain caricata. Ultimo hash: %x\n", lastHash)
		bc.tip = lastHash
	}

	return bc, nil
}

// Dato un blocco lo memorizza nel DB
func memorizeBlock(b *Block, s Storage) error {
	blockBytes, err := b.Serialize()
	if err != nil {
		return err
	}
	return s.SaveBlock(b.Hash, blockBytes)
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

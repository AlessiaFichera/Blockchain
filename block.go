package main

import (
	"time"
)

// Rappresenta un blocco della catena
type Block struct {
	Timestamp     int64
	Data          []byte
	PrevBlockHash []byte
	Hash          []byte
	Nonce         int
}

// Restituisce un nuovo blocco
func NewBlock(data string, prevBlockHash []byte) *Block {
	block := &Block{
		Timestamp:     time.Now().Unix(),
		Data:          []byte(data),
		PrevBlockHash: prevBlockHash,
	}

	pow := NewProofOfWork(block)
	block.Nonce, block.Hash = pow.Mine()

	return block
}

// Restituisce un GenesisBlock
func NewGenesisBlock() *Block {
	return NewBlock("Genesis Block", []byte{})
}

package main

import (
	"time"
)

// Block contiene gli header dei blocchi
type Block struct {
	Timestamp     int64
	Data          []byte
	PrevBlockHash []byte
	Hash          []byte
	Nonce         int
}

// NewBlock crea e restituisce un blocco
func NewBlock(data string, prevBlockHash []byte) *Block {
	block := &Block{time.Now().Unix(), []byte(data), prevBlockHash, []byte{}, 0}
	pow := NewProofOfWork(block)
	nonce, hash := pow.Run()

	block.Hash = hash[:]
	block.Nonce = nonce

	return block
}

// NewGenesisBlock crea e restituisce un GenesisBlock
func NewGenesisBlock() *Block {
	return NewBlock("Genesis Block", []byte{})
}

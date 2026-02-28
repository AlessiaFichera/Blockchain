package main

import (
	"crypto/sha256"
	"encoding/hex"
	"strconv"
	"strings"
	"time"
)

// Rappresenta un blocco della catena
type Block struct {
	Timestamp     int64
	Transactions  []*Transaction
	PrevBlockHash []byte
	Hash          []byte
	Nonce         int
}

// Restituisce un nuovo blocco
func NewBlock(transactions []*Transaction, prevBlockHash []byte) *Block {
	block := &Block{
		Timestamp:     time.Now().Unix(),
		Transactions:  transactions,
		PrevBlockHash: prevBlockHash,
	}

	pow := NewProofOfWork(block)
	block.Nonce, block.Hash = pow.Mine()

	return block
}

// Restituisce un GenesisBlock
func NewGenesisBlock(coinbase *Transaction) *Block {
	return NewBlock([]*Transaction{coinbase}, []byte{})
}

// Restituisce un hash identificatore delle transazioni in un blocco
func (b *Block) HashTransactions() []byte {
	hasher := sha256.New()

	for _, tx := range b.Transactions {
		hasher.Write(tx.ID)
	}

	return hasher.Sum(nil)
}

// Restituisce il contenuto di un blocco sotto forma di stringa
func (b *Block) String() string {
	var builder strings.Builder

	//builder.Grow()

	builder.WriteString("--- Blocco ---\n")

	// Timestamp
	builder.WriteString("Timestamp:  ")
	builder.WriteString(time.Unix(b.Timestamp, 0).Format("02/01/2006 15:04:05"))
	builder.WriteByte('\n')

	// Hash Precedente
	builder.WriteString("Hash Prec:             ")
	builder.WriteString(hex.EncodeToString(b.PrevBlockHash))
	builder.WriteByte('\n')

	// Hash attuale
	builder.WriteString("Hash:             ")
	builder.WriteString(hex.EncodeToString(b.Hash))
	builder.WriteByte('\n')

	// Nonce
	builder.WriteString("Nonce:            ")
	builder.WriteString(strconv.Itoa(b.Nonce))
	builder.WriteByte('\n')

	// Transazioni
	builder.WriteString("Transactions:   ")
	for i, tx := range b.Transactions {
		builder.WriteString("  [")
		builder.WriteString(strconv.Itoa(i))
		builder.WriteString("] ")
		builder.WriteString(hex.EncodeToString(tx.ID))
		builder.WriteByte('\n')
	}

	return builder.String()
}

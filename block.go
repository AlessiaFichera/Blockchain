package main

import (
	"bytes"
	"encoding/gob"
	"encoding/hex"
	"strconv"
	"strings"
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

// Serializza un blocco per salvarlo nel DB
func (b *Block) Serialize() ([]byte, error) {
	var result bytes.Buffer
	encoder := gob.NewEncoder(&result)
	err := encoder.Encode(b)
	return result.Bytes(), err
}

// Deserializza un blocco letto dal DB
func DeserializeBlock(d []byte) (*Block, error) {
	var block Block
	decoder := gob.NewDecoder(bytes.NewReader(d))
	err := decoder.Decode(&block)
	return &block, err
}

// Restituisce il contenuto di un blocco sotto forma di stringa
func (b *Block) String() string {
	var builder strings.Builder

	//builder.Grow()

	builder.WriteString("--- Blocco ---\n")

	// Timestamp
	builder.WriteString("Timestamp:  ")
	builder.WriteString(time.Unix(b.Timestamp, 0).Format(time.RFC822))
	builder.WriteByte('\n')

	// Dati
	builder.WriteString("Dati:       ")
	builder.Write(b.Data)
	builder.WriteByte('\n')

	// Hash Precedente
	builder.WriteString("Hash Prec:  ")
	builder.WriteString(hex.EncodeToString(b.PrevBlockHash))
	builder.WriteByte('\n')

	// Hash attuale
	builder.WriteString("Hash:       ")
	builder.WriteString(hex.EncodeToString(b.Hash))
	builder.WriteByte('\n')

	// Nonce
	builder.WriteString("Nonce:      ")
	builder.WriteString(strconv.Itoa(b.Nonce))
	builder.WriteByte('\n')

	return builder.String()
}

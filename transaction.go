package main

import (
	"bytes"
	"crypto/rand"
	"crypto/sha256"
	"encoding/gob"
	"strings"
	"time"
)

const (
	maxDataLen  = 100
	randDataLen = 16
)

// Descrittore delle transazioni
type Transaction struct {
	ID   []byte     // Hash del contenuto
	Vin  []TXInput  // Insieme delle transazioni in input
	Vout []TXOutput // Insieme delle transazioni in output
}

// Restituisce la ricompensa per il mining
func NewCoinbaseTX(to, data string) *Transaction {
	if data == "" {
		var builder strings.Builder
		builder.WriteString("Reward to:  ")
		builder.WriteString(to)
		builder.WriteString(" at ")
		builder.WriteString(time.Now().Format("02/01/2006 15:04:05"))
		randData := make([]byte, randDataLen)
		rand.Read(randData)

		builder.Write(randData)

		data = builder.String()
	}

	if len(data) > maxDataLen {
		data = data[:maxDataLen]
	}

	txin := TXInput{Txid: []byte{}, Vout: -1, Signature: nil, PubKey: []byte(data)}
	txout := NewTXOutput(subsidy, to)
	tx := Transaction{ID: nil, Vin: []TXInput{txin}, Vout: []TXOutput{*txout}}
	tx.ID, _ = tx.Hash()

	return &tx
}

// Restituisce l'hash della transazione. Errore se tx è nil
func (tx *Transaction) Hash() ([]byte, error) {
	// Creiamo una copia perchè l'ID non deve fare parte dell'hash
	txCopy := *tx
	txCopy.ID = nil

	data, err := txCopy.serialize()
	if err != nil {
		return nil, err
	}
	hash := sha256.Sum256(data)
	return hash[:], nil
}

// Controlla se la transazione è una coinbase
func (tx Transaction) IsCoinbase() bool {
	return len(tx.Vin) == 1 && len(tx.Vin[0].Txid) == 0 && tx.Vin[0].Vout == -1
}

// Serializza la transazione. Errore se tx è nil
func (tx *Transaction) serialize() ([]byte, error) {
	var result bytes.Buffer
	encoder := gob.NewEncoder(&result)
	err := encoder.Encode(tx)
	return result.Bytes(), err
}

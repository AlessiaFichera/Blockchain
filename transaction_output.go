package main

import (
	"github.com/btcsuite/btcd/btcutil/base58"
)

const subsidy = 10

// Rappresenta una transazione in uscita (TXO)
type TXOutput struct {
	Value      int    // Valore contenuto
	PubKeyHash []byte // Hash della chiave da sbloccare
}

// Crea e restituisce un TXO
func NewTXOutput(value int, address string) *TXOutput {
	txo := &TXOutput{value, nil}

	// Trasforma address in PubKeyHash (se lo utilizziamo da altre parti va reso una funzione )
	payload := base58.Decode(address)
	pubKeyHash := payload[versionLen : len(payload)-addressChecksumLen]
	txo.PubKeyHash = pubKeyHash

	return txo
}

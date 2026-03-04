package main

import (
	"encoding/hex"
	"strconv"
	"strings"
)

const subsidy = 10

// Rappresenta una transazione in uscita (TXO)
type TXOutput struct {
	Value      int    // Valore contenuto
	PubKeyHash []byte // Hash della chiave da sbloccare
}

// Insieme di TXO
type TXOutputs struct {
	Outputs []TXOutput
}

// Crea e restituisce un TXO
func NewTXOutput(value int, address string) *TXOutput {
	txo := &TXOutput{value, nil}
	pubKeyHash := AddressToPubKeyHash(address)
	txo.PubKeyHash = pubKeyHash

	return txo
}

// Restituisce il contenuto di una TXOutput sotto forma di stringa
func (out TXOutput) String() string {
	var builder strings.Builder

	builder.WriteString("      Value:      ")
	builder.WriteString(strconv.Itoa(out.Value))
	builder.WriteByte('\n')

	builder.WriteString("      PubKeyHash: ")
	builder.WriteString(hex.EncodeToString(out.PubKeyHash))
	builder.WriteByte('\n')

	return builder.String()
}

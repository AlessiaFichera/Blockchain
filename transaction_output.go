package main

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

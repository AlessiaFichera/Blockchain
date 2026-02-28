package main

// Rappresenta una transazione in ingresso
type TXInput struct {
	Txid      []byte //ID della transazione cui si riferisce
	Vout      int    // Valore contenuto
	Signature []byte // Firma di validità
	PubKey    []byte // Chiave di verifica
}

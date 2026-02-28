package main

// Rappresenta un UTXO
type UTXO struct {
	TxID     []byte   // ID della transazione che lo contiene
	Index    int      // Indice. (TxID, Indice) = identificatore univoco
	TXOutput TXOutput // Contiene Value e PubKeyHash
}

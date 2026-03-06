package main

import (
	"encoding/hex"
	"strconv"
	"strings"
)

// Rappresenta un UTXO
type UTXO struct {
	TxID     []byte   // ID della transazione che lo contiene
	Index    int      // Indice. (TxID, Indice) = identificatore univoco
	TXOutput TXOutput // Contiene Value e PubKeyHash
}

func (u UTXO) String() string {
	var builder strings.Builder

	builder.WriteString("--- UTXO ---\n")
	builder.WriteString("TXID: ")
	builder.WriteString(hex.EncodeToString(u.TxID))
	builder.WriteString("\n")
	builder.WriteString("Index: ")
	builder.WriteString(strconv.Itoa(u.Index))
	builder.WriteString("\n")
	builder.WriteString("Value: ")
	builder.WriteString(strconv.Itoa(u.TXOutput.Value))
	builder.WriteString("\n")
	builder.WriteString("PubKeyHash: ")
	builder.WriteString(hex.EncodeToString(u.TXOutput.PubKeyHash))

	return builder.String()
}

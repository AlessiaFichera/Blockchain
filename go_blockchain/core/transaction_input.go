package core

import (
	"encoding/hex"
	"strconv"
	"strings"
)

// Rappresenta una transazione in ingresso
type TXInput struct {
	Txid      []byte // ID della transazione cui si riferisce
	Vout      int    // Indice dell'UTXO cui si riferisce
	Signature []byte // Firma di validità
	PubKey    []byte // Chiave di verifica
}

// Restituisce il contenuto di una TXInput sotto forma di stringa
func (in TXInput) String() string {
	var builder strings.Builder

	builder.WriteString("      TXID:       ")
	if len(in.Txid) > 0 {
		builder.WriteString(hex.EncodeToString(in.Txid))
	} else {
		builder.WriteString("<nil>")
	}
	builder.WriteByte('\n')

	builder.WriteString("      Out Index:  ")
	builder.WriteString(strconv.Itoa(in.Vout))
	builder.WriteByte('\n')

	builder.WriteString("      Signature:  ")
	if len(in.Signature) > 0 {
		builder.WriteString(hex.EncodeToString(in.Signature))
	} else {
		builder.WriteString("<nil>")
	}
	builder.WriteByte('\n')

	builder.WriteString("      PubKey:     ")
	if len(in.PubKey) > 0 {
		builder.WriteString(hex.EncodeToString(in.PubKey))
	} else {
		builder.WriteString("<nil>")
	}
	builder.WriteByte('\n')

	return builder.String()
}

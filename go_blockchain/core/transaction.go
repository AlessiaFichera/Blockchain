package core

import (
	"bytes"
	"crypto/ecdsa"
	"crypto/elliptic"
	"crypto/rand"
	"crypto/sha256"
	"encoding/binary"
	"encoding/hex"
	"fmt"
	"math/big"
	"strconv"
	"strings"
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

	if len(data) > maxDataLen {
		data = data[:maxDataLen]
	}

	txin := TXInput{Txid: []byte{}, Vout: -1, Signature: nil, PubKey: []byte(data)}
	txout := NewTXOutput(subsidy, to)
	tx := Transaction{ID: nil, Vin: []TXInput{txin}, Vout: []TXOutput{*txout}}
	tx.ID, _ = tx.hash()

	return &tx
}

// Restituisce l'hash della transazione. Errore se tx è nil
func (tx *Transaction) hash() ([]byte, error) {
	var buf bytes.Buffer

	for _, vin := range tx.Vin {
		buf.Write(vin.Txid)

		vout := int32(vin.Vout)
		binary.Write(&buf, binary.BigEndian, vout)

		buf.Write(vin.PubKey)
	}

	for _, vout := range tx.Vout {
		value := int32(vout.Value)
		binary.Write(&buf, binary.BigEndian, value)

		buf.Write(vout.PubKeyHash)
	}

	hash := sha256.Sum256(buf.Bytes())
	return hash[:], nil
}

// Controlla se la transazione è una coinbase
func (tx Transaction) IsCoinbase() bool {
	return len(tx.Vin) == 1 && len(tx.Vin[0].Txid) == 0 && tx.Vin[0].Vout == -1
}

// Crea una nuova transazione
func NewTransaction(bc *Blockchain, account *Account, to string, amount uint64) (*Transaction, error) {
	var inputs []TXInput
	var outputs []TXOutput

	address := account.GetAddress()
	pubKeyHash := AddressToPubKeyHash(address)

	acc, utxos, err := bc.Storage.GetUTXOForAmount(pubKeyHash, amount)
	if err != nil {
		return nil, err
	}

	if acc < amount {
		return nil, fmt.Errorf("fondi non sufficienti")
	}

	// Costruzione inputs
	for _, utxo := range utxos {
		input := TXInput{Txid: utxo.TxID, Vout: utxo.Index, Signature: nil, PubKey: nil}
		inputs = append(inputs, input)
	}

	// Costruzione outputs
	outputs = append(outputs, *NewTXOutput(amount, to))
	if acc > amount {
		// Gestione resto
		outputs = append(outputs, *NewTXOutput(acc-amount, address))
	}

	tx := Transaction{ID: nil, Vin: inputs, Vout: outputs}
	tx.ID, _ = tx.hash()
	err = bc.SignTransaction(account, &tx)
	if err != nil {
		return nil, fmt.Errorf("errore durante la firma: %w", err)
	}

	return &tx, nil
}

// Firma di una transazione
func (tx *Transaction) Sign(account *Account, prevTXs map[string]Transaction) error {
	if tx.IsCoinbase() {
		return nil
	}

	privKey := assemblePrivateKey(account)

	txCopy := tx.trimmedCopy()

	// Firma di ogni singola transazione: Singature e PubKey di tutte le altre transazioni devono essere nil
	for inID, vin := range txCopy.Vin {
		prevTx := prevTXs[hex.EncodeToString(vin.Txid)]

		txCopy.Vin[inID].PubKey = prevTx.Vout[vin.Vout].PubKeyHash
		hashToSign, _ := txCopy.hash()
		txCopy.Vin[inID].PubKey = nil

		r, s, err := ecdsa.Sign(rand.Reader, privKey, hashToSign)

		if err != nil {
			return err
		}

		signature := make([]byte, 64)

		// Leading Zeroes per evitare troncamenti
		r.FillBytes(signature[0:32])
		s.FillBytes(signature[32:64])

		// Inserimento firma nella transazione reale
		tx.Vin[inID].Signature = signature
		tx.Vin[inID].PubKey = account.PublicKey
	}
	return nil
}

// Verifica la correttezza di una firma
func (tx *Transaction) VerifySignature(prevTXs map[string]Transaction) (bool, error) {
	if tx.IsCoinbase() {
		return true, nil
	}

	txCopy := tx.trimmedCopy()
	curve := elliptic.P256()

	for inID, vin := range tx.Vin {
		if len(vin.Signature) == 0 || len(vin.PubKey) == 0 {
			return false, fmt.Errorf("transazioni in input senza firma  o Chive Pubblica")
		}

		prevTx := prevTXs[hex.EncodeToString(vin.Txid)]
		txCopy.Vin[inID].PubKey = prevTx.Vout[vin.Vout].PubKeyHash
		hashToVerify, _ := txCopy.hash()
		txCopy.Vin[inID].PubKey = nil

		r := big.Int{}
		s := big.Int{}
		r.SetBytes(vin.Signature[:32])
		s.SetBytes(vin.Signature[32:])

		x := big.Int{}
		y := big.Int{}
		x.SetBytes(vin.PubKey[:32])
		y.SetBytes(vin.PubKey[32:])

		rawPubKey := ecdsa.PublicKey{Curve: curve, X: &x, Y: &y}

		if !ecdsa.Verify(&rawPubKey, hashToVerify, &r, &s) {
			return false, fmt.Errorf("verifica di validità della firma fallita")
		}
	}
	fmt.Printf("Transazione %s: Verificata con successo\n", hex.EncodeToString(tx.ID))
	return true, nil
}

// Restituisce il contenuto di una transazione sotto forma di stringa
func (tx Transaction) String() string {
	var builder strings.Builder

	builder.WriteString("--- Transaction ---\n")
	builder.WriteString("ID: ")
	builder.WriteString(hex.EncodeToString(tx.ID))
	builder.WriteString("\n")

	builder.WriteString("Inputs: \n")
	for i, input := range tx.Vin {
		builder.WriteString("    Input ")
		builder.WriteString(strconv.Itoa(i))
		builder.WriteString(":\n")
		builder.WriteString(input.String())
	}

	builder.WriteString("Outputs: \n")
	for i, output := range tx.Vout {
		builder.WriteString("    Output ")
		builder.WriteString(strconv.Itoa(i))
		builder.WriteString(":\n")
		builder.WriteString(output.String())
	}

	return builder.String()
}

// Genera una copia della transazione per la firma
func (tx *Transaction) trimmedCopy() Transaction {
	var inputs []TXInput
	var outputs []TXOutput

	for _, vin := range tx.Vin {
		txidCopy := make([]byte, len(vin.Txid))
		copy(txidCopy, vin.Txid)
		inputs = append(inputs, TXInput{
			Txid:      txidCopy,
			Vout:      vin.Vout,
			Signature: nil,
			PubKey:    nil,
		})
	}

	//outputs = append(outputs, tx.Vout...)
	for _, vout := range tx.Vout {
		pubKeyHashCopy := make([]byte, len(vout.PubKeyHash))
		copy(pubKeyHashCopy, vout.PubKeyHash)
		outputs = append(outputs, TXOutput{vout.Value, pubKeyHashCopy})
	}

	return Transaction{
		ID:   nil,
		Vin:  inputs,
		Vout: outputs,
	}
}

func assemblePrivateKey(account *Account) *ecdsa.PrivateKey {
	curve := elliptic.P256()

	priv := new(ecdsa.PrivateKey)
	priv.PublicKey.Curve = curve
	priv.D = new(big.Int).SetBytes(account.PrivateKey)

	X := new(big.Int).SetBytes(account.PublicKey[:32])
	Y := new(big.Int).SetBytes(account.PublicKey[32:])

	priv.PublicKey.X = X
	priv.PublicKey.Y = Y

	return priv
}

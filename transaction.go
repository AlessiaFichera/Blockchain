package main

import (
	"bytes"
	"crypto/ecdsa"
	"crypto/elliptic"
	"crypto/rand"
	"crypto/sha256"
	"encoding/gob"
	"encoding/hex"
	"fmt"
	"log"
	"math/big"
	"strconv"
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

// Crea una nuova transazione
func NewTransaction(bc *Blockchain, account *Account, to string, amount int) (*Transaction, error) {
	var inputs []TXInput
	var outputs []TXOutput

	address := account.GetAddress()
	pubKeyHash := AddressToPubKeyHash(address)

	acc, utxos, err := bc.storage.GetUTXO(pubKeyHash, amount)
	if err != nil {
		return nil, err
	}

	if acc < amount {
		return nil, fmt.Errorf("Errore: fondi non sufficienti")
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
	tx.ID, _ = tx.Hash()
	err = bc.SignTransaction(account, &tx)
	if err != nil {
		return nil, fmt.Errorf("errore durante la firma: %w", err)
	}

	return &tx, nil
}

// Firma di una transazione
func (tx *Transaction) Sign(account *Account, prevTXs map[string]Transaction) {
	if tx.IsCoinbase() {
		return
	}

	privKey := assemblePrivateKey(account)

	txCopy := tx.trimmedCopy()

	// Firma di ogni singola transazione: Singature e Pubey di tutte le altre transazioni devono essere nil
	for inID, vin := range txCopy.Vin {
		prevTx := prevTXs[hex.EncodeToString(vin.Txid)]

		// PubKeyHash dell'output originale messo nella transazione per firmala
		txCopy.Vin[inID].PubKey = prevTx.Vout[vin.Vout].PubKeyHash

		// Preparazione Hash da firmare
		txCopy.ID, _ = txCopy.Hash()

		// Ripristino del campo a nil
		txCopy.Vin[inID].PubKey = nil

		// Firma dell'ID della transazione
		r, s, err := ecdsa.Sign(rand.Reader, privKey, txCopy.ID)
		if err != nil {
			log.Panic(err)
		}
		signature := append(r.Bytes(), s.Bytes()...)

		// Inserimento firma nella transazione reale
		tx.Vin[inID].Signature = signature
		tx.Vin[inID].PubKey = account.PublicKey
	}
}

// Verifica la correttezza di una firma
func (tx *Transaction) Verify(prevTXs map[string]Transaction) bool {
	if tx.IsCoinbase() {
		return true
	}

	txCopy := tx.trimmedCopy()
	curve := elliptic.P256()

	for inID, vin := range tx.Vin {
		prevTx := prevTXs[hex.EncodeToString(vin.Txid)]

		txCopy.Vin[inID].PubKey = prevTx.Vout[vin.Vout].PubKeyHash
		txCopy.ID, _ = txCopy.Hash()
		txCopy.Vin[inID].PubKey = nil

		r := big.Int{}
		s := big.Int{}
		sigLen := len(vin.Signature)
		r.SetBytes(vin.Signature[:(sigLen / 2)])
		s.SetBytes(vin.Signature[(sigLen / 2):])

		x := big.Int{}
		y := big.Int{}
		keyLen := len(vin.PubKey)
		x.SetBytes(vin.PubKey[:(keyLen / 2)])
		y.SetBytes(vin.PubKey[(keyLen / 2):])

		rawPubKey := ecdsa.PublicKey{Curve: curve, X: &x, Y: &y}

		if !ecdsa.Verify(&rawPubKey, txCopy.ID, &r, &s) {
			return false
		}
	}
	return true
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
		builder.WriteString("     Input ")
		builder.WriteString(strconv.Itoa(i))
		builder.WriteString(":\n")

		builder.WriteString("       TXID:      ")
		builder.WriteString(hex.EncodeToString(input.Txid))
		builder.WriteByte('\n')

		builder.WriteString("       Out Index: ")
		builder.WriteString(strconv.Itoa(input.Vout))
		builder.WriteByte('\n')

		if len(input.Signature) > 0 {
			builder.WriteString("       Signature: ")
			builder.WriteString(hex.EncodeToString(input.Signature))
			builder.WriteByte('\n')
		}

		if len(input.PubKey) > 0 {
			builder.WriteString("       PubKey:    ")
			builder.WriteString(hex.EncodeToString(input.PubKey))
			builder.WriteByte('\n')
		}
	}

	builder.WriteString("Outputs: \n")
	for i, output := range tx.Vout {
		builder.WriteString("     Output ")
		builder.WriteString(strconv.Itoa(i))
		builder.WriteString(":\n")

		builder.WriteString("       Value:      ")
		builder.WriteString(strconv.Itoa(output.Value))
		builder.WriteByte('\n')

		builder.WriteString("       PubKeyHash: ")
		builder.WriteString(hex.EncodeToString(output.PubKeyHash))
		builder.WriteByte('\n')
	}

	return builder.String()
}

// Genera una copia della transazione per la firma
func (tx *Transaction) trimmedCopy() Transaction {
	var inputs []TXInput

	for _, vin := range tx.Vin {
		inputs = append(inputs, TXInput{Txid: vin.Txid, Vout: vin.Vout, Signature: nil, PubKey: nil})
	}

	outputs := make([]TXOutput, len(tx.Vout))
	copy(outputs, tx.Vout)

	return Transaction{tx.ID, inputs, outputs}
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

// Serializza la transazione. Errore se tx è nil
func (tx *Transaction) serialize() ([]byte, error) {
	var result bytes.Buffer
	encoder := gob.NewEncoder(&result)
	err := encoder.Encode(tx)
	return result.Bytes(), err
}

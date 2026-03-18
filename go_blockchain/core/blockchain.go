package core

import (
	"bytes"
	"encoding/hex"
	"fmt"
	"strconv"
	"strings"
)

const genesisBlockData = "The Times 03/Jan/2009 Chancellor on brink of second bailout for banks"

// Blockchain: sequenza di blocchi
type Blockchain struct {
	Tip     []byte
	Storage Storage
}

// Restituisce la blockchain
func NewBlockchain(s Storage) (*Blockchain, error) {
	lastHash, err := s.GetLastHash()
	if err != nil {
		return nil, err
	}

	return &Blockchain{Tip: lastHash, Storage: s}, nil
}

// Restituisce la blockchain, se non trova blocchi crea il Genesis block
func NewBlockchainWithGB(address string, s Storage) (*Blockchain, error) {
	lastHash, err := s.GetLastHash()
	if err != nil {
		return nil, err
	}

	bc := &Blockchain{Storage: s}

	if len(lastHash) == 0 {
		fmt.Printf("Nessuna blockchain trovata. Generazione Genesis Block con l'address: %s\n", address)

		genesis, err := bc.MineNewBlock(address, "", []*Transaction{})
		if err != nil {
			return nil, err
		}
		err = bc.AddBlockToChain(genesis)
		if err != nil {
			return nil, err
		}

	} else {
		fmt.Printf("Blockchain caricata.\n")
		bc.Tip = lastHash
	}

	return bc, nil
}

// Mina un nuovo blocco e lo restituisce
func (bc *Blockchain) MineNewBlock(address string, dataCoinbase string, transactions []*Transaction) (*Block, error) {
	height, err := bc.Storage.GetHeight()
	if err != nil {
		return nil, err
	}

	if dataCoinbase == "" {
		if height == 0 {
			dataCoinbase = genesisBlockData
		} else {
			var builder strings.Builder
			builder.WriteString("Reward for block ")
			builder.WriteString(strconv.Itoa(height + 1))
			dataCoinbase = builder.String()
		}

	}

	cbtx := NewCoinbaseTX(address, dataCoinbase)

	var txs []*Transaction
	txs = append(txs, cbtx)
	txs = append(txs, transactions...)

	newBlock := NewBlock(txs, bc.Tip, height+1)

	return newBlock, nil
}

// Aggiunge un blocco alla catena
func (bc *Blockchain) AddBlockToChain(block *Block) error {
	if err := bc.Storage.SaveBlock(block); err != nil {
		return err
	}

	bc.Tip = block.Hash

	return nil
}

// Restituisce la blockchain come []string
func (bc *Blockchain) GetBlockchain() ([]string, error) {
	var chain []string
	it := bc.Iterator()

	for {
		block, err := it.Next()
		if err != nil {
			return nil, fmt.Errorf("errore durante la stampa: %w", err)
		}
		if block == nil {
			break
		}

		chain = append(chain, block.String())
	}
	return chain, nil
}

// Restituisce una transazione dato l'ID
func (bc *Blockchain) FindTransaction(ID []byte) (Transaction, error) {
	bci := bc.Iterator()

	for {
		block, err := bci.Next()
		if err != nil {
			return Transaction{}, err
		}

		if block == nil {
			break
		}

		for _, tx := range block.Transactions {
			if bytes.Equal(tx.ID, ID) {
				return *tx, nil
			}
		}
	}

	return Transaction{}, fmt.Errorf("Transaction not found")
}

// Firma la transazione in input. Verifica che tutte le TxInput siano presenti in blockchain
func (bc *Blockchain) SignTransaction(account *Account, tx *Transaction) error {
	prevTXs, err := bc.getPrevTransactions(tx)
	if err != nil {
		return err
	}

	return tx.Sign(account, prevTXs)
}

// Verifica la correttezza di una transazione
func (bc *Blockchain) VerifyTransaction(tx *Transaction) (bool, error) {
	if tx.IsCoinbase() {
		return true, nil
	}

	prevTXs, err := bc.getPrevTransactions(tx)
	if err != nil {
		return false, err
	}

	var inputSum uint64
	for _, vin := range tx.Vin {
		prevTx := prevTXs[hex.EncodeToString(vin.Txid)]
		targetOutput := prevTx.Vout[vin.Vout]

		// Verifica la presenza degli UTXO
		exists, err := bc.Storage.CheckUTXO(vin.Txid, vin.Vout)
		if err != nil {
			return false, fmt.Errorf("errore database durante controllo UTXO: %w", err)
		}
		if !exists {
			return false, fmt.Errorf("tentativo di Double Spending: output %x[%d] già speso", vin.Txid, vin.Vout)
		}

		// Verifica che l'UTXO utilizzato sia effettivamente di chi sta provando a spenderlo
		pubKeyHash := HashPubKey(vin.PubKey)
		if !bytes.Equal(pubKeyHash, targetOutput.PubKeyHash) {
			return false, fmt.Errorf("il mittente non è il proprietario dell'output %x[%d]", vin.Txid, vin.Vout)
		}

		inputSum += targetOutput.Value
	}

	var outputSum uint64
	for _, vout := range tx.Vout {
		outputSum += vout.Value
	}

	// Verifica bilancio (inputSum >= outputSum)
	if inputSum < outputSum {
		return false, fmt.Errorf("fondi insufficienti: input (%d) < output (%d)", inputSum, outputSum)
	}

	// Verifica firma
	return tx.VerifySignature(prevTXs)
}

// Recupera le transazioni precedenti a cui una nuova transazione si riferisce
func (bc *Blockchain) getPrevTransactions(tx *Transaction) (map[string]Transaction, error) {
	prevTXs := make(map[string]Transaction)

	for _, vin := range tx.Vin {
		prevTX, err := bc.FindTransaction(vin.Txid)
		if err != nil {
			return nil, err
		}
		prevTXs[hex.EncodeToString(prevTX.ID)] = prevTX
	}

	return prevTXs, nil
}

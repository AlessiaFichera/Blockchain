package main

import (
	"bytes"
	"encoding/hex"
	"fmt"
)

const genesisBlockData = "The Times 03/Jan/2009 Chancellor on brink of second bailout for banks"

// Blockchain: sequenza di blocchi
type Blockchain struct {
	tip     []byte
	storage Storage
}

// Restituisce la blockchain
func NewBlockchain(s Storage) (*Blockchain, error) {
	lastHash, err := s.GetLastHash()
	if err != nil {
		return nil, err
	}

	return &Blockchain{tip: lastHash, storage: s}, nil
}

// Restituisce la blockchain, se non trova blocchi crea il Genesis block
func NewBlockchainWithGB(address string, s Storage) (*Blockchain, error) {
	lastHash, err := s.GetLastHash()
	if err != nil {
		return nil, err
	}

	bc := &Blockchain{storage: s}

	if len(lastHash) == 0 {
		fmt.Println("Nessuna blockchain trovata. Generazione Genesis Block ...")

		genesis, err := bc.MineNewBlock(address, "", []*Transaction{})
		if err != nil {
			return nil, err
		}
		err = bc.AddBlockToChain(genesis)
		if err != nil {
			return nil, err
		}

	} else {
		fmt.Printf("Blockchain caricata. Ultimo hash: %x\n", lastHash)
		bc.tip = lastHash
	}

	return bc, nil
}

// Mina un nuovo blocco e lo restituisce
func (bc *Blockchain) MineNewBlock(address string, dataCoinbase string, transactions []*Transaction) (*Block, error) {
	height, err := bc.storage.GetHeight()
	if err != nil {
		return nil, err
	}

	if dataCoinbase == "" {
		if height == 0 {
			dataCoinbase = genesisBlockData
		} else {
			dataCoinbase = "Reward per il blocco"
		}

	}

	cbtx := NewCoinbaseTX(address, dataCoinbase)

	var txs []*Transaction
	txs = append(txs, cbtx)
	txs = append(txs, transactions...)

	newBlock := NewBlock(txs, bc.tip, height+1)

	return newBlock, nil
}

// Aggiunge un blocco alla catena
func (bc *Blockchain) AddBlockToChain(block *Block) error {
	if err := bc.storage.SaveBlock(block); err != nil {
		return err
	}

	bc.tip = block.Hash

	return nil
}

// Stampa la blockchain
func (bc *Blockchain) PrintBlockchain() error {
	it := bc.Iterator()

	for {
		block, err := it.Next()
		if err != nil {
			return fmt.Errorf("errore durante la stampa: %w", err)
		}

		fmt.Println(block)

		if len(block.PrevBlockHash) == 0 {
			break
		}
	}
	return nil
}

// Restituisce una transazione dato l'ID
func (bc *Blockchain) FindTransaction(ID []byte) (Transaction, error) {
	bci := bc.Iterator()

	for {
		block, err := bci.Next()
		if err != nil {
			return Transaction{}, err
		}

		for _, tx := range block.Transactions {
			if bytes.Equal(tx.ID, ID) {
				return *tx, nil
			}
		}

		if len(block.PrevBlockHash) == 0 {
			break
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

	tx.Sign(account, prevTXs)
	return nil
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

	var inputSum int
	for _, vin := range tx.Vin {
		prevTx := prevTXs[hex.EncodeToString(vin.Txid)]
		targetOutput := prevTx.Vout[vin.Vout]

		// Verifica la presenza degli UTXO
		exists, err := bc.storage.CheckUTXO(vin.Txid, vin.Vout)
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

	var outputSum int
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

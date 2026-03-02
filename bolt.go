package main

import (
	"bytes"
	"encoding/binary"
	"encoding/gob"
	"fmt"

	"go.etcd.io/bbolt"
)

var (
	blocksBucket   = []byte("blocks")   // bucket: hash del blocco -> blocco
	utxoBucket     = []byte("utxo")     // bucket: hash della transazione + indice UTXO -> UTXO della transazione
	metadataBucket = []byte("metadata") // bucket per info varie
	lastHashKey    = []byte("lastHash") // chiave per lastHash in metadataBucket
	heightKey      = []byte("height")   // chiave per lastHeight in metadataBucket
)

// Implementa l'interfaccia Storage per BoltDB
type BoltStorage struct {
	db *bbolt.DB
}

// Restituisce un nuovo DB
func NewBoltStorage(dbPath string) (*BoltStorage, error) {

	// Open apre il file dbPath.db. Se non esiste lo crea in R/W
	db, err := bbolt.Open(dbPath, 0600, nil)
	if err != nil {
		return nil, err
	}

	// Update avvia una transazione
	err = db.Update(func(tx *bbolt.Tx) error {

		// Crea i bucket iniziali se non esistono
		_, err := tx.CreateBucketIfNotExists(blocksBucket)
		if err != nil {
			return err
		}
		_, err = tx.CreateBucketIfNotExists(utxoBucket)
		if err != nil {
			return err
		}
		_, err = tx.CreateBucketIfNotExists(metadataBucket)
		return err
	})

	return &BoltStorage{db: db}, err
}

// Salva l'ultimo blocco nel DB
func (s *BoltStorage) SaveBlock(block *Block) error {
	transactions := block.Transactions
	hash := block.Hash
	height := make([]byte, 4)
	binary.BigEndian.PutUint32(height, uint32(block.Height))

	blockBytes, err := serialize(block)
	if err != nil {
		return err
	}

	return s.db.Update(func(tx *bbolt.Tx) error {
		b := tx.Bucket(blocksBucket)
		err := b.Put(hash, blockBytes)
		if err != nil {
			return err
		}

		b = tx.Bucket(metadataBucket)
		err = b.Put(lastHashKey, hash)
		if err != nil {
			return err
		}
		err = b.Put(heightKey, height)
		if err != nil {
			return err
		}

		return s.updateUTXOBucket(tx, transactions)
	})
}

// Restituisce un blocco dato l'hash
func (s *BoltStorage) GetBlock(hash []byte) (*Block, error) {
	var val []byte
	// View entra in sola lettura
	err := s.db.View(func(tx *bbolt.Tx) error {
		b := tx.Bucket(blocksBucket)
		val = b.Get(hash)
		return nil
	})
	if err != nil || val == nil {
		return &Block{}, err
	}

	return deserialize[*Block](val)
}

// Restituisce l'hash dell'ultimo blocco
func (s *BoltStorage) GetLastHash() ([]byte, error) {
	var hash []byte
	err := s.db.View(func(tx *bbolt.Tx) error {
		b := tx.Bucket(metadataBucket)
		hash = b.Get(lastHashKey)
		return nil
	})
	return hash, err
}

// Restituisce l'height dell'ultimo blocco
func (s *BoltStorage) GetHeight() (int, error) {
	var intHeight int
	err := s.db.View(func(tx *bbolt.Tx) error {
		b := tx.Bucket(metadataBucket)
		height := b.Get(heightKey)

		if len(height) == 0 {
			intHeight = 0
			return nil
		}

		intHeight = int(binary.BigEndian.Uint32(height))
		return nil
	})

	return intHeight, err
}

/*
Restituisce gli UTXO di un PubKeyHash
Se amount > 0, si ferma appena raggiunge la soglia.
Se amount <= 0, restituisce tutti gli UTXO dell'indirizzo.
*/
func (s *BoltStorage) GetUTXO(pubKeyHash []byte, amount int) (int, []UTXO, error) {
	var unspentOutputs []UTXO
	accumulated := 0

	err := s.db.View(func(tx *bbolt.Tx) error {
		b := tx.Bucket(utxoBucket)
		c := b.Cursor()

		for k, v := c.First(); k != nil; k, v = c.Next() {
			out, _ := deserialize[TXOutput](v)

			if bytes.Equal(out.PubKeyHash, pubKeyHash) {

				txID := make([]byte, 32)
				copy(txID, k[:32])
				index := int(binary.BigEndian.Uint32(k[32:]))

				utxo := UTXO{TxID: txID, Index: index, TXOutput: out}
				unspentOutputs = append(unspentOutputs, utxo)

				accumulated += out.Value

				if amount > 0 && accumulated >= amount {
					return nil
				}
			}
		}
		return nil
	})
	return accumulated, unspentOutputs, err
}

// Aggiorna utxobucket dopo l'aggiunta di un nuovo blocco nella blockchain
func (s *BoltStorage) updateUTXOBucket(boltTx *bbolt.Tx, transactions []*Transaction) error {
	b := boltTx.Bucket([]byte(utxoBucket))

	for _, tx := range transactions {

		// Rimozione UTXO spesi
		if !tx.IsCoinbase() {
			for _, vin := range tx.Vin {
				key := constructUTXOKey(vin.Txid, vin.Vout)
				if err := b.Delete(key); err != nil {
					return err
				}
			}
		}

		// Aggiunta nuovi UTXO
		for outIdx, out := range tx.Vout {
			key := constructUTXOKey(tx.ID, outIdx)

			// Usiamo la tua funzione generica Serialize[TXOutput]
			data, err := serialize(out)
			if err != nil {
				return err
			}
			if err := b.Put(key, data); err != nil {
				return err
			}
		}
	}
	return nil
}

// Costruisce la chiave del utxobucket dati i suoi elementi
func constructUTXOKey(txID []byte, index int) []byte {
	idxBytes := make([]byte, 4)
	binary.BigEndian.PutUint32(idxBytes, uint32(index))
	return append(txID, idxBytes...)
}

// Trasforma qualsiasi dato in un array di byte, per poterlo salvare nel DB
func serialize[T any](data T) ([]byte, error) {
	var result bytes.Buffer
	encoder := gob.NewEncoder(&result)

	err := encoder.Encode(data)
	if err != nil {
		return nil, err
	}

	return result.Bytes(), nil
}

// Trasforma un array di byte nel tipo specificato
func deserialize[T any](d []byte) (T, error) {
	var target T

	if len(d) == 0 {
		return target, fmt.Errorf("dati vuoti")
	}

	decoder := gob.NewDecoder(bytes.NewReader(d))
	err := decoder.Decode(&target)
	if err != nil {
		return target, err
	}

	return target, nil
}

// Chiude il DB
func (s *BoltStorage) Close() error {
	return s.db.Close()
}

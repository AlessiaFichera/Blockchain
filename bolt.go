package main

import (
	"go.etcd.io/bbolt"
)

var (
	blocksBucket   = []byte("blocks")
	metadataBucket = []byte("metadata")
	lastHashKey    = []byte("last")
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
		_, err = tx.CreateBucketIfNotExists(metadataBucket)
		return err
	})

	return &BoltStorage{db: db}, err
}

// Salva l'ultimo blocco nel DB
func (s *BoltStorage) SaveBlock(hash []byte, blockBytes []byte) error {
	return s.db.Update(func(tx *bbolt.Tx) error {
		b := tx.Bucket(blocksBucket)
		err := b.Put(hash, blockBytes)
		if err != nil {
			return err
		}
		b = tx.Bucket(metadataBucket)
		return b.Put(lastHashKey, hash)
	})
}

// Restituisce un blocco dato l'hash
func (s *BoltStorage) GetBlock(hash []byte) ([]byte, error) {
	var val []byte
	// View entra in sola lettura
	err := s.db.View(func(tx *bbolt.Tx) error {
		b := tx.Bucket(blocksBucket)
		val = b.Get(hash)
		return nil
	})
	return val, err
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

// Chiude il DB
func (s *BoltStorage) Close() error {
	return s.db.Close()
}

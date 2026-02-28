package main

type Storage interface {
	SaveBlock(hash []byte, block *Block) error
	GetBlock(hash []byte) (*Block, error)
	GetLastHash() ([]byte, error)
	GetUTXO(pubKeyHash []byte) ([]UTXO, error)
	Close() error
}

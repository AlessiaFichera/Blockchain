package main

type Storage interface {
	SaveBlock(block *Block) error
	GetBlock(hash []byte) (*Block, error)
	GetLastHash() ([]byte, error)
	GetHeight() (int, error)
	GetUTXO(pubKeyHash []byte, amount int) (int, []UTXO, error)
	Close() error
}

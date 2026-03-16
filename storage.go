package main

type Storage interface {
	SaveBlock(block *Block) error
	GetBlock(hash []byte) (*Block, error)
	SaveCandidateBlock(block *Block) error
	GetCandidateBlock(hash []byte) (*Block, error)
	DeleteCandidateBlock(hash []byte) error
	GetLastHash() ([]byte, error)
	GetHeight() (int, error)
	GetBalanceUTXO(pubKeyHash []byte) (int, error)
	GetUTXOForAmount(pubKeyHash []byte, amount int) (int, []UTXO, error)
	GetUTXOSet() ([]UTXO, error)
	CheckUTXO(txID []byte, index int) (bool, error)
	Close() error
}

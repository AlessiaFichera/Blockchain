package main

type Storage interface {
	SaveBlock(hash []byte, data []byte) error
	GetBlock(hash []byte) ([]byte, error)
	GetLastHash() ([]byte, error)
	Close() error
}

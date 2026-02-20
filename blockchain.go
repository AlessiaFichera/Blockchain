package main

// Blockchain: sequenza di blocchi
type Blockchain struct {
	blocks []*Block
}

// AddBlock salva i dati forniti come un blocco nella blockchain
func (bc *Blockchain) AddBlock(data string) {
	prevBlock := bc.blocks[len(bc.blocks)-1]
	newBlock := NewBlock(data, prevBlock.Hash)
	bc.blocks = append(bc.blocks, newBlock)
}

// NewBlockchain crea una nuova blockchain con un genesis block
func NewBlockchain() *Blockchain {
	return &Blockchain{[]*Block{NewGenesisBlock()}}
}

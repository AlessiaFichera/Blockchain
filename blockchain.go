package main

// Blockchain: sequenza di blocchi
type Blockchain struct {
	blocks []*Block
}

// Crea un blocco con i dati in input e lo aggiunge alla catena
func (bc *Blockchain) AddBlock(data string) {
	prevBlock := bc.blocks[len(bc.blocks)-1]
	newBlock := NewBlock(data, prevBlock.Hash)
	bc.blocks = append(bc.blocks, newBlock)
}

// Crea una nuova blockchain con un Genesis block
func NewBlockchain() *Blockchain {
	return &Blockchain{[]*Block{NewGenesisBlock()}}
}

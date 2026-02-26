package main

type BlockchainIterator struct {
	currentHash []byte
	storage     Storage
}

// Crea un nuovo iteratore partendo dal tip della blockchain
func (bc *Blockchain) Iterator() *BlockchainIterator {
	return &BlockchainIterator{bc.tip, bc.storage}
}

// Dato un blocco restituisce il precedente nella catena
func (i *BlockchainIterator) Next() (*Block, error) {
	blockBytes, err := i.storage.GetBlock(i.currentHash)
	if err != nil {
		return nil, err
	}

	block, err := DeserializeBlock(blockBytes)
	if err != nil {
		return nil, err
	}

	i.currentHash = block.PrevBlockHash

	return block, nil
}

package core

type BlockchainIterator struct {
	currentHash []byte
	storage     Storage
}

// Crea un nuovo iteratore partendo dal tip della blockchain
func (bc *Blockchain) Iterator() *BlockchainIterator {
	return &BlockchainIterator{bc.Tip, bc.Storage}
}

// Restituisce il blocco precedente nella catena
func (i *BlockchainIterator) Next() (*Block, error) {

	if len(i.currentHash) == 0 {
		return nil, nil
	}

	block, err := i.storage.GetBlock(i.currentHash)
	if err != nil || block == nil {
		return nil, err
	}

	i.currentHash = block.PrevBlockHash

	return block, nil
}

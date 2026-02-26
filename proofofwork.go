package main

import (
	"bytes"
	"crypto/sha256"
	"encoding/binary"
	"fmt"
	"math"
)

// Numero di bit più significativi che l'hash deve avere a 0
const difficultyBits = 10

var maxNonce = math.MaxInt64

// Rappresenta il PoW di un blocco
type ProofOfWork struct {
	block  *Block   // Blocco a cui appartiene il PoW
	target [32]byte // target, e quindi difficoltà, con il quale il PoW è stato calcolato
}

// Restituisce il PoW per un dato Blocco
func NewProofOfWork(b *Block) *ProofOfWork {
	var target [32]byte

	byteIdx := difficultyBits / 8
	bitIdx := uint(difficultyBits % 8)

	// Settiamo i primi difficultyBits a 0 e i successivi a 1
	target[byteIdx] = byte((1 << (8 - bitIdx)) - 1)

	// Tutti i bit successivi devono essere a 1 (byte a 255)
	for i := byteIdx + 1; i < 32; i++ {
		target[i] = 255
	}

	return &ProofOfWork{b, target}
}

// Dato il contenuto di un blocco e il nonce restituisce un unico []byte
func (pow *ProofOfWork) prepareData(buf *bytes.Buffer, nonce int) []byte {
	buf.Reset()

	buf.Write(pow.block.PrevBlockHash)
	buf.Write(pow.block.Data)

	//binary.Write scrive nel buffer degli int64 come bytes
	binary.Write(buf, binary.BigEndian, pow.block.Timestamp)
	binary.Write(buf, binary.BigEndian, int64(difficultyBits))
	binary.Write(buf, binary.BigEndian, int64(nonce))

	return buf.Bytes()
}

// Mining di un blocco
func (pow *ProofOfWork) Mine() (int, []byte) {
	var hash [32]byte
	var buf bytes.Buffer
	// buf.Grow() //potrei preallocare la memoria ma ancora non so quanta memoria occuperanno le transazioni
	nonce := 0

	fmt.Printf("Mining the block containing \"%s\"\n", pow.block.Data)
	for nonce < maxNonce {
		data := pow.prepareData(&buf, nonce)

		hash = sha256.Sum256(data)
		if bytes.Compare(hash[:], pow.target[:]) == -1 {
			break
		} else {
			nonce++
		}
	}
	fmt.Printf("\r%x\n\n", hash)

	return nonce, hash[:]
}

// Verifica la validità di un blocco
func (pow *ProofOfWork) Validate() bool {
	var buf bytes.Buffer

	data := pow.prepareData(&buf, pow.block.Nonce)
	hash := sha256.Sum256(data)

	isValid := bytes.Compare(hash[:], pow.target[:]) == -1

	return isValid
}

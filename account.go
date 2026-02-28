package main

import (
	"bytes"
	"crypto/ecdsa"
	"crypto/elliptic"
	"crypto/rand"
	"crypto/sha256"

	"github.com/btcsuite/btcd/btcutil/base58"
	"golang.org/x/crypto/ripemd160"
)

const (
	version            = byte(0x00)
	versionLen         = 1
	addressChecksumLen = 4
	addressLen         = 25 // 1 byte versione + 20 byte PubKeyHash + 4 byte checksum
)

// Contiene una coppia di chiavi pubblica-privata
type Account struct {
	PrivateKey []byte
	PublicKey  []byte
}

// Crea e restituisce un account
func NewAccount() (*Account, error) {
	curve := elliptic.P256()
	privateECDSA, err := ecdsa.GenerateKey(curve, rand.Reader)
	if err != nil {
		return nil, err
	}
	public := append(privateECDSA.PublicKey.X.Bytes(), privateECDSA.PublicKey.Y.Bytes()...)
	private := privateECDSA.D.Bytes()

	return &Account{private, public}, nil
}

// Restituisce l'Address di un Account: Base58(version | PubKeyHash | checksum)
func (account Account) GetAddress() string {
	pubKeyHash := hashPubKey(account.PublicKey)

	payload := make([]byte, 0, addressLen)

	payload = append(payload, version)
	payload = append(payload, pubKeyHash...)

	checksum := checksum(payload)
	payload = append(payload, checksum...)

	return base58.Encode(payload)
}

// ValidateAddress check if address if valid
func ValidateAddress(address string) bool {
	payload := base58.Decode(address)

	// Controllo preliminare sulla lunghezza
	if len(payload) != addressLen {
		return false
	}

	splitIndex := len(payload) - addressChecksumLen
	content := payload[:splitIndex]        // [versione + pubKeyHash]
	actualChecksum := payload[splitIndex:] // [checksum]

	return bytes.Equal(actualChecksum, checksum(content))
}

// Converte l'address leggibile in PubKeyHash
func AddressToPubKeyHash(address string) []byte {
	payload := base58.Decode(address)
	return payload[versionLen : len(payload)-addressChecksumLen]
}

// Restituisce l'hash della chiave pubblica: RIPEMD160(SHA256(PubKey))
func hashPubKey(pubKey []byte) []byte {
	hashSHA256 := sha256.Sum256(pubKey)

	hasher := ripemd160.New()
	hasher.Write(hashSHA256[:])

	return hasher.Sum(nil)
}

// Genera il checksum dell'address: SHA256(SHA256(version | PubKeyHash))
func checksum(payload []byte) []byte {
	hash := sha256.Sum256(payload)
	hash = sha256.Sum256(hash[:])

	return hash[:addressChecksumLen]
}

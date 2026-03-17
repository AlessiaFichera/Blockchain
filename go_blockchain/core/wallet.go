package core

import (
	"bytes"
	"crypto/elliptic"
	"encoding/gob"
	"os"
)

// Gestisce l'insieme di account
type Wallet struct {
	Accounts map[string]*Account
	filePath string
}

// Crea un wallet e lo restituisce
func NewWallet(path string) (*Wallet, error) {
	wallet := Wallet{
		Accounts: make(map[string]*Account),
		filePath: path,
	}
	err := wallet.loadFromFile()
	return &wallet, err
}

// Aggiunge un nuovo account al wallet
func (wallet *Wallet) AddAccount() (string, error) {
	account, err := NewAccount()
	if err != nil {
		return "", err
	}
	address := account.GetAddress()

	wallet.Accounts[address] = account
	err = wallet.saveToFile()
	return address, err
}

// Restituisce tutti gli address presenti nel wallet
func (wallet *Wallet) GetAddresses() []string {
	addresses := make([]string, 0, len(wallet.Accounts))

	for address := range wallet.Accounts {
		addresses = append(addresses, address)
	}

	return addresses
}

// Carica le coppie di chiavi presenti nel file sul wallet
func (wallet *Wallet) loadFromFile() error {
	if _, err := os.Stat(wallet.filePath); os.IsNotExist(err) {
		return nil
	}

	fileContent, err := os.ReadFile(wallet.filePath)
	if err != nil {
		return err
	}

	gob.Register(elliptic.P256())
	decoder := gob.NewDecoder(bytes.NewReader(fileContent))
	err = decoder.Decode(wallet)
	if err != nil {
		return err
	}

	return nil
}

// Salva il contenuto di wallet su file
func (wallet Wallet) saveToFile() error {
	var content bytes.Buffer

	gob.Register(elliptic.P256())

	encoder := gob.NewEncoder(&content)
	err := encoder.Encode(wallet)
	if err != nil {
		return err
	}

	err = os.WriteFile(wallet.filePath, content.Bytes(), 0600)
	if err != nil {
		return err
	}

	return nil
}

package main

import (
	"log"

	"github.com/diagridio/catalyst-samples/go/config"
	"github.com/diagridio/catalyst-samples/go/internal/app"
)

func main() {
	// Configuration
	cfg, err := config.NewConfig(".")
	if err != nil {
		log.Fatalf("config error: %s", err)
	}

	// Run
	app.Run(cfg)
}
